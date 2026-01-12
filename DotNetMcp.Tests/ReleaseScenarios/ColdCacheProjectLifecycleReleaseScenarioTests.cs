using DotNetMcp.Tests.Scenarios;
using Xunit;

namespace DotNetMcp.Tests.ReleaseScenarios;

[Collection("ProcessWideStateTests")]
public class ColdCacheProjectLifecycleReleaseScenarioTests
{
    [ReleaseScenarioFact]
    public async Task ReleaseScenario_ColdNuGetCache_Console_AddPackage_Restore_Build_Publish()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var tempRoot = ScenarioHelpers.CreateTempDirectory(nameof(ReleaseScenario_ColdNuGetCache_Console_AddPackage_Restore_Build_Publish));

        var nugetPackagesDir = Path.Join(tempRoot.Path, ".nuget", "packages");
        var dotnetCliHomeDir = Path.Join(tempRoot.Path, ".dotnet", "home");
        Directory.CreateDirectory(nugetPackagesDir);
        Directory.CreateDirectory(dotnetCliHomeDir);

        var previousNugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        var previousDotnetCliHome = Environment.GetEnvironmentVariable("DOTNET_CLI_HOME");

        try
        {
            // Isolate NuGet + dotnet CLI state to ensure we are exercising a cold cache.
            Environment.SetEnvironmentVariable("NUGET_PACKAGES", nugetPackagesDir);
            Environment.SetEnvironmentVariable("DOTNET_CLI_HOME", dotnetCliHomeDir);

            // Create console project via CLI (keeps scenario stable).
            var (exitCode, _, stderr) = await ScenarioHelpers.RunDotNetAsync(
                $"new console -n App -o \"{tempRoot.Path}\"",
                workingDirectory: tempRoot.Path,
                cancellationToken);

            Assert.True(exitCode == 0, $"dotnet new console failed: {stderr}");

            var projectPath = Path.Join(tempRoot.Path, "App.csproj");
            Assert.True(File.Exists(projectPath), $"Expected App.csproj to exist at {projectPath}");

            await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

            // Add a small public package to force a real restore against NuGet.
            var addPackageJsonText = await client.CallToolTextAsync(
                toolName: "dotnet_package",
                args: new Dictionary<string, object?>
                {
                    ["action"] = "Add",
                    ["project"] = projectPath,
                    ["packageId"] = "Humanizer",
                    ["version"] = "2.14.1",
                    ["source"] = "https://api.nuget.org/v3/index.json",
                    ["machineReadable"] = true
                },
                cancellationToken);

            using (var addPackageJson = ScenarioHelpers.ParseJson(addPackageJsonText))
            {
                ScenarioHelpers.AssertMachineReadableSuccess(addPackageJson.RootElement);
            }

            // Restore via MCP.
            var restoreJsonText = await client.CallToolTextAsync(
                toolName: "dotnet_project",
                args: new Dictionary<string, object?>
                {
                    ["action"] = "Restore",
                    ["project"] = projectPath,
                    ["machineReadable"] = true
                },
                cancellationToken);

            using (var restoreJson = ScenarioHelpers.ParseJson(restoreJsonText))
            {
                ScenarioHelpers.AssertMachineReadableSuccess(restoreJson.RootElement);
            }

            // Build via MCP.
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

            using (var buildJson = ScenarioHelpers.ParseJson(buildJsonText))
            {
                ScenarioHelpers.AssertMachineReadableSuccess(buildJson.RootElement);
            }

            // Publish via MCP and validate output exists.
            var publishDir = Path.Join(tempRoot.Path, "publish");
            Directory.CreateDirectory(publishDir);

            var publishJsonText = await client.CallToolTextAsync(
                toolName: "dotnet_project",
                args: new Dictionary<string, object?>
                {
                    ["action"] = "Publish",
                    ["project"] = projectPath,
                    ["configuration"] = "Release",
                    ["output"] = publishDir,
                    ["noBuild"] = true,
                    ["machineReadable"] = true
                },
                cancellationToken);

            using (var publishJson = ScenarioHelpers.ParseJson(publishJsonText))
            {
                ScenarioHelpers.AssertMachineReadableSuccess(publishJson.RootElement);
            }

            Assert.True(File.Exists(Path.Join(publishDir, "App.dll")), "Expected published App.dll to exist.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("NUGET_PACKAGES", previousNugetPackages);
            Environment.SetEnvironmentVariable("DOTNET_CLI_HOME", previousDotnetCliHome);
        }
    }
}
