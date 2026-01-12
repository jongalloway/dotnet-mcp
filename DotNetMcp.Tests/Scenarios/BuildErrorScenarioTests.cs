using System.Text.Json;
using Xunit;

namespace DotNetMcp.Tests.Scenarios;

public class BuildErrorScenarioTests
{
    [ScenarioFact]
    public async Task Scenario_DotnetProject_Build_WithCompileError_ReturnsMachineReadableError()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var tempRoot = ScenarioHelpers.CreateTempDirectory(nameof(Scenario_DotnetProject_Build_WithCompileError_ReturnsMachineReadableError));

        // Create a throwaway project via CLI to avoid mutating repo projects.
        var (newExit, _, newErr) = await ScenarioHelpers.RunDotNetAsync(
            $"new console -n BrokenApp -o \"{tempRoot}\"",
            workingDirectory: tempRoot,
            cancellationToken);

        Assert.True(newExit == 0, $"dotnet new console failed: {newErr}");

        var projectPath = Directory.GetFiles(tempRoot, "*.csproj", SearchOption.AllDirectories)
            .Single();
        Assert.True(File.Exists(projectPath), "Expected a .csproj to exist");

        var projectDir = Path.GetDirectoryName(projectPath);
        Assert.False(string.IsNullOrWhiteSpace(projectDir));

        // Introduce a compile error.
        var programPath = Path.Combine(projectDir!, "Program.cs");
        Assert.True(File.Exists(programPath), "Expected Program.cs to exist");
        await File.AppendAllTextAsync(programPath, "\nthis_will_not_compile\n", cancellationToken);

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

        var jsonText = await client.CallToolTextAsync(
            toolName: "dotnet_project",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Build",
                ["project"] = projectPath,
                ["configuration"] = "Release",
                ["machineReadable"] = true
            },
            cancellationToken);

        using var json = ScenarioHelpers.ParseJson(jsonText);
        ScenarioHelpers.AssertMachineReadableFailure(json.RootElement);

        Assert.True(json.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array);
        Assert.NotEqual(0, errors.GetArrayLength());

        // Contract sanity: error payload should mention a build/compile failure.
        var errorsText = errors.GetRawText();
        Assert.Contains("error", errorsText, StringComparison.OrdinalIgnoreCase);
    }
}
