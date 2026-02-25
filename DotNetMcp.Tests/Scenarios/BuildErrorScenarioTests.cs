using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests.Scenarios;

public class BuildErrorScenarioTests
{
    [ScenarioFact]
    public async Task Scenario_DotnetProject_Build_WithCompileError_ReturnsMachineReadableError()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var tempRoot = ScenarioHelpers.CreateTempDirectory(nameof(Scenario_DotnetProject_Build_WithCompileError_ReturnsMachineReadableError));

        // Create a throwaway project via CLI to avoid mutating repo projects.
        var (newExit, _, newErr) = await ScenarioHelpers.RunDotNetAsync(
            $"new console -n BrokenApp -o \"{tempRoot.Path}\"",
            workingDirectory: tempRoot.Path,
            cancellationToken);

        Assert.True(newExit == 0, $"dotnet new console failed: {newErr}");

        var projectPath = Directory.GetFiles(tempRoot.Path, "*.csproj", SearchOption.AllDirectories)
            .Single();
        Assert.True(File.Exists(projectPath), "Expected a .csproj to exist");

        var projectDir = Path.GetDirectoryName(projectPath);
        Assert.False(string.IsNullOrWhiteSpace(projectDir));

        // Introduce a compile error.
        var programPath = Path.Join(projectDir!, "Program.cs");
        Assert.True(File.Exists(programPath), "Expected Program.cs to exist");
        await File.AppendAllTextAsync(programPath, "\nthis_will_not_compile\n", cancellationToken);

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

        var text = await client.CallToolTextAsync(
            toolName: "dotnet_project",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Build",
                ["project"] = projectPath,
                ["configuration"] = "Release",
            },
            cancellationToken);

        // Contract sanity: output should mention a build/compile failure.
        Assert.Contains("error", text, StringComparison.OrdinalIgnoreCase);
    }
}
