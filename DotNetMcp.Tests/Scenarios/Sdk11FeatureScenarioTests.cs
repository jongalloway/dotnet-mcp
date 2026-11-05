using System.Text.Json;

namespace DotNetMcp.Tests.Scenarios;

public sealed class Sdk11FeatureScenarioTests(ITestOutputHelper output)
{
    [ScenarioFact]
    public async Task NewTestTemplates_ExposeMtpOptionsThroughMcp()
    {
        using var root = await Sdk11FeatureTests.CreateFixtureAsync();
        await using var client = await McpScenarioClient.CreateAsync(TestContext.Current.CancellationToken, output);
        foreach (var (template, options, expectedPackage) in new[]
        {
            ("xunit", "--xunit-version v3 --no-restore", "xunit.v3"),
            ("nunit", "--test-runner Microsoft.Testing.Platform --no-restore", "NUnit3TestAdapter")
        })
        {
            var destination = Path.Join(root.Path, template);
            var result = await client.CallToolAsync("dotnet_project", new()
            {
                ["action"] = "New", ["template"] = template, ["name"] = "GeneratedTests",
                ["output"] = destination, ["framework"] = "net11.0", ["additionalOptions"] = options,
                ["workingDirectory"] = root.Path
            }, TestContext.Current.CancellationToken);
            Assert.True(result.IsError != true, result.GetText());
            var project = await File.ReadAllTextAsync(Path.Join(destination, "GeneratedTests.csproj"), TestContext.Current.CancellationToken);
            Assert.Contains(expectedPackage, project);
            Assert.Contains("TestingPlatform", project);
            using var global = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Join(root.Path, "global.json"), TestContext.Current.CancellationToken));
            Assert.Equal("Microsoft.Testing.Platform", global.RootElement.GetProperty("test").GetProperty("runner").GetString());
        }
    }

    [ScenarioFact]
    public async Task RunEnvironment_IsAcceptedByStdioSchemaAndReachesApp()
    {
        using var root = await Sdk11FeatureTests.CreateFixtureAsync();
        var project = await Sdk11FeatureTests.WriteConsoleAsync(root.Path,
            """Console.WriteLine("MCP_ENV:" + Environment.GetEnvironmentVariable("MCP_FIXTURE"));""");
        await using var client = await McpScenarioClient.CreateAsync(TestContext.Current.CancellationToken, output);
        var result = await client.CallToolAsync("dotnet_project", new()
        {
            ["action"] = "Run", ["project"] = project, ["workingDirectory"] = root.Path,
            ["environmentVariables"] = new Dictionary<string, string> { ["MCP_FIXTURE"] = "space = \"quote\"" }
        }, TestContext.Current.CancellationToken);
        Assert.True(result.IsError != true, result.GetText());
        Assert.Contains("MCP_ENV:space = \"quote\"", result.GetText());
        Assert.NotNull(result.StructuredContent);
        var tools = await client.Client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var environment = tools.Single(tool => tool.Name == "dotnet_project").ProtocolTool.InputSchema
            .GetProperty("properties").GetProperty("environmentVariables");
        Assert.Contains("string", environment.GetRawText());
    }

    [ScenarioFact]
    public async Task SolutionFilter_CreateAndMembershipRoundTripThroughStdio()
    {
        using var root = await Sdk11FeatureTests.CreateFixtureAsync();
        var project = await Sdk11FeatureTests.WriteConsoleAsync(root.Path);
        var parent = Path.Join(root.Path, "Parent.slnx");
        var filterDirectory = Directory.CreateDirectory(Path.Join(root.Path, "filters")).FullName;
        var filter = Path.Join(filterDirectory, "Subset.slnf");
        await using var client = await McpScenarioClient.CreateAsync(TestContext.Current.CancellationToken, output);
        foreach (var args in new Dictionary<string, object?>[]
        {
            new() { ["action"] = "Create", ["name"] = "Parent", ["output"] = root.Path, ["format"] = "slnx" },
            new() { ["action"] = "Add", ["solution"] = parent, ["projects"] = new[] { project } },
            new() { ["action"] = "Create", ["name"] = "Subset", ["output"] = filterDirectory,
                ["format"] = "slnf", ["parentSolution"] = Path.Join("..", "Parent.slnx") },
            new() { ["action"] = "Add", ["solution"] = filter, ["projects"] = new[] { project } }
        })
        {
            var result = await client.CallToolAsync("dotnet_solution", args, TestContext.Current.CancellationToken);
            Assert.True(result.IsError != true, result.GetText());
        }
        var listed = await client.CallToolAsync("dotnet_solution", new()
        {
            ["action"] = "List", ["solution"] = filter
        }, TestContext.Current.CancellationToken);
        Assert.True(listed.IsError != true, listed.GetText());
        Assert.Contains("App.csproj", listed.GetText());
        Assert.NotNull(listed.StructuredContent);
        var removed = await client.CallToolAsync("dotnet_solution", new()
        {
            ["action"] = "Remove", ["solution"] = filter, ["projects"] = new[] { project }
        }, TestContext.Current.CancellationToken);
        Assert.True(removed.IsError != true, removed.GetText());
        using var content = JsonDocument.Parse(await File.ReadAllTextAsync(filter, TestContext.Current.CancellationToken));
        Assert.Empty(content.RootElement.GetProperty("solution").GetProperty("projects").EnumerateArray());
    }
}
