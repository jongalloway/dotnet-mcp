using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests.Scenarios;

public class SecretsRedactionScenarioTests
{
    private readonly ITestOutputHelper _output;

    public SecretsRedactionScenarioTests(ITestOutputHelper output)
    {
        _output = output;
    }

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

        var projectPath = Path.Join(tempRoot.Path, "SecretsProj.csproj");
        Assert.True(File.Exists(projectPath), $"Expected SecretsProj.csproj to exist at {projectPath}");

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken, _output);

        // Initialize + set a secret for the main project.
        var initText = await client.CallToolTextAsync(
            toolName: "dotnet_dev_certs",
            args: new Dictionary<string, object?>
            {
                ["action"] = "SecretsInit",
                ["project"] = projectPath,
            },
            cancellationToken);

        Assert.DoesNotContain("Error:", initText);
        ScenarioHelpers.AssertDoesNotContainSecret(initText, "SuperSecret123!");

        var setText = await client.CallToolTextAsync(
            toolName: "dotnet_dev_certs",
            args: new Dictionary<string, object?>
            {
                ["action"] = "SecretsSet",
                ["project"] = projectPath,
                ["key"] = "ConnectionStrings:Default",
                ["value"] = secretValue,
            },
            cancellationToken);

        Assert.DoesNotContain("Error:", setText);

        // The tool should never echo the raw secret value.
        ScenarioHelpers.AssertDoesNotContainSecret(setText, "SuperSecret123!");

        // Now run a command that prints environment/config info.
        var sdkInfoText = await client.CallToolTextAsync(
            toolName: "dotnet_sdk",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Info",
            },
            cancellationToken);

        Assert.DoesNotContain("Error:", sdkInfoText);

        // Confirm secret doesn't appear anywhere.
        ScenarioHelpers.AssertDoesNotContainSecret(sdkInfoText, "SuperSecret123!");
    }
}
