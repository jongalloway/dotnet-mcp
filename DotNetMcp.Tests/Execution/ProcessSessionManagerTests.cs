using System;
using System.ComponentModel;
using System.Diagnostics;
using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests.Execution;

/// <summary>
/// Tests for ProcessSessionManager class.
/// </summary>
public class ProcessSessionManagerTests : IDisposable
{
    private readonly ProcessSessionManager _manager;
    private readonly List<Process> _processesToCleanup = new();

    public ProcessSessionManagerTests()
    {
        _manager = new ProcessSessionManager(NullLogger.Instance);
    }

    public void Dispose()
    {
        // Clean up any test processes
        foreach (var process in _processesToCleanup)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
                process.Dispose();
            }
            catch (InvalidOperationException)
            {
                // Best effort cleanup for invalid process state
            }
            catch (Win32Exception)
            {
                // Best effort cleanup for OS-related process errors
            }
        }
        _manager.Clear();
    }

    [Fact]
    public void RegisterSession_WithValidParameters_ReturnsTrue()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var process = CreateTestProcess();

        // Act
        var result = _manager.RegisterSession(sessionId, process, "run", "/test/project.csproj");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RegisterSession_WithDuplicateSessionId_ReturnsFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var process1 = CreateTestProcess();
        var process2 = CreateTestProcess();

        // Act
        var result1 = _manager.RegisterSession(sessionId, process1, "run", "/test/project1.csproj");
        var result2 = _manager.RegisterSession(sessionId, process2, "run", "/test/project2.csproj");

        // Assert
        Assert.True(result1);
        Assert.False(result2);
    }

    [Fact]
    public void RegisterSession_WithNullSessionId_ThrowsArgumentException()
    {
        // Arrange
        var process = CreateTestProcess();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _manager.RegisterSession(null!, process, "run", "/test/project.csproj"));
    }

    [Fact]
    public void RegisterSession_WithNullProcess_ThrowsArgumentNullException()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _manager.RegisterSession(sessionId, null!, "run", "/test/project.csproj"));
    }

    [Fact]
    public void TryStopSession_WithExistingSession_ReturnsTrue()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var process = CreateTestProcess();
        _manager.RegisterSession(sessionId, process, "run", "/test/project.csproj");

        // Act
        var result = _manager.TryStopSession(sessionId, out var errorMessage);

        // Assert
        Assert.True(result);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void TryStopSession_WithNonExistentSession_ReturnsFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();

        // Act
        var result = _manager.TryStopSession(sessionId, out var errorMessage);

        // Assert
        Assert.False(result);
        Assert.NotNull(errorMessage);
        Assert.Contains("not found", errorMessage);
    }

    [Fact]
    public void TryGetSession_WithExistingSession_ReturnsTrue()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var process = CreateTestProcess();
        _manager.RegisterSession(sessionId, process, "run", "/test/project.csproj");

        // Act
        var result = _manager.TryGetSession(sessionId, out var info);

        // Assert
        Assert.True(result);
        Assert.NotNull(info);
        Assert.Equal(sessionId, info.SessionId);
        Assert.Equal("run", info.OperationType);
        Assert.Equal("/test/project.csproj", info.Target);
    }

    [Fact]
    public void TryGetSession_WithNonExistentSession_ReturnsFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();

        // Act
        var result = _manager.TryGetSession(sessionId, out var info);

        // Assert
        Assert.False(result);
        Assert.Null(info);
    }

    [Fact]
    public void GetActiveSessions_ReturnsOnlyRunningSessions()
    {
        // Arrange
        var sessionId1 = Guid.NewGuid().ToString();
        var sessionId2 = Guid.NewGuid().ToString();
        var process1 = CreateTestProcess();
        var process2 = CreateTestProcess();
        
        _manager.RegisterSession(sessionId1, process1, "run", "/test/project1.csproj");
        _manager.RegisterSession(sessionId2, process2, "watch", "/test/project2.csproj");

        // Act
        var activeSessions = _manager.GetActiveSessions();

        // Assert
        Assert.True(activeSessions.Count >= 2); // At least our 2 sessions
        Assert.Contains(activeSessions, s => s.SessionId == sessionId1);
        Assert.Contains(activeSessions, s => s.SessionId == sessionId2);
    }

    [Fact]
    public void ActiveSessionCount_ReturnsCorrectCount()
    {
        // Arrange
        var initialCount = _manager.ActiveSessionCount;
        var sessionId = Guid.NewGuid().ToString();
        var process = CreateTestProcess();
        _manager.RegisterSession(sessionId, process, "run", "/test/project.csproj");

        // Act
        var afterRegisterCount = _manager.ActiveSessionCount;
        _manager.TryStopSession(sessionId, out _);
        var afterStopCount = _manager.ActiveSessionCount;

        // Assert
        Assert.Equal(initialCount + 1, afterRegisterCount);
        Assert.Equal(initialCount, afterStopCount);
    }

    [Fact]
    public void CleanupCompletedSessions_RemovesExitedProcesses()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var process = CreateTestProcess();
        _manager.RegisterSession(sessionId, process, "run", "/test/project.csproj");
        
        // Stop the process
        process.Kill(entireProcessTree: true);
        process.WaitForExit(2000);

        // Act
        var cleanedCount = _manager.CleanupCompletedSessions();

        // Assert
        Assert.True(cleanedCount >= 1);
    }

    [Fact]
    public void Clear_RemovesAllSessions()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var process = CreateTestProcess();
        _manager.RegisterSession(sessionId, process, "run", "/test/project.csproj");

        // Act
        _manager.Clear();

        // Assert
        var result = _manager.TryGetSession(sessionId, out _);
        Assert.False(result);
    }

    [Fact]
    public async Task GetSessionLogs_WithExistingSession_ReturnsLogs()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var process = CreateTestProcessWithOutput();
        _manager.RegisterSession(sessionId, process, "run", "/test/project.csproj");

        // Wait for output to be captured
        await Task.Delay(1000);

        // Act
        var logs = _manager.GetSessionLogs(sessionId);

        // Assert
        Assert.NotNull(logs);
        Assert.Equal(sessionId, logs.SessionId);
        Assert.Equal("run", logs.OperationType);
        Assert.True(logs.IsRunning);
        Assert.NotEmpty(logs.OutputLines);
    }

    [Fact]
    public void GetSessionLogs_WithNonExistentSession_ReturnsNull()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();

        // Act
        var logs = _manager.GetSessionLogs(sessionId);

        // Assert
        Assert.Null(logs);
    }

    [Fact]
    public async Task GetSessionLogs_WithTailLines_ReturnsLimitedOutput()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var process = CreateTestProcessWithOutput();
        _manager.RegisterSession(sessionId, process, "run", "/test/project.csproj");

        // Wait for output to be captured
        await Task.Delay(1000);

        // Act - request only 2 lines
        var logs = _manager.GetSessionLogs(sessionId, tailLines: 2);

        // Assert
        Assert.NotNull(logs);
        var totalLines = logs.OutputLines.Length + logs.ErrorLines.Length;
        Assert.True(totalLines <= 2, $"Expected at most 2 lines, got {totalLines}");
    }

    [Fact]
    public async Task GetSessionLogs_WithSinceTimestamp_ReturnsFilteredOutput()
    {
        // Arrange
        var sessionId = Guid.NewGuid().ToString();
        var process = CreateTestProcessWithOutput();
        var startTime = DateTime.UtcNow;
        _manager.RegisterSession(sessionId, process, "run", "/test/project.csproj");

        // Wait for initial output
        await Task.Delay(500);
        
        var filterTime = DateTime.UtcNow;
        
        // Wait for more output
        await Task.Delay(500);

        // Act - get logs since the filter time
        var logs = _manager.GetSessionLogs(sessionId, since: filterTime);

        // Assert
        Assert.NotNull(logs);
        // All returned lines should be after the filter time
        foreach (var line in logs.OutputLines)
        {
            Assert.True(line.Timestamp >= filterTime, $"Line timestamp {line.Timestamp} should be >= {filterTime}");
        }
        foreach (var line in logs.ErrorLines)
        {
            Assert.True(line.Timestamp >= filterTime, $"Line timestamp {line.Timestamp} should be >= {filterTime}");
        }
    }

    /// <summary>
    /// Creates a test process that can be tracked. Uses a simple sleep command.
    /// </summary>
    private Process CreateTestProcess()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "sleep",
                Arguments = "3600", // Sleep for 1 hour (will be killed in cleanup)
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        process.Start();
        _processesToCleanup.Add(process);
        return process;
    }

    /// <summary>
    /// Creates a test process that produces output on stdout and stderr.
    /// </summary>
    private Process CreateTestProcessWithOutput()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "sh",
                Arguments = "-c \"for i in 1 2 3 4 5; do echo Line $i; echo Error $i >&2; sleep 0.1; done; sleep 3600\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        process.Start();
        _processesToCleanup.Add(process);
        return process;
    }
}
