using DotNetMcp;
using System.Diagnostics;
using Xunit;

namespace DotNetMcp.Tests.Scenarios;

public class BackgroundRunScenarioTests
{
    private readonly ITestOutputHelper _output;

    public BackgroundRunScenarioTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static async Task<string> WaitForLogsContainingAsync(
        McpScenarioClient client,
        string sessionId,
        string[] expectedFragments,
        CancellationToken cancellationToken,
        int timeoutSeconds = 15)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        string? latestLogs = null;

        while (DateTime.UtcNow < deadline)
        {
            latestLogs = await client.CallToolTextAsync(
                toolName: "dotnet_project",
                args: new Dictionary<string, object?>
                {
                    ["action"] = "Logs",
                    ["sessionId"] = sessionId,
                },
                cancellationToken);

            if (expectedFragments.All(fragment => latestLogs.Contains(fragment, StringComparison.Ordinal)))
            {
                return latestLogs;
            }

            await Task.Delay(250, cancellationToken);
        }

        return latestLogs ?? string.Empty;
    }

    private static string? ParsePrefixedLine(string text, string prefix)
    {
        var idx = text.IndexOf(prefix, StringComparison.Ordinal);
        if (idx < 0) return null;
        var start = idx + prefix.Length;
        var end = text.IndexOfAny(new[] { '\r', '\n' }, start);
        return (end >= 0 ? text[start..end] : text[start..]).Trim();
    }

    [ScenarioFact]
    public async Task Scenario_BackgroundRun_StartStopProcess_Success()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var tempRoot = ScenarioHelpers.CreateTempDirectory(nameof(Scenario_BackgroundRun_StartStopProcess_Success));

        // Create a simple console app that runs for a while
        var (exitCode, _, stderr) = await ScenarioHelpers.RunDotNetAsync(
            $"new console -n LongRunningApp -o \"{tempRoot.Path}\"",
            workingDirectory: tempRoot.Path,
            cancellationToken);

        Assert.True(exitCode == 0, $"dotnet new console failed: {stderr}");

        var projectPath = Path.Join(tempRoot.Path, "LongRunningApp.csproj");
        var programPath = Path.Join(tempRoot.Path, "Program.cs");
        Assert.True(File.Exists(projectPath), $"Expected LongRunningApp.csproj to exist at {projectPath}");

        // Modify Program.cs to run for a while
        var programContent = @"
using System;
using System.Threading;

Console.WriteLine(""Application started"");
// Run for 2 minutes to simulate long-running process
Thread.Sleep(TimeSpan.FromMinutes(2));
Console.WriteLine(""Application finished"");
";
        File.WriteAllText(programPath, programContent);

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken, _output);

        // Build the project
        var buildText = await client.CallToolTextAsync(
            toolName: "dotnet_project",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Build",
                ["project"] = projectPath,
            },
            cancellationToken);

        Assert.DoesNotContain("Error:", buildText);

        // Start the app in background mode with noBuild=true
        var runText = await client.CallToolTextAsync(
            toolName: "dotnet_project",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Run",
                ["project"] = projectPath,
                ["noBuild"] = true,
                ["startMode"] = "Background",
            },
            cancellationToken);

        Assert.Contains("Process started in background mode", runText);

        var sessionId = ParsePrefixedLine(runText, "Session ID: ");
        var pidString = ParsePrefixedLine(runText, "PID: ");

        Assert.NotNull(sessionId);
        Assert.NotNull(pidString);

        var pid = int.Parse(pidString!);

        // Verify the process is running and stop it
        using (var process = Process.GetProcessById(pid))
        {
            Assert.False(process.HasExited, "Process should still be running");

            // Stop the process using the sessionId
            var stopText = await client.CallToolTextAsync(
                toolName: "dotnet_project",
                args: new Dictionary<string, object?>
                {
                    ["action"] = "Stop",
                    ["sessionId"] = sessionId,
                },
                cancellationToken);

            Assert.Contains("stopped", stopText, StringComparison.OrdinalIgnoreCase);

            // Wait a moment for the process to actually terminate
            await Task.Delay(2000, cancellationToken);

            // Verify the process has been terminated
            try
            {
                process.Refresh();
                Assert.True(process.HasExited, "Process should have exited after Stop");
            }
            catch (InvalidOperationException)
            {
                // Process no longer exists - this is expected and acceptable
            }
        }
    }

    [ScenarioFact]
    public async Task Scenario_BackgroundRun_RetrieveLogs_Success()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var tempRoot = ScenarioHelpers.CreateTempDirectory(nameof(Scenario_BackgroundRun_RetrieveLogs_Success));

        // Create a simple console app that outputs text
        var (exitCode, _, stderr) = await ScenarioHelpers.RunDotNetAsync(
            $"new console -n LogTestApp -o \"{tempRoot.Path}\"",
            workingDirectory: tempRoot.Path,
            cancellationToken);

        Assert.True(exitCode == 0, $"dotnet new console failed: {stderr}");

        var projectPath = Path.Join(tempRoot.Path, "LogTestApp.csproj");
        var programPath = Path.Join(tempRoot.Path, "Program.cs");
        Assert.True(File.Exists(projectPath), $"Expected LogTestApp.csproj to exist at {projectPath}");

        // Modify Program.cs to output identifiable messages
        var programContent = @"
