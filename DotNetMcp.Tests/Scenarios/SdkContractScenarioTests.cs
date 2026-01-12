using System.Text.Json;
using Xunit;

namespace DotNetMcp.Tests.Scenarios;

public class SdkContractScenarioTests
{
    [ScenarioFact]
    public async Task Scenario_DotnetSdk_ListSdks_MachineReadable_Success()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

        var jsonText = await client.CallToolTextAsync(
            toolName: "dotnet_sdk",
            args: new Dictionary<string, object?>
            {
                ["action"] = "ListSdks",
                ["machineReadable"] = true
            },
            cancellationToken);

        using var json = ScenarioHelpers.ParseJson(jsonText);
        ScenarioHelpers.AssertMachineReadableSuccess(json.RootElement);
    }

    [ScenarioFact]
    public async Task Scenario_DotnetSdk_SearchTemplates_MissingSearchTerm_ReturnsValidationError()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

        var jsonText = await client.CallToolTextAsync(
            toolName: "dotnet_sdk",
            args: new Dictionary<string, object?>
            {
                ["action"] = "SearchTemplates",
                ["machineReadable"] = true
            },
            cancellationToken);

        using var json = ScenarioHelpers.ParseJson(jsonText);
        ScenarioHelpers.AssertMachineReadableFailure(json.RootElement);

        Assert.True(json.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array);
        Assert.NotEqual(0, errors.GetArrayLength());

        var found = false;
        foreach (var error in errors.EnumerateArray())
        {
            if (!error.TryGetProperty("code", out var code) || code.GetString() != "INVALID_PARAMS")
            {
                continue;
            }

            if (!error.TryGetProperty("category", out var category) || category.GetString() != "Validation")
            {
                continue;
            }

            if (!error.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!data.TryGetProperty("additionalData", out var additionalData) || additionalData.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!additionalData.TryGetProperty("parameter", out var parameter))
            {
                continue;
            }

            if (!string.Equals(parameter.GetString(), "searchTerm", StringComparison.Ordinal))
            {
                continue;
            }

            // Optional but contract-relevant: missing required parameter.
            Assert.True(additionalData.TryGetProperty("reason", out var reason));
            Assert.Equal("required", reason.GetString());

            found = true;
            break;
        }

        Assert.True(found, "Expected a Validation/INVALID_PARAMS error with data.additionalData.parameter == 'searchTerm'.");
    }
}
