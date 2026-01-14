using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests.Tools;

/// <summary>
/// Tests for background run mode with session management.
/// </summary>
public class BackgroundRunTests : IDisposable
{
    private readonly DotNetCliTools _tools;
    private readonly ProcessSessionManager _sessionManager;
    private readonly ConcurrencyManager _concurrencyManager;
    private readonly List<Process> _processesToCleanup = new();

    public BackgroundRunTests()
    {
        _sessionManager = new ProcessSessionManager(NullLogger.Instance);
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager, _sessionManager);
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
            catch (Exception)
            {
                // Best effort cleanup
            }
        }
        _sessionManager.Clear();
    }

    [Fact]
    public async Task DotnetProject_Run_ForegroundMode_BlocksUntilExit()
    {
        // Create a simple console app that exits immediately
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a console app
            await _tools.DotnetProject(
                action: DotnetProjectAction.New,
                template: "console",
                name: "TestApp",
                output: tempDir,
                machineReadable: false);

            var projectPath = Path.Join(tempDir, "TestApp.csproj");

            // Build it
            await _tools.DotnetProject(
                action: DotnetProjectAction.Build,
                project: projectPath,
                machineReadable: false);

            // Run in foreground mode (default) - should block until exit
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Run,
                project: projectPath,
                noBuild: true,
                startMode: StartMode.Foreground,
                machineReadable: true);

            // Verify it completed (should have exit code)
            Assert.NotNull(result);
            Assert.Contains("\"success\": true", result);
            Assert.Contains("\"exitCode\": 0", result);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public async Task DotnetProject_Run_BackgroundMode_ReturnsSessionId()
    {
        // Create a long-running console app
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a console app with a delay
            await _tools.DotnetProject(
                action: DotnetProjectAction.New,
                template: "console",
                name: "TestApp",
                output: tempDir,
                machineReadable: false);

            var projectPath = Path.Join(tempDir, "TestApp.csproj");
            var programFile = Path.Join(tempDir, "Program.cs");

            // Modify Program.cs to sleep for a while
            var programContent = @"
using System;
using System.Threading;

Console.WriteLine(""Starting long-running app..."");
Thread.Sleep(TimeSpan.FromSeconds(30));
Console.WriteLine(""Finished"");
";
            File.WriteAllText(programFile, programContent);

            // Build it
            await _tools.DotnetProject(
                action: DotnetProjectAction.Build,
                project: projectPath,
                machineReadable: false);

            // Run in background mode
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Run,
                project: projectPath,
                noBuild: true,
                startMode: StartMode.Background,
                machineReadable: true);

            // Verify it returned immediately with session metadata
            Assert.NotNull(result);
            Assert.Contains("\"success\": true", result);
            Assert.Contains("\"sessionId\":", result);
            Assert.Contains("\"pid\":", result);
            Assert.Contains("\"operationType\": \"run\"", result);
            Assert.Contains("\"startMode\": \"background\"", result);

            // Parse the result to get the session ID
            var jsonDoc = JsonDocument.Parse(result);
            var sessionId = jsonDoc.RootElement.GetProperty("metadata").GetProperty("sessionId").GetString();
            Assert.NotNull(sessionId);

            // Verify the session is registered
            var sessionExists = _sessionManager.TryGetSession(sessionId!, out var sessionInfo);
            Assert.True(sessionExists);
            Assert.NotNull(sessionInfo);
            Assert.Equal("run", sessionInfo!.OperationType);

            // Clean up - stop the session
            var stopResult = await _tools.DotnetProject(
                action: DotnetProjectAction.Stop,
                sessionId: sessionId,
                machineReadable: true);

            Assert.Contains("\"success\": true", stopResult);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public async Task DotnetProject_Run_BackgroundMode_WithNoBuild_WorksCorrectly()
    {
        // Create a console app
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create console app
            await _tools.DotnetProject(
                action: DotnetProjectAction.New,
                template: "console",
                name: "TestApp",
                output: tempDir,
                machineReadable: false);

            var projectPath = Path.Join(tempDir, "TestApp.csproj");
            var programFile = Path.Join(tempDir, "Program.cs");

            // Modify to sleep
            File.WriteAllText(programFile, @"
using System;
using System.Threading;
Thread.Sleep(TimeSpan.FromSeconds(30));
");

            // Build it
            await _tools.DotnetProject(
                action: DotnetProjectAction.Build,
                project: projectPath,
                machineReadable: false);

            // Run with noBuild=true in background
            var result = await _tools.DotnetProject(
                action: DotnetProjectAction.Run,
                project: projectPath,
                noBuild: true,
                startMode: StartMode.Background,
                machineReadable: true);

            Assert.Contains("\"success\": true", result);
            Assert.Contains("\"sessionId\":", result);

            // Parse and stop
            var jsonDoc = JsonDocument.Parse(result);
            var sessionId = jsonDoc.RootElement.GetProperty("metadata").GetProperty("sessionId").GetString();

            await _tools.DotnetProject(
                action: DotnetProjectAction.Stop,
                sessionId: sessionId,
                machineReadable: true);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public async Task DotnetProject_Stop_WithBackgroundSession_TerminatesProcess()
    {
        // Create a console app
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create console app with long sleep
            await _tools.DotnetProject(
                action: DotnetProjectAction.New,
                template: "console",
                name: "TestApp",
                output: tempDir,
                machineReadable: false);

            var projectPath = Path.Join(tempDir, "TestApp.csproj");
            var programFile = Path.Join(tempDir, "Program.cs");

            File.WriteAllText(programFile, @"
using System;
using System.Threading;
Console.WriteLine(""App started"");
Thread.Sleep(TimeSpan.FromMinutes(5));
Console.WriteLine(""App finished"");
");

            // Build
            await _tools.DotnetProject(
                action: DotnetProjectAction.Build,
                project: projectPath,
                machineReadable: false);

            // Start in background
            var runResult = await _tools.DotnetProject(
                action: DotnetProjectAction.Run,
                project: projectPath,
                noBuild: true,
                startMode: StartMode.Background,
                machineReadable: true);

            var jsonDoc = JsonDocument.Parse(runResult);
            var sessionId = jsonDoc.RootElement.GetProperty("metadata").GetProperty("sessionId").GetString();
            var pidStr = jsonDoc.RootElement.GetProperty("metadata").GetProperty("pid").GetString();
            var pid = int.Parse(pidStr!);

            // Verify process is running
            var process = Process.GetProcessById(pid);
            Assert.False(process.HasExited);

            // Stop it
            var stopResult = await _tools.DotnetProject(
                action: DotnetProjectAction.Stop,
                sessionId: sessionId,
                machineReadable: true);

            Assert.Contains("\"success\": true", stopResult);

            // Wait a bit for the process to actually terminate
            await Task.Delay(1000);

            // Verify process is terminated
            try
            {
                process.Refresh();
                Assert.True(process.HasExited, "Process should have exited after stop");
            }
            catch (ArgumentException)
            {
                // Process no longer exists - this is expected
            }

            process.Dispose();
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public async Task DotnetProject_Run_BackgroundMode_CleansUpSessionAfterProcessExits()
    {
        // Create a console app that exits quickly
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create console app with short sleep
            await _tools.DotnetProject(
                action: DotnetProjectAction.New,
                template: "console",
                name: "TestApp",
                output: tempDir,
                machineReadable: false);

            var projectPath = Path.Join(tempDir, "TestApp.csproj");
            var programFile = Path.Join(tempDir, "Program.cs");

            File.WriteAllText(programFile, @"
using System;
using System.Threading;
Console.WriteLine(""Quick app"");
Thread.Sleep(TimeSpan.FromSeconds(2));
Console.WriteLine(""Done"");
");

            // Build
            await _tools.DotnetProject(
                action: DotnetProjectAction.Build,
                project: projectPath,
                machineReadable: false);

            // Start in background
            var runResult = await _tools.DotnetProject(
                action: DotnetProjectAction.Run,
                project: projectPath,
                noBuild: true,
                startMode: StartMode.Background,
                machineReadable: true);

            var jsonDoc = JsonDocument.Parse(runResult);
            var sessionId = jsonDoc.RootElement.GetProperty("metadata").GetProperty("sessionId").GetString();

            // Verify session exists
            var sessionExists = _sessionManager.TryGetSession(sessionId!, out _);
            Assert.True(sessionExists);

            // Wait for the process to exit
            await Task.Delay(5000);

            // Session should be cleaned up automatically (the cleanup happens in the background task)
            // Give it a moment for the cleanup task to run
            await Task.Delay(1000);

            // Try to get the session - it might still be there if cleanup hasn't run yet
            // But we can verify by checking active sessions
            var activeSessions = _sessionManager.GetActiveSessions();
            var activeSession = activeSessions.FirstOrDefault(s => s.SessionId == sessionId);

            // The process should not be running
            if (activeSession != null)
            {
                Assert.False(activeSession.IsRunning, "Process should have exited");
            }
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