using System;
using System.Threading;

Console.WriteLine(""Line 1: Application started"");
Console.WriteLine(""Line 2: Initializing..."");
Console.Error.WriteLine(""Error: This is an error message"");
Console.WriteLine(""Line 3: Processing..."");

// Run for a while to keep the process alive
Thread.Sleep(TimeSpan.FromSeconds(30));
Console.WriteLine(""Line 4: Application finished"");
";
        File.WriteAllText(programPath, programContent);

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken, _output);

        // Build the project
        var buildText = await client.CallToolTextAsync(
            toolName: "dotnet_project",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Build",
                ["project"] = projectPath,
            },
            cancellationToken);

        Assert.DoesNotContain("Error:", buildText);

        // Start the app in background mode
        var runText = await client.CallToolTextAsync(
            toolName: "dotnet_project",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Run",
                ["project"] = projectPath,
                ["noBuild"] = true,
                ["startMode"] = "Background",
            },
            cancellationToken);

        Assert.Contains("Process started in background mode", runText);

        var sessionId = ParsePrefixedLine(runText, "Session ID: ");
        Assert.NotNull(sessionId);

        try
        {
            var logsText = await WaitForLogsContainingAsync(
                client,
                sessionId!,
                [
                    "Line 1: Application started",
                    "Line 2: Initializing...",
                    "Line 3: Processing...",
                    "[stderr] Error: This is an error message",
                ],
                cancellationToken);

            Assert.StartsWith("Logs for session", logsText);
            Assert.Contains(sessionId!, logsText);

            // Verify logs output contains expected messages
            Assert.Contains("Line 1: Application started", logsText);
            Assert.Contains("Line 2: Initializing...", logsText);
            Assert.Contains("Line 3: Processing...", logsText);
            Assert.Contains("[stderr] Error: This is an error message", logsText);

            // Test tailLines parameter - request only last 2 lines
            var tailLogsText = await client.CallToolTextAsync(
                toolName: "dotnet_project",
                args: new Dictionary<string, object?>
                {
                    ["action"] = "Logs",
                    ["sessionId"] = sessionId,
                    ["tailLines"] = 2,
                },
                cancellationToken);

            Assert.StartsWith("Logs for session", tailLogsText);
            Assert.Contains("Returned", tailLogsText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            // Clean up - stop the background process
            await client.CallToolTextAsync(
                toolName: "dotnet_project",
                args: new Dictionary<string, object?>
                {
                    ["action"] = "Stop",
                    ["sessionId"] = sessionId,
                },
                cancellationToken);
        }
    }

    [ScenarioFact]
    public async Task Scenario_BackgroundRun_Logs_InvalidSessionId_ReturnsError()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var client = await McpScenarioClient.CreateAsync(cancellationToken, _output);

        // Try to retrieve logs for a non-existent session
        var logsText = await client.CallToolTextAsync(
            toolName: "dotnet_project",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Logs",
                ["sessionId"] = "non-existent-session-id",
            },
            cancellationToken);

        Assert.Contains("not found", logsText, StringComparison.OrdinalIgnoreCase);
    }
}
