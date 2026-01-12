using System.Text.Json;
using Xunit;

namespace DotNetMcp.Tests.Scenarios;

public class SecretsRedactionScenarioTests
{
    [ScenarioFact]
    public async Task Scenario_UserSecrets_NoSecretLeak_InOtherToolOutputs()
    {
        var secretValue = "Server=localhost;Password=SuperSecret123!";
        var cancellationToken = TestContext.Current.CancellationToken;

        using var tempRoot = ScenarioHelpers.CreateTempDirectory(nameof(Scenario_UserSecrets_NoSecretLeak_InOtherToolOutputs));

        // Create a throwaway project so we don't mutate repo project files (SecretsInit writes UserSecretsId).
        var (exitCode, _, stderr) = await ScenarioHelpers.RunDotNetAsync(
            $"new classlib -n SecretsProj -o \"{tempRoot.Path}\"",
            workingDirectory: tempRoot.Path,
            cancellationToken);

        Assert.True(exitCode == 0, $"dotnet new failed: {stderr}");

        var projectPath = Path.Combine(tempRoot.Path, "SecretsProj.csproj");
        Assert.True(File.Exists(projectPath), $"Expected SecretsProj.csproj to exist at {projectPath}");

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

        // Initialize + set a secret for the main project.
        var initJsonText = await client.CallToolTextAsync(
            toolName: "dotnet_dev_certs",
            args: new Dictionary<string, object?>
            {
                ["action"] = "SecretsInit",
                ["project"] = projectPath,
                ["machineReadable"] = true
            },
            cancellationToken);

        using var initJson = ScenarioHelpers.ParseJson(initJsonText);
        ScenarioHelpers.AssertMachineReadableSuccess(initJson.RootElement);
        ScenarioHelpers.AssertDoesNotContainSecret(initJsonText, "SuperSecret123!");

        var setJsonText = await client.CallToolTextAsync(
            toolName: "dotnet_dev_certs",
            args: new Dictionary<string, object?>
            {
                ["action"] = "SecretsSet",
                ["project"] = projectPath,
                ["key"] = "ConnectionStrings:Default",
                ["value"] = secretValue,
                ["machineReadable"] = true
            },
            cancellationToken);

        using var setJson = ScenarioHelpers.ParseJson(setJsonText);
        ScenarioHelpers.AssertMachineReadableSuccess(setJson.RootElement);

        // The tool should never echo the raw secret value.
        ScenarioHelpers.AssertDoesNotContainSecret(setJsonText, "SuperSecret123!");

        // Now run a command that prints environment/config info.
        var sdkInfoJsonText = await client.CallToolTextAsync(
            toolName: "dotnet_sdk",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Info",
                ["machineReadable"] = true
            },
            cancellationToken);

        using var sdkInfoJson = ScenarioHelpers.ParseJson(sdkInfoJsonText);
        ScenarioHelpers.AssertMachineReadableSuccess(sdkInfoJson.RootElement);

        // Confirm secret doesn't appear anywhere.
        ScenarioHelpers.AssertDoesNotContainSecret(sdkInfoJsonText, "SuperSecret123!");
    }
}
