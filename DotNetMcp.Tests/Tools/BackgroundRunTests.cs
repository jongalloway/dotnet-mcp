using System;
using System.Diagnostics;
using System.Linq;
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
            (await _tools.DotnetProject(
                action: DotnetProjectAction.New,
                template: "console",
                name: "TestApp",
                output: tempDir)).GetText();

            var projectPath = Path.Join(tempDir, "TestApp.csproj");

            // Build it with Release configuration to match CI test execution
            (await _tools.DotnetProject(
                action: DotnetProjectAction.Build,
                project: projectPath,
                configuration: "Release")).GetText();

            // Ensure build artifacts exist before running - search for DLL
            // rather than hardcoding TFM (SDK version determines the default TFM)
            var releaseBinPath = Path.Join(tempDir, "bin", "Release");
            
            // Poll for build artifact instead of fixed delay (file system flush can vary)
            var artifactDeadline = DateTime.UtcNow.AddSeconds(15);
            string? exePath = null;
            while (DateTime.UtcNow < artifactDeadline)
            {
                if (Directory.Exists(releaseBinPath))
                {
                    var candidates = Directory.GetFiles(releaseBinPath, "TestApp.dll", SearchOption.AllDirectories);
                    if (candidates.Length > 0)
                    {
                        exePath = candidates[0];
                        break;
                    }
                }
                await Task.Delay(250, TestContext.Current.CancellationToken);
            }
            
            // Verify the built DLL exists
            if (exePath == null)
            {
                throw new InvalidOperationException($"Build artifact not found under: {releaseBinPath}");
            }

            // Run in foreground mode (default) - should block until exit
            var result = (await _tools.DotnetProject(
                action: DotnetProjectAction.Run,
                project: projectPath,
                configuration: "Release",
                noBuild: true,
                startMode: StartMode.Foreground)).GetText();

            // Verify it completed (should have exit code)
            Assert.NotNull(result);
            Assert.DoesNotContain("Error:", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Exit Code: 0", result, StringComparison.OrdinalIgnoreCase);
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
            (await _tools.DotnetProject(
                action: DotnetProjectAction.New,
                template: "console",
                name: "TestApp",
                output: tempDir)).GetText();

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

            // Build it with Release configuration to match CI test execution
            (await _tools.DotnetProject(
                action: DotnetProjectAction.Build,
                project: projectPath,
                configuration: "Release")).GetText();

            // Run in background mode
            var result = (await _tools.DotnetProject(
                action: DotnetProjectAction.Run,
                project: projectPath,
                configuration: "Release",
                noBuild: true,
                startMode: StartMode.Background)).GetText();

            // Verify it returned immediately with session metadata
            Assert.NotNull(result);
            Assert.DoesNotContain("Error:", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Session ID:", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("PID:", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Process started in background mode", result, StringComparison.OrdinalIgnoreCase);

            var sessionId = ExtractRequiredMetadataValue(result, "Session ID");
            Assert.NotEmpty(sessionId);

            // Verify the session is registered
            var sessionExists = _sessionManager.TryGetSession(sessionId, out var sessionInfo);
            Assert.True(sessionExists);
            Assert.NotNull(sessionInfo);
            Assert.Equal("run", sessionInfo!.OperationType);

            // Clean up - stop the session
            var stopResult = (await _tools.DotnetProject(
                action: DotnetProjectAction.Stop,
                sessionId: sessionId)).GetText();

            Assert.DoesNotContain("Error:", stopResult, StringComparison.OrdinalIgnoreCase);
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
            (await _tools.DotnetProject(
                action: DotnetProjectAction.New,
                template: "console",
                name: "TestApp",
                output: tempDir)).GetText();

            var projectPath = Path.Join(tempDir, "TestApp.csproj");
            var programFile = Path.Join(tempDir, "Program.cs");

            // Modify to sleep
            File.WriteAllText(programFile, @"
using System;
using System.Threading;
Thread.Sleep(TimeSpan.FromSeconds(30));
");

            // Build it with Release configuration to match CI test execution
            (await _tools.DotnetProject(
                action: DotnetProjectAction.Build,
                project: projectPath,
                configuration: "Release")).GetText();

            // Run with noBuild=true in background
            var result = (await _tools.DotnetProject(
                action: DotnetProjectAction.Run,
                project: projectPath,
                configuration: "Release",
                noBuild: true,
                startMode: StartMode.Background)).GetText();

            Assert.DoesNotContain("Error:", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Session ID:", result, StringComparison.OrdinalIgnoreCase);

            // Parse and stop
            var sessionId = ExtractRequiredMetadataValue(result, "Session ID");

            (await _tools.DotnetProject(
                action: DotnetProjectAction.Stop,
                sessionId: sessionId)).GetText();
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
        var cancellationToken = TestContext.Current.CancellationToken;

        // Create a console app
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create console app with long sleep
            (await _tools.DotnetProject(
                action: DotnetProjectAction.New,
                template: "console",
                name: "TestApp",
                output: tempDir)).GetText();

            var projectPath = Path.Join(tempDir, "TestApp.csproj");
            var programFile = Path.Join(tempDir, "Program.cs");

            File.WriteAllText(programFile, @"
using System;
using System.Threading;
Console.WriteLine(""App started"");
Thread.Sleep(TimeSpan.FromMinutes(5));
Console.WriteLine(""App finished"");
");

            // Build with Release configuration to match CI test execution
            (await _tools.DotnetProject(
                action: DotnetProjectAction.Build,
                project: projectPath,
                configuration: "Release")).GetText();

            // Start in background
            var runResult = (await _tools.DotnetProject(
                action: DotnetProjectAction.Run,
                project: projectPath,
                configuration: "Release",
                noBuild: true,
                startMode: StartMode.Background)).GetText();

            var sessionId = ExtractRequiredMetadataValue(runResult, "Session ID");
            var pidStr = ExtractRequiredMetadataValue(runResult, "PID");
            var pid = int.Parse(pidStr);

            // Verify process is running
            var process = Process.GetProcessById(pid);
            Assert.False(process.HasExited);

            // Stop it
            var stopResult = (await _tools.DotnetProject(
                action: DotnetProjectAction.Stop,
                sessionId: sessionId)).GetText();

            Assert.DoesNotContain("Error:", stopResult, StringComparison.OrdinalIgnoreCase);

            // Wait a bit for the process to actually terminate
            await Task.Delay(1000, cancellationToken);

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
        var cancellationToken = TestContext.Current.CancellationToken;

        // Create a console app that exits quickly
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create console app with short sleep
            (await _tools.DotnetProject(
                action: DotnetProjectAction.New,
                template: "console",
                name: "TestApp",
                output: tempDir)).GetText();

            var projectPath = Path.Join(tempDir, "TestApp.csproj");
            var programFile = Path.Join(tempDir, "Program.cs");

            File.WriteAllText(programFile, @"
using System;
using System.Threading;
Console.WriteLine(""Quick app"");
Thread.Sleep(TimeSpan.FromSeconds(2));
Console.WriteLine(""Done"");
");

            // Build with Release configuration to match CI test execution
            (await _tools.DotnetProject(
                action: DotnetProjectAction.Build,
                project: projectPath,
                configuration: "Release")).GetText();

            // Start in background
            var runResult = (await _tools.DotnetProject(
                action: DotnetProjectAction.Run,
                project: projectPath,
                configuration: "Release",
                noBuild: true,
                startMode: StartMode.Background)).GetText();

            var sessionId = ExtractRequiredMetadataValue(runResult, "Session ID");

            // Verify session exists
            var sessionExists = _sessionManager.TryGetSession(sessionId, out _);
            Assert.True(sessionExists);

            // Poll for process exit instead of fixed delays (startup + JIT time can vary)
            var exitDeadline = DateTime.UtcNow.AddSeconds(30);
            bool processExited = false;
            while (DateTime.UtcNow < exitDeadline)
            {
                var activeSessions = _sessionManager.GetActiveSessions();
                var activeSession = activeSessions.FirstOrDefault(s => s.SessionId == sessionId);
                if (activeSession == null || !activeSession.IsRunning)
                {
                    processExited = true;
                    break;
                }
                await Task.Delay(500, cancellationToken);
            }

            Assert.True(processExited, "Process should have exited within 30 seconds");
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

    private static string ExtractRequiredMetadataValue(string output, string key)
    {
        var prefix = key + ":";
        foreach (var line in output
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            var value = line[prefix.Length..].Trim();
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        throw new InvalidOperationException($"Missing '{prefix}' in output: {output}");
    }
}
