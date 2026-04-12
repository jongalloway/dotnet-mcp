using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests.Scenarios;

public class ConsoleScenarioTests
{
    private readonly ITestOutputHelper _output;

    public ConsoleScenarioTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [ScenarioFact]
    public async Task Scenario_ConsoleProject_AddPackageAndBuild_Release()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var tempRoot = ScenarioHelpers.CreateTempDirectory(nameof(Scenario_ConsoleProject_AddPackageAndBuild_Release));

        // Create console project via CLI (keeps scenario stable even if template enumeration/validation is flaky).
        var (exitCode, _, stderr) = await ScenarioHelpers.RunDotNetAsync(
            $"new console -n ConsoleApp -o \"{tempRoot.Path}\"",
            workingDirectory: tempRoot.Path,
            cancellationToken);

        Assert.True(exitCode == 0, $"dotnet new console failed: {stderr}");

        var projectPath = Path.Join(tempRoot.Path, "ConsoleApp.csproj");
        Assert.True(File.Exists(projectPath), $"Expected ConsoleApp.csproj to exist at {projectPath}");

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken, _output);

        // Add a package via MCP.
        var addPackageText = await client.CallToolTextAsync(
            toolName: "dotnet_package",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Add",
                ["project"] = projectPath,
                ["packageId"] = "Aspire.Hosting",
                ["version"] = "13.1.0",
                ["source"] = "https://api.nuget.org/v3/index.json",
            },
            cancellationToken);

        Assert.DoesNotContain("Error:", addPackageText);

        // Build via MCP in Release.
        var buildText = await client.CallToolTextAsync(
            toolName: "dotnet_project",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Build",
                ["project"] = projectPath,
                ["configuration"] = "Release",
                ["noRestore"] = true,
            },
            cancellationToken);

        Assert.DoesNotContain("Error:", buildText);
    }
}
