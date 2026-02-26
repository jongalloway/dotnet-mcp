using DotNetMcp;
using DotNetMcp.Tests.Scenarios;
using Xunit;

namespace DotNetMcp.Tests.ReleaseScenarios;

public class ServerConcurrencyStressReleaseScenarioTests
{
    [ReleaseScenarioFact]
    public async Task ReleaseScenario_ServerConcurrency_ManyParallelSdkInfoCalls()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

        const int callCount = 64;

        var tasks = Enumerable.Range(0, callCount)
            .Select(_ => client.CallToolTextAsync(
                toolName: "dotnet_sdk",
                args: new Dictionary<string, object?>
                {
                    ["action"] = "Info"
                },
                cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        Assert.Equal(callCount, results.Length);

        foreach (var result in results)
        {
            ScenarioHelpers.AssertSuccess(result, "dotnet_sdk Info");
        }
    }
}
