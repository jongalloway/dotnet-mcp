using DotNetMcp.Tests.Scenarios;
using Xunit;

namespace DotNetMcp.Tests.ReleaseScenarios;

public class LocalToolManifestReleaseScenarioTests
{
    [ReleaseScenarioFact]
    public async Task ReleaseScenario_DotnetTool_CreateManifest_Install_Restore_List()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var tempRoot = ScenarioHelpers.CreateTempDirectory(nameof(ReleaseScenario_DotnetTool_CreateManifest_Install_Restore_List));

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

        var createManifestText = await client.CallToolTextAsync(
            toolName: "dotnet_tool",
            args: new Dictionary<string, object?>
            {
                ["action"] = "CreateManifest",
                ["output"] = tempRoot.Path,
                ["workingDirectory"] = tempRoot.Path,
                ["machineReadable"] = true
            },
            cancellationToken);

        using (var createManifestJson = ScenarioHelpers.ParseJson(createManifestText))
        {
            ScenarioHelpers.AssertMachineReadableSuccess(createManifestJson.RootElement);
        }

        Assert.True(File.Exists(Path.Combine(tempRoot.Path, ".config", "dotnet-tools.json")), "Expected local tool manifest to exist.");

        var installText = await client.CallToolTextAsync(
            toolName: "dotnet_tool",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Install",
                ["packageId"] = "dotnet-ef",
                ["global"] = false,
                ["workingDirectory"] = tempRoot.Path,
                ["machineReadable"] = true
            },
            cancellationToken);

        using (var installJson = ScenarioHelpers.ParseJson(installText))
        {
            ScenarioHelpers.AssertMachineReadableSuccess(installJson.RootElement);
        }

        var restoreText = await client.CallToolTextAsync(
            toolName: "dotnet_tool",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Restore",
                ["workingDirectory"] = tempRoot.Path,
                ["machineReadable"] = true
            },
            cancellationToken);

        using (var restoreJson = ScenarioHelpers.ParseJson(restoreText))
        {
            ScenarioHelpers.AssertMachineReadableSuccess(restoreJson.RootElement);
        }

        var listText = await client.CallToolTextAsync(
            toolName: "dotnet_tool",
            args: new Dictionary<string, object?>
            {
                ["action"] = "List",
                ["global"] = false,
                ["workingDirectory"] = tempRoot.Path,
                ["machineReadable"] = true
            },
            cancellationToken);

        using (var listJson = ScenarioHelpers.ParseJson(listText))
        {
            ScenarioHelpers.AssertMachineReadableSuccess(listJson.RootElement);
            Assert.True(listJson.RootElement.TryGetProperty("output", out var output));
            Assert.Contains("dotnet-ef", output.GetString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
