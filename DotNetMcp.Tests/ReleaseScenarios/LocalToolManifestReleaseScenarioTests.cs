using DotNetMcp;
using DotNetMcp.Tests.Scenarios;
using Xunit;

namespace DotNetMcp.Tests.ReleaseScenarios;

public class LocalToolManifestReleaseScenarioTests
{
    private readonly ITestOutputHelper _output;

    public LocalToolManifestReleaseScenarioTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [ReleaseScenarioFact]
    public async Task ReleaseScenario_DotnetTool_CreateManifest_Install_Restore_List()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var tempRoot = ScenarioHelpers.CreateTempDirectory(nameof(ReleaseScenario_DotnetTool_CreateManifest_Install_Restore_List));

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken, _output);

        var createManifestText = await client.CallToolTextAsync(
            toolName: "dotnet_tool",
            args: new Dictionary<string, object?>
            {
                ["action"] = "CreateManifest",
                ["output"] = tempRoot.Path,
                ["workingDirectory"] = tempRoot.Path
            },
            cancellationToken);

        ScenarioHelpers.AssertSuccess(createManifestText, "dotnet_tool CreateManifest");

        Assert.True(File.Exists(Path.Join(tempRoot.Path, ".config", "dotnet-tools.json")), "Expected local tool manifest to exist.");

        var installText = await client.CallToolTextAsync(
            toolName: "dotnet_tool",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Install",
                ["packageId"] = "dotnet-ef",
                ["global"] = false,
                ["workingDirectory"] = tempRoot.Path
            },
            cancellationToken);

        ScenarioHelpers.AssertSuccess(installText, "dotnet_tool Install dotnet-ef");

        var restoreText = await client.CallToolTextAsync(
            toolName: "dotnet_tool",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Restore",
                ["workingDirectory"] = tempRoot.Path
            },
            cancellationToken);

        ScenarioHelpers.AssertSuccess(restoreText, "dotnet_tool Restore");

        var listText = await client.CallToolTextAsync(
            toolName: "dotnet_tool",
            args: new Dictionary<string, object?>
            {
                ["action"] = "List",
                ["global"] = false,
                ["workingDirectory"] = tempRoot.Path
            },
            cancellationToken);

        ScenarioHelpers.AssertSuccess(listText, "dotnet_tool List");
        Assert.Contains("dotnet-ef", listText, StringComparison.OrdinalIgnoreCase);
    }
}
