using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace DotNetMcp;

/// <summary>
/// Manages long-running process sessions for operations like 'dotnet run' and 'dotnet watch'.
/// Tracks processes by session ID and provides clean termination semantics.
/// Captures stdout/stderr output for background processes with configurable size limits.
/// </summary>
public sealed class ProcessSessionManager
{
    private readonly Dictionary<string, ProcessSession> _sessions = new();
    private readonly Lock _lock = new();
    private readonly ILogger? _logger;

    /// <summary>
    /// Maximum number of output lines to buffer per session (default: 1000).
    /// Prevents unbounded memory growth for long-running processes.
    /// </summary>
    private const int MaxOutputLines = 1000;

    public ProcessSessionManager(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a new process session.
    /// </summary>
    /// <param name="sessionId">Unique session identifier (typically a GUID)</param>
    /// <param name="process">The process to track</param>
    /// <param name="operationType">Type of operation (e.g., "run", "watch")</param>
    /// <param name="target">Target project or directory</param>
    /// <returns>True if session was registered, false if sessionId already exists</returns>
    public bool RegisterSession(string sessionId, Process process, string operationType, string target)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        if (process == null)
            throw new ArgumentNullException(nameof(process));

        lock (_lock)
        {
            if (_sessions.ContainsKey(sessionId))
            {
                _logger?.LogWarning("Session ID {SessionId} already exists", sessionId);
                return false;
            }

            var outputBuffer = new ConcurrentQueue<OutputLine>();
            var errorBuffer = new ConcurrentQueue<OutputLine>();

            // Set up output capture handlers
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    var line = new OutputLine { Timestamp = DateTime.UtcNow, Content = e.Data };
                    outputBuffer.Enqueue(line);
                    
                    // Trim buffer if it exceeds max size
                    while (outputBuffer.Count > MaxOutputLines)
                    {
                        outputBuffer.TryDequeue(out _);
                    }
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    var line = new OutputLine { Timestamp = DateTime.UtcNow, Content = e.Data };
                    errorBuffer.Enqueue(line);
                    
                    // Trim buffer if it exceeds max size
                    while (errorBuffer.Count > MaxOutputLines)
                    {
                        errorBuffer.TryDequeue(out _);
                    }
                }
            };

            // Start reading output streams
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _sessions[sessionId] = new ProcessSession
            {
                SessionId = sessionId,
                Process = process,
                OperationType = operationType,
                Target = target,
                StartTime = DateTime.UtcNow,
                OutputBuffer = outputBuffer,
                ErrorBuffer = errorBuffer
            };

            _logger?.LogInformation("Registered session {SessionId} for {OperationType} on {Target} (PID: {ProcessId})",
                sessionId, operationType, target, process.Id);

