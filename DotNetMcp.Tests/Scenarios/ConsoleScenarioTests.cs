using System.Text.Json;
using Xunit;

namespace DotNetMcp.Tests.Scenarios;

public class ConsoleScenarioTests
{
    [ScenarioFact]
    public async Task Scenario_ConsoleProject_AddPackageAndBuild_Release()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var tempRoot = ScenarioHelpers.CreateTempDirectory(nameof(Scenario_ConsoleProject_AddPackageAndBuild_Release));

        // Create console project via CLI (keeps scenario stable even if template enumeration/validation is flaky).
        var (exitCode, _, stderr) = await ScenarioHelpers.RunDotNetAsync(
            $"new console -n ConsoleApp -o \"{tempRoot}\"",
            workingDirectory: tempRoot,
            cancellationToken);

        Assert.True(exitCode == 0, $"dotnet new console failed: {stderr}");

        var projectPath = Path.Combine(tempRoot, "ConsoleApp.csproj");
        Assert.True(File.Exists(projectPath), "Expected ConsoleApp.csproj to exist");

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

        // Add a package via MCP.
        var addPackageJsonText = await client.CallToolTextAsync(
            toolName: "dotnet_package",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Add",
                ["project"] = projectPath,
                ["packageId"] = "Aspire.Hosting",
                ["version"] = "13.1.0",
                ["source"] = "https://api.nuget.org/v3/index.json",
                ["machineReadable"] = true
            },
            cancellationToken);

        using var addPackageJson = ScenarioHelpers.ParseJson(addPackageJsonText);
        ScenarioHelpers.AssertMachineReadableSuccess(addPackageJson.RootElement);

        // Build via MCP in Release.
        var buildJsonText = await client.CallToolTextAsync(
            toolName: "dotnet_project",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Build",
                ["project"] = projectPath,
                ["configuration"] = "Release",
                ["noRestore"] = true,
                ["machineReadable"] = true
            },
            cancellationToken);

        using var buildJson = ScenarioHelpers.ParseJson(buildJsonText);
        ScenarioHelpers.AssertMachineReadableSuccess(buildJson.RootElement);
    }
}
