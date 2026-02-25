using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests.Scenarios;

public class TemplateSearchScenarioTests
{
    [ScenarioFact]
    public async Task Scenario_DotnetSdk_SearchTemplates_Console_MachineReadable_Success()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

        var text = await client.CallToolTextAsync(
            toolName: "dotnet_sdk",
            args: new Dictionary<string, object?>
            {
                ["action"] = "SearchTemplates",
                ["searchTerm"] = "console",
            },
            cancellationToken);

        Assert.NotEmpty(text);
        Assert.DoesNotContain("Error:", text);
        Assert.Contains("console", text, StringComparison.OrdinalIgnoreCase);
    }
}