            return true;
        }
    }

    /// <summary>
    /// Stops a process session by terminating the entire process tree.
    /// </summary>
    /// <param name="sessionId">Session ID to stop</param>
    /// <param name="errorMessage">Error message if stop fails</param>
    /// <returns>True if session was stopped successfully, false otherwise</returns>
    public bool TryStopSession(string sessionId, out string? errorMessage)
    {
        errorMessage = null;

        ProcessSession? session;
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out session))
            {
                errorMessage = $"Session '{sessionId}' not found. It may have already completed or been stopped.";
                _logger?.LogWarning("Attempted to stop non-existent session {SessionId}", sessionId);
                return false;
            }

            // Remove from tracking immediately to prevent double-stop
            _sessions.Remove(sessionId);
        }

        try
        {
            if (session.Process.HasExited)
            {
                _logger?.LogInformation("Session {SessionId} process already exited (Exit code: {ExitCode})",
                    sessionId, session.Process.ExitCode);
                session.Process.Dispose();
                return true;
            }

            _logger?.LogInformation("Stopping session {SessionId} (PID: {ProcessId})", sessionId, session.Process.Id);

            // Kill the entire process tree to clean up child processes
            session.Process.Kill(entireProcessTree: true);

            // Wait a short time for graceful termination
            if (!session.Process.WaitForExit(5000))
            {
                _logger?.LogWarning("Session {SessionId} did not exit within 5 seconds", sessionId);
            }

            _logger?.LogInformation("Successfully stopped session {SessionId}", sessionId);
            session.Process.Dispose();
            return true;
        }
        catch (InvalidOperationException ex)
        {
            // Process already exited - this is expected and not an error
            _logger?.LogDebug("Session {SessionId} process already exited during stop: {Message}",
                sessionId, ex.Message);
            try
            {
                session.Process.Dispose();
            }
            catch (InvalidOperationException)
            {
                // Process already disposed
            }
            return true;
        }
        catch (Win32Exception ex)
        {
            errorMessage = $"Failed to stop session '{sessionId}': {ex.Message}";
            _logger?.LogError(ex, "Error stopping session {SessionId}", sessionId);
            try
            {
                session.Process.Dispose();
            }
            catch (InvalidOperationException)
            {
                // Process already disposed
            }
            return false;
        }
    }

    /// <summary>
    /// Gets information about a session.
    /// </summary>
    public bool TryGetSession(string sessionId, out ProcessSessionInfo? info)
    {
        info = null;
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return false;

            try
            {
                info = new ProcessSessionInfo
                {
                    SessionId = session.SessionId,
                    ProcessId = session.Process.Id,
                    OperationType = session.OperationType,
                    Target = session.Target,
                    StartTime = session.StartTime,
                    IsRunning = !session.Process.HasExited
                };
                return true;
            }
            catch (InvalidOperationException)
            {
                // Process already exited
                return false;
            }
        }
    }

    /// <summary>
    /// Gets all active sessions.
    /// </summary>
    public List<ProcessSessionInfo> GetActiveSessions()
    {
        lock (_lock)
        {
            var activeSessions = new List<ProcessSessionInfo>();
            foreach (var kvp in _sessions)
            {
                try
                {
                    if (!kvp.Value.Process.HasExited)
                    {
                        activeSessions.Add(new ProcessSessionInfo
                        {
                            SessionId = kvp.Value.SessionId,
                            ProcessId = kvp.Value.Process.Id,
                            OperationType = kvp.Value.OperationType,
                            Target = kvp.Value.Target,
                            StartTime = kvp.Value.StartTime,
                            IsRunning = true
                        });
                    }
                }
                catch (InvalidOperationException)
                {
                    // Process already exited - skip
                }
            }
            return activeSessions;
        }
    }

    /// <summary>
    /// Cleans up completed sessions (processes that have exited).
    /// Disposes the Process objects to release system resources.
    /// </summary>
    public int CleanupCompletedSessions()
    {
        lock (_lock)
        {
            var completed = _sessions
                .Where(kvp =>
                {
                    try
                    {
                        return kvp.Value.Process.HasExited;
                    }
                    catch (InvalidOperationException)
                    {
                        return true; // Already disposed
                    }
                })
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var sessionId in completed)
            {
                if (_sessions.TryGetValue(sessionId, out var session))
                {
                    try
                    {
                        session.Process.Dispose();
                        _logger?.LogDebug("Disposed process and cleaned up completed session {SessionId}", sessionId);
                    }
                    catch (InvalidOperationException)
                    {
                        // Process already disposed
                        _logger?.LogDebug("Cleaned up completed session {SessionId} (process already disposed)", sessionId);
                    }
                }
                _sessions.Remove(sessionId);
            }

            return completed.Count;
        }
    }

    /// <summary>
    /// Gets the count of active sessions.
    /// </summary>
    public int ActiveSessionCount
    {
        get
        {
            lock (_lock)
            {
                return _sessions.Count(kvp =>
                {
                    try
                    {
                        return !kvp.Value.Process.HasExited;
                    }
                    catch (InvalidOperationException)
                    {
                        return false;
                    }
                });
            }
        }
    }

    /// <summary>
    /// Gets logs (stdout/stderr) for a session.
    /// </summary>
    /// <param name="sessionId">Session ID to retrieve logs for</param>
    /// <param name="tailLines">Number of most recent lines to return (optional, returns all if not specified)</param>
    /// <param name="since">Only return lines after this timestamp (optional)</param>
    /// <returns>Session logs if found, null otherwise</returns>
    public ProcessSessionLogs? GetSessionLogs(string sessionId, int? tailLines = null, DateTime? since = null)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return null;

            var outputLines = session.OutputBuffer.ToArray();
            var errorLines = session.ErrorBuffer.ToArray();

            // Apply timestamp filter if specified
            if (since.HasValue)
            {
                outputLines = outputLines.Where(l => l.Timestamp >= since.Value).ToArray();
                errorLines = errorLines.Where(l => l.Timestamp >= since.Value).ToArray();
            }

            // Apply tail filter if specified
            if (tailLines.HasValue && tailLines.Value > 0)
            {
                var totalLines = outputLines.Length + errorLines.Length;
                if (totalLines > tailLines.Value)
                {
                    // Combine all lines, sort by timestamp, and take the last N
                    var allLines = outputLines.Select(l => (l, isError: false))
                        .Concat(errorLines.Select(l => (l, isError: true)))
                        .OrderBy(x => x.l.Timestamp)
                        .TakeLast(tailLines.Value)
                        .ToList();

                    outputLines = allLines.Where(x => !x.isError).Select(x => x.l).ToArray();
                    errorLines = allLines.Where(x => x.isError).Select(x => x.l).ToArray();
                }
            }

            bool isRunning;
            try
            {
                isRunning = !session.Process.HasExited;
            }
            catch (InvalidOperationException)
            {
                isRunning = false;
            }

            return new ProcessSessionLogs
            {
                SessionId = session.SessionId,
                OperationType = session.OperationType,
                Target = session.Target,
                StartTime = session.StartTime,
                IsRunning = isRunning,
                OutputLines = outputLines,
                ErrorLines = errorLines,
                TotalOutputLines = session.OutputBuffer.Count,
                TotalErrorLines = session.ErrorBuffer.Count
            };
        }
    }

    /// <summary>
    /// Clears all sessions. Used for testing.
    /// Disposes all tracked processes before removing them to avoid resource leaks.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            // Take a snapshot to avoid modifying the dictionary while iterating
            var sessions = _sessions.Values.ToList();

            foreach (var session in sessions)
            {
                var process = session.Process;
                try
                {
                    // If the process is still running, attempt to terminate it
                    // Ignore failures here since this is primarily a test helper
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(entireProcessTree: true);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Process is already exited or cannot be killed; ignore
                    }
                    catch (Win32Exception)
                    {
                        // Process might have already exited or we lack permission; ignore
                    }

                    process.Dispose();
                    _logger?.LogDebug("Disposed process for session {SessionId} during Clear()", session.SessionId);
                }
                catch (InvalidOperationException)
                {
                    // Process already disposed; nothing more to do
                }
            }

            _sessions.Clear();
        }
    }

    private sealed class ProcessSession
    {
        public required string SessionId { get; init; }
        public required Process Process { get; init; }
        public required string OperationType { get; init; }
        public required string Target { get; init; }
        public required DateTime StartTime { get; init; }
        public required ConcurrentQueue<OutputLine> OutputBuffer { get; init; }
        public required ConcurrentQueue<OutputLine> ErrorBuffer { get; init; }
    }
}

/// <summary>
/// Information about a process session.
/// </summary>
public sealed class ProcessSessionInfo
{
    public required string SessionId { get; init; }
    public required int ProcessId { get; init; }
    public required string OperationType { get; init; }
    public required string Target { get; init; }
    public required DateTime StartTime { get; init; }
    public required bool IsRunning { get; init; }
}

/// <summary>
/// A single line of output from a process with timestamp.
/// </summary>
public sealed class OutputLine
{
    public required DateTime Timestamp { get; init; }
    public required string Content { get; init; }
}

/// <summary>
/// Logs/output from a process session.
/// </summary>
public sealed class ProcessSessionLogs
{
    public required string SessionId { get; init; }
    public required string OperationType { get; init; }
    public required string Target { get; init; }
    public required DateTime StartTime { get; init; }
    public required bool IsRunning { get; init; }
    public required OutputLine[] OutputLines { get; init; }
    public required OutputLine[] ErrorLines { get; init; }
    public required int TotalOutputLines { get; init; }
    public required int TotalErrorLines { get; init; }
}
