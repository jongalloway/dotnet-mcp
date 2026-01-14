using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace DotNetMcp.Tests.Scenarios;

public class BackgroundRunScenarioTests
{
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

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

        // Build the project
        var buildJsonText = await client.CallToolTextAsync(
            toolName: "dotnet_project",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Build",
                ["project"] = projectPath,
                ["machineReadable"] = true
            },
            cancellationToken);

        using (var buildJson = ScenarioHelpers.ParseJson(buildJsonText))
        {
            ScenarioHelpers.AssertMachineReadableSuccess(buildJson.RootElement);
        }

        // Start the app in background mode with noBuild=true
        var runJsonText = await client.CallToolTextAsync(
            toolName: "dotnet_project",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Run",
                ["project"] = projectPath,
                ["noBuild"] = true,
                ["startMode"] = "Background",
                ["machineReadable"] = true
            },
            cancellationToken);

        using var runJson = ScenarioHelpers.ParseJson(runJsonText);
        ScenarioHelpers.AssertMachineReadableSuccess(runJson.RootElement);

        // Verify metadata is present
        Assert.True(runJson.RootElement.TryGetProperty("metadata", out var metadata));
        Assert.True(metadata.TryGetProperty("sessionId", out var sessionIdElement));
        Assert.True(metadata.TryGetProperty("pid", out var pidElement));
        Assert.True(metadata.TryGetProperty("operationType", out var operationTypeElement));
        Assert.True(metadata.TryGetProperty("target", out var targetElement));
        Assert.True(metadata.TryGetProperty("startMode", out var startModeElement));

        var sessionId = sessionIdElement.GetString();
        var pidString = pidElement.GetString();
        var operationType = operationTypeElement.GetString();
        var startMode = startModeElement.GetString();

        Assert.NotNull(sessionId);
        Assert.NotNull(pidString);
        Assert.Equal("run", operationType);
        Assert.Equal("background", startMode);

        var pid = int.Parse(pidString!);

        // Verify the process is running and stop it
        using (var process = Process.GetProcessById(pid))
        {
            Assert.False(process.HasExited, "Process should still be running");

            // Stop the process using the sessionId
            var stopJsonText = await client.CallToolTextAsync(
                toolName: "dotnet_project",
                args: new Dictionary<string, object?>
                {
                    ["action"] = "Stop",
                    ["sessionId"] = sessionId,
                    ["machineReadable"] = true
                },
                cancellationToken);

            using var stopJson = ScenarioHelpers.ParseJson(stopJsonText);
            ScenarioHelpers.AssertMachineReadableSuccess(stopJson.RootElement);

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
}
