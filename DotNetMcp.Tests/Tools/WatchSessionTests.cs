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
/// Tests for background watch mode with session management.
/// </summary>
public class WatchSessionTests : IDisposable
{
    private readonly DotNetCliTools _tools;
    private readonly ProcessSessionManager _sessionManager;
    private readonly ConcurrencyManager _concurrencyManager;

    public WatchSessionTests()
    {
        _sessionManager = new ProcessSessionManager(NullLogger.Instance);
        _concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, _concurrencyManager, _sessionManager);
    }

    public void Dispose()
    {
        _sessionManager.Clear();
    }

    [Fact]
    public async Task DotnetProject_Watch_BackgroundMode_ReturnsSessionId()
    {
        // Create a console app to watch
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a console app
            (await _tools.DotnetProject(
                action: DotnetProjectAction.New,
                template: "console",
                name: "WatchTestApp",
                output: tempDir)).GetText();

            var projectPath = Path.Join(tempDir, "WatchTestApp.csproj");

            // Start watch in background mode
            var result = (await _tools.DotnetProject(
                action: DotnetProjectAction.Watch,
                project: projectPath,
                watchAction: "run",
                startMode: StartMode.Background)).GetText();

            // Verify it returned immediately with session metadata
            Assert.NotNull(result);
            Assert.DoesNotContain("Error:", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Session ID:", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("PID:", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Watch process started in background mode", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Watch Action: run", result, StringComparison.OrdinalIgnoreCase);

            var sessionId = ExtractRequiredMetadataValue(result, "Session ID");
            Assert.NotEmpty(sessionId);

            // Verify the session is registered as "watch" type
            var sessionExists = _sessionManager.TryGetSession(sessionId, out var sessionInfo);
            Assert.True(sessionExists);
            Assert.NotNull(sessionInfo);
            Assert.Equal("watch", sessionInfo!.OperationType);

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
    public async Task DotnetProject_Watch_ForegroundMode_ReturnsWarning()
    {
        // Foreground watch should return the existing warning message
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Watch,
            watchAction: "run",
            project: "TestProject.csproj",
            startMode: StartMode.Foreground)).GetText();

        Assert.NotNull(result);
        Assert.Contains("Warning:", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("long-running command", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dotnet watch", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_Watch_DefaultStartMode_ReturnsWarning()
    {
        // Default (no startMode) should behave as foreground
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Watch,
            watchAction: "build",
            project: "TestProject.csproj")).GetText();

        Assert.NotNull(result);
        Assert.Contains("Warning:", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("long-running command", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_Watch_MissingWatchAction_ReturnsError()
    {
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Watch,
            watchAction: null,
            startMode: StartMode.Background)).GetText();

        Assert.NotNull(result);
        Assert.Contains("Error", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("watchAction", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_Watch_InvalidWatchAction_ReturnsError()
    {
        var result = (await _tools.DotnetProject(
            action: DotnetProjectAction.Watch,
            watchAction: "invalid",
            startMode: StartMode.Background)).GetText();

        Assert.NotNull(result);
        Assert.Contains("Error", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Invalid watchAction", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetProject_Watch_BackgroundStop_TerminatesWatchProcess()
    {
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a console app
            (await _tools.DotnetProject(
                action: DotnetProjectAction.New,
                template: "console",
                name: "WatchStopApp",
                output: tempDir)).GetText();

            var projectPath = Path.Join(tempDir, "WatchStopApp.csproj");

            // Start watch build in background
            var result = (await _tools.DotnetProject(
                action: DotnetProjectAction.Watch,
                project: projectPath,
                watchAction: "build",
                startMode: StartMode.Background)).GetText();

            Assert.DoesNotContain("Error:", result, StringComparison.OrdinalIgnoreCase);

            var sessionId = ExtractRequiredMetadataValue(result, "Session ID");
            var pidStr = ExtractRequiredMetadataValue(result, "PID");
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
            await Task.Delay(1000, TestContext.Current.CancellationToken);

            // Verify process is terminated
            try
            {
                process.Refresh();
                Assert.True(process.HasExited, "Watch process should have exited after stop");
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
    public async Task DotnetProject_Watch_BackgroundTestAction_ReturnsSessionId()
    {
        var tempDir = Path.Join(Path.GetTempPath(), "dotnet-mcp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create a test project
            (await _tools.DotnetProject(
                action: DotnetProjectAction.New,
                template: "xunit",
                name: "WatchTestProject",
                output: tempDir)).GetText();

            var projectPath = Path.Join(tempDir, "WatchTestProject.csproj");

            // Start watch test in background
            var result = (await _tools.DotnetProject(
                action: DotnetProjectAction.Watch,
                project: projectPath,
                watchAction: "test",
                startMode: StartMode.Background)).GetText();

            Assert.NotNull(result);
            Assert.DoesNotContain("Error:", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Session ID:", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Watch Action: test", result, StringComparison.OrdinalIgnoreCase);

            var sessionId = ExtractRequiredMetadataValue(result, "Session ID");

            // Verify session is registered as watch type
            var sessionExists = _sessionManager.TryGetSession(sessionId, out var sessionInfo);
            Assert.True(sessionExists);
            Assert.Equal("watch", sessionInfo!.OperationType);

            // Clean up
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