using System.Text.Json;
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

        // Introduce a compile error: reference an unresolved type so we get a CS0246 diagnostic.
        var programPath = Path.Join(projectDir!, "Program.cs");
        Assert.True(File.Exists(programPath), "Expected Program.cs to exist");
        await File.WriteAllTextAsync(programPath, "IAmABogusType bogus = new IAmABogusType();\n", cancellationToken);

        await using var client = await McpScenarioClient.CreateAsync(cancellationToken);

        var result = await client.CallToolAsync(
            toolName: "dotnet_project",
            args: new Dictionary<string, object?>
            {
                ["action"] = "Build",
                ["project"] = projectPath,
                ["configuration"] = "Release",
            },
            cancellationToken);

        // Text content sanity: output should mention a build/compile failure.
        var text = result.GetText();
        Assert.Contains("error", text, StringComparison.OrdinalIgnoreCase);

        // Structured content: verify BuildResult is present and includes compiler error details.
        Assert.True(result.StructuredContent.HasValue, "Expected structured content in Build response");
        var structuredJson = result.StructuredContent!.Value.GetRawText();
        Assert.False(string.IsNullOrWhiteSpace(structuredJson));

        using var doc = JsonDocument.Parse(structuredJson);
        var root = doc.RootElement;

        // success should be false
        Assert.True(root.TryGetProperty("success", out var successProp));
        Assert.False(successProp.GetBoolean());

        // errorCount should be > 0
        Assert.True(root.TryGetProperty("errorCount", out var errorCountProp));
        Assert.True(errorCountProp.GetInt32() > 0, "Expected at least one error in errorCount");

        // errors array should be present and non-empty
        Assert.True(root.TryGetProperty("errors", out var errorsProp), "Expected 'errors' array in BuildResult");
        Assert.Equal(JsonValueKind.Array, errorsProp.ValueKind);
        Assert.True(errorsProp.GetArrayLength() > 0, "Expected at least one entry in 'errors' array");

        // Each diagnostic should have file, line, column, code, message
        foreach (var diagnostic in errorsProp.EnumerateArray())
        {
            Assert.True(diagnostic.TryGetProperty("code", out var codeProp), "Diagnostic missing 'code'");
            Assert.False(string.IsNullOrWhiteSpace(codeProp.GetString()), "Diagnostic 'code' should not be empty");

            Assert.True(diagnostic.TryGetProperty("message", out var msgProp), "Diagnostic missing 'message'");
            Assert.False(string.IsNullOrWhiteSpace(msgProp.GetString()), "Diagnostic 'message' should not be empty");

            // file/line/column should be present for Roslyn compiler errors
            Assert.True(diagnostic.TryGetProperty("file", out var fileProp), "Diagnostic missing 'file'");
            Assert.False(string.IsNullOrWhiteSpace(fileProp.GetString()), "Diagnostic 'file' should not be empty");

            Assert.True(diagnostic.TryGetProperty("line", out var lineProp), "Diagnostic missing 'line'");
            Assert.True(lineProp.GetInt32() > 0, "Diagnostic 'line' should be > 0");

            Assert.True(diagnostic.TryGetProperty("column", out var colProp), "Diagnostic missing 'column'");
            Assert.True(colProp.GetInt32() > 0, "Diagnostic 'column' should be > 0");
        }
    }
}
