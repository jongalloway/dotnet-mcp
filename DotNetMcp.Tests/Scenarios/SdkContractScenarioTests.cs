using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests.Scenarios;

public class SdkContractScenarioTests
{
    [ScenarioFact]
    public async Task Scenario_DotnetSdk_ListSdks_MachineReadable_Success()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

        var text = await client.CallToolTextAsync(
            toolName: "dotnet_sdk",
            args: new Dictionary<string, object?>
            {
                ["action"] = "ListSdks",
            },
            cancellationToken);

        Assert.NotEmpty(text);
        Assert.DoesNotContain("Error:", text);
        Assert.Matches(@"\d+\.\d+", text);
    }

    [ScenarioFact]
    public async Task Scenario_DotnetSdk_SearchTemplates_MissingSearchTerm_ReturnsValidationError()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

        var text = await client.CallToolTextAsync(
            toolName: "dotnet_sdk",
            args: new Dictionary<string, object?>
            {
                ["action"] = "SearchTemplates",
            },
            cancellationToken);

        Assert.Contains("searchTerm", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("required", text, StringComparison.OrdinalIgnoreCase);
    }

    [ScenarioFact]
    public async Task Scenario_DotnetSdk_ListTemplatePacks_MachineReadable_Success()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

        var text = await client.CallToolTextAsync(
            toolName: "dotnet_sdk",
            args: new Dictionary<string, object?>
            {
                ["action"] = "ListTemplatePacks",
            },
            cancellationToken);

        Assert.NotEmpty(text);
        Assert.DoesNotContain("Error:", text);
        Assert.Contains("dotnet new uninstall", text);
    }
}
