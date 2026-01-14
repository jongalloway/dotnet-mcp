using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DotNetMcp;

/// <summary>
/// Manages long-running process sessions for operations like 'dotnet run' and 'dotnet watch'.
/// Tracks processes by session ID and provides clean termination semantics.
/// </summary>
public sealed class ProcessSessionManager
{
    private readonly Dictionary<string, ProcessSession> _sessions = new();
    private readonly Lock _lock = new();
    private readonly ILogger? _logger;

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

            _sessions[sessionId] = new ProcessSession
            {
                SessionId = sessionId,
                Process = process,
                OperationType = operationType,
                Target = target,
                StartTime = DateTime.UtcNow
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
            return true;
        }
        catch (InvalidOperationException ex)
        {
            // Process already exited - this is expected and not an error
            _logger?.LogDebug("Session {SessionId} process already exited during stop: {Message}",
                sessionId, ex.Message);
            return true;
        }
        catch (Win32Exception ex)
        {
            errorMessage = $"Failed to stop session '{sessionId}': {ex.Message}";
            _logger?.LogError(ex, "Error stopping session {SessionId}", sessionId);
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
                _sessions.Remove(sessionId);
                _logger?.LogDebug("Cleaned up completed session {SessionId}", sessionId);
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
    /// Clears all sessions. Used for testing.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
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
