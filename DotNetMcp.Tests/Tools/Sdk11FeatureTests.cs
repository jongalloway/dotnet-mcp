using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using DotNetMcp.Actions;
using DotNetMcp.SdkIntegration;
using DotNetMcp.Tests.Scenarios;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetMcp.Tests;

[Collection("ProcessWideStateTests")]
public sealed class Sdk11FeatureTests
{
    private static DotNetCliTools CreateTools(ProcessSessionManager? sessions = null)
        => new(NullLogger<DotNetCliTools>.Instance, new ConcurrencyManager(), sessions ?? new ProcessSessionManager());

    [Theory]
    [InlineData("Microsoft.Testing.Platform", TestRunner.MicrosoftTestingPlatform)]
    [InlineData("mIcRoSoFt.TeStInG.pLaTfOrM", TestRunner.MicrosoftTestingPlatform)]
    [InlineData("VSTest", TestRunner.VSTest)]
    [InlineData("vstest", TestRunner.VSTest)]
    public async Task EnvironmentRunner_OverridesGlobalJsonOnlyOnSupportedSdk(string value, TestRunner expected)
    {
        using var root = await CreateFixtureAsync();
        await File.WriteAllTextAsync(Path.Join(root.Path, "global.json"), """{"test":{"runner":"Microsoft.Testing.Platform"}}""", TestContext.Current.CancellationToken);
        var previous = Environment.GetEnvironmentVariable("DOTNET_TEST_RUNNER");
        try
        {
            Environment.SetEnvironmentVariable("DOTNET_TEST_RUNNER", value);
            Assert.Equal((expected, "DOTNET_TEST_RUNNER"), TestRunnerDetector.DetectTestRunner(root.Path));
            Assert.Equal((TestRunner.MicrosoftTestingPlatform, "global.json"),
                TestRunnerDetector.DetectTestRunner(root.Path, supportsEnvironmentRunner: false));
        }
        finally { Environment.SetEnvironmentVariable("DOTNET_TEST_RUNNER", previous); }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData(" VSTest ")]
    public async Task EnvironmentRunner_UnrecognizedValueFallsThrough(string? value)
    {
        using var root = await CreateFixtureAsync();
        await File.WriteAllTextAsync(Path.Join(root.Path, "global.json"), """{"test":{"runner":"Microsoft.Testing.Platform"}}""", TestContext.Current.CancellationToken);
        var previous = Environment.GetEnvironmentVariable("DOTNET_TEST_RUNNER");
        try
        {
            Environment.SetEnvironmentVariable("DOTNET_TEST_RUNNER", value);
            Assert.Equal((TestRunner.MicrosoftTestingPlatform, "global.json"), TestRunnerDetector.DetectTestRunner(root.Path));
        }
        finally { Environment.SetEnvironmentVariable("DOTNET_TEST_RUNNER", previous); }
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("KEY=VALUE")]
    [InlineData("KEY\n")]
    public async Task Run_InvalidEnvironmentNameIsAnApplicationError(string name)
    {
        foreach (var mode in new[] { StartMode.Foreground, StartMode.Background })
        {
            var result = await CreateTools().DotnetProject(DotnetProjectAction.Run, startMode: mode,
                environmentVariables: new() { [name] = "value" });
            Assert.True(result.IsError);
            Assert.Contains("environmentVariables", result.GetText());
        }
    }

    [Fact]
    public async Task NewParameters_RejectIrrelevantActionsAndInvalidValues()
    {
        var tools = CreateTools();
        Assert.True((await tools.DotnetProject(DotnetProjectAction.Build, environmentVariables: new())).IsError);
        Assert.True((await tools.DotnetSdk(DotnetSdkAction.Version, prerelease: true)).IsError);
        Assert.True((await tools.DotnetSolution(DotnetSolutionAction.List, parentSolution: "Parent.slnx")).IsError);
        Assert.True((await tools.DotnetSolution(DotnetSolutionAction.Create, name: "Filter", format: "slnf")).IsError);
        Assert.True((await tools.DotnetSolution(DotnetSolutionAction.Create, name: "Filter", format: "slnf",
            parentSolution: "Other.slnf")).IsError);
        Assert.True((await tools.DotnetSolution(DotnetSolutionAction.Create, name: "Filter",
            parentSolution: "Parent.slnx")).IsError);
        Assert.True((await tools.DotnetSolution(DotnetSolutionAction.Analyze, solution: "Filter.slnf")).IsError);
        Assert.True((await tools.DotnetProject(DotnetProjectAction.Format, framework: "invalid")).IsError);
        Assert.True((await tools.DotnetProject(DotnetProjectAction.Run,
            environmentVariables: new() { ["KEY"] = "bad\nvalue" })).IsError);
    }

    [Fact]
    public void RunArguments_EscapeEnvironmentBeforeApplicationArguments()
    {
        var arguments = DotNetCliTools.BuildRunArguments(null, null, "--sample", true,
            new() { ["EMPTY"] = "", ["TEXT"] = "a=b \"quoted\" C:\\path\\" }).ToString();
        var parsed = new ProcessStartInfo("dotnet") { Arguments = arguments };
        Assert.Contains("--environment \"EMPTY=\"", parsed.Arguments);
        Assert.Contains("--environment \"TEXT=a=b \\\"quoted\\\" C:\\path\\\\\"", parsed.Arguments);
        Assert.EndsWith(" -- --sample", arguments);
    }

    [Fact]
    public async Task SolutionFilter_RealMembershipAndStructuredList()
    {
        using var root = await CreateFixtureAsync();
        var tools = CreateTools();
        var parent = Path.Join(root.Path, "Parent solution.slnx");
        var project = await WriteConsoleAsync(root.Path);
        Assert.True((await tools.DotnetSolution(DotnetSolutionAction.Create, name: "Parent solution",
            output: root.Path, format: "slnx")).IsError != true);
        Assert.True((await tools.DotnetSolution(DotnetSolutionAction.Add, solution: parent, projects: [project])).IsError != true);
        var filters = Directory.CreateDirectory(Path.Join(root.Path, "filters")).FullName;
        var filter = Path.Join(filters, "Selected projects.slnf");
        var created = await tools.DotnetSolution(DotnetSolutionAction.Create, name: "Selected projects",
            output: filters, format: "slnf", parentSolution: Path.Join("..", "Parent solution.slnx"));
        Assert.True(created.IsError != true, created.GetText());
        using (var json = JsonDocument.Parse(await File.ReadAllTextAsync(filter, TestContext.Current.CancellationToken)))
            Assert.EndsWith("Parent solution.slnx", json.RootElement.GetProperty("solution").GetProperty("path").GetString());
        var added = await tools.DotnetSolution(DotnetSolutionAction.Add, solution: filter, projects: [project]);
        Assert.True(added.IsError != true, added.GetText());
        var listed = await tools.DotnetSolution(DotnetSolutionAction.List, solution: filter);
        Assert.True(listed.IsError != true, listed.GetText());
        Assert.NotNull(listed.StructuredContent);
        Assert.Contains("App.csproj", listed.GetText());
        var removed = await tools.DotnetSolution(DotnetSolutionAction.Remove, solution: filter, projects: [project]);
        Assert.True(removed.IsError != true, removed.GetText());
        using var after = JsonDocument.Parse(await File.ReadAllTextAsync(filter, TestContext.Current.CancellationToken));
        Assert.True(after.RootElement.GetProperty("solution").GetProperty("projects").GetArrayLength() == 0,
            removed.GetText() + "\n" + after.RootElement.GetRawText());
    }

    [Fact]
    public async Task SolutionFilter_InvalidRemovalIsUnchangedAndSuccessfulRemovalPreservesMetadata()
    {
        using var root = await CreateFixtureAsync();
        var tools = CreateTools();
        var parent = Path.Join(root.Path, "Parent.slnx");
        await File.WriteAllTextAsync(parent, "<Solution />", TestContext.Current.CancellationToken);
        var filter = Path.Join(root.Path, "Subset.slnf");
        const string original = """
            {"custom":"preserve","solution":{"path":"Parent.slnx","custom":42,"projects":["App.csproj"]}}
            """;
        await File.WriteAllTextAsync(filter, original, TestContext.Current.CancellationToken);
        var invalid = await tools.DotnetSolution(DotnetSolutionAction.Remove, solution: filter,
            projects: [Path.Join(root.Path, "App.csproj"), Path.Join(root.Path, "Missing.csproj")]);
        Assert.True(invalid.IsError);
        Assert.Equal(original, await File.ReadAllTextAsync(filter, TestContext.Current.CancellationToken));
        var valid = await tools.DotnetSolution(DotnetSolutionAction.Remove, solution: filter,
            projects: [Path.Join(root.Path, "App.csproj")]);
        Assert.True(valid.IsError != true, valid.GetText());
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(filter, TestContext.Current.CancellationToken));
        Assert.Equal("preserve", json.RootElement.GetProperty("custom").GetString());
        Assert.Equal(42, json.RootElement.GetProperty("solution").GetProperty("custom").GetInt32());
        Assert.Empty(Directory.GetFiles(root.Path, "*.tmp"));
        Assert.True((await tools.DotnetSolution(DotnetSolutionAction.Create, name: "Broken", output: root.Path,
            format: "slnf", parentSolution: "Missing.slnx")).IsError);
        Assert.False(File.Exists(Path.Join(root.Path, "Broken.slnf")));
    }

    [Fact]
    public async Task RunEnvironment_ReachesForegroundAndBackgroundAppWithoutChangingServer()
    {
        using var root = await CreateFixtureAsync();
        var project = await WriteConsoleAsync(root.Path, """
            Console.WriteLine("TEXT:" + Environment.GetEnvironmentVariable("MCP_FIXTURE_TEXT"));
            Console.WriteLine("EMPTY:" + Environment.GetEnvironmentVariable("MCP_FIXTURE_EMPTY"));
            Console.WriteLine("ARG:" + args[0]);
            if (args.Length > 1) await Task.Delay(TimeSpan.FromMinutes(1));
            """);
        var variables = new Dictionary<string, string> { ["MCP_FIXTURE_TEXT"] = "a=b \"quoted\" C:\\path\\", ["MCP_FIXTURE_EMPTY"] = "" };
        var previous = Environment.GetEnvironmentVariable("MCP_FIXTURE_TEXT");
        var sessions = new ProcessSessionManager();
        var tools = CreateTools(sessions);
        try
        {
            var foreground = await tools.DotnetProject(DotnetProjectAction.Run, project: project,
                workingDirectory: root.Path, appArgs: "sample", environmentVariables: variables);
            Assert.True(foreground.IsError != true, foreground.GetText());
            Assert.Contains("TEXT:" + variables["MCP_FIXTURE_TEXT"], foreground.GetText());
            Assert.Contains("ARG:sample", foreground.GetText());
            var background = await tools.DotnetProject(DotnetProjectAction.Run, project: project,
                workingDirectory: root.Path, noBuild: true, appArgs: "sample wait", startMode: StartMode.Background,
                environmentVariables: variables);
            Assert.True(background.IsError != true, background.GetText());
            var id = background.GetText().Split('\n').Single(line => line.StartsWith("Session ID:")).Split(':')[1].Trim();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(20));
            while (true)
            {
                var logs = (await tools.DotnetProject(DotnetProjectAction.Logs, sessionId: id)).GetText();
                if (logs.Contains("TEXT:" + variables["MCP_FIXTURE_TEXT"], StringComparison.Ordinal)
                    && logs.Contains("ARG:sample", StringComparison.Ordinal))
                    break;
                await Task.Delay(100, deadline.Token);
            }
            Assert.True((await tools.DotnetProject(DotnetProjectAction.Stop, sessionId: id)).IsError != true);
            Assert.Equal(previous, Environment.GetEnvironmentVariable("MCP_FIXTURE_TEXT"));
        }
        finally { sessions.Clear(); }
    }

    [Fact]
    public async Task FormatFramework_LoadsOnlySelectedTarget()
    {
        using var root = await CreateFixtureAsync();
        var project = await WriteConsoleAsync(root.Path, "Console.WriteLine(   \"ok\" );");
        var xml = await File.ReadAllTextAsync(project, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(project, xml.Replace("<TargetFramework>net10.0</TargetFramework>",
            "<TargetFrameworks>net10.0;net11.0</TargetFrameworks>").Replace("</Project>", """
            <ItemGroup><Compile Remove="OnlyNet11.cs" /></ItemGroup>
            <ItemGroup Condition="'$(TargetFramework)' == 'net11.0'"><Compile Include="OnlyNet11.cs" /></ItemGroup>
            </Project>
            """), TestContext.Current.CancellationToken);
        var otherTarget = Path.Join(root.Path, "OnlyNet11.cs");
        const string unformatted = "class OnlyNet11 { public int Value=>   42; }";
        await File.WriteAllTextAsync(otherTarget, unformatted, TestContext.Current.CancellationToken);
        var result = await CreateTools().DotnetProject(DotnetProjectAction.Format, project: project,
            workingDirectory: root.Path, framework: "net10.0");
        Assert.True(result.IsError != true, result.GetText());
        Assert.Contains("--framework net10.0", result.GetText());
        Assert.DoesNotContain("   ", await File.ReadAllTextAsync(Path.Join(root.Path, "Program.cs"), TestContext.Current.CancellationToken));
        Assert.Equal(unformatted, await File.ReadAllTextAsync(otherTarget, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PrereleaseTemplates_SelectLatestAndPreserveExplicitVersion()
    {
        using var root = await CreateFixtureAsync();
        var feed = Directory.CreateDirectory(Path.Join(root.Path, "feed")).FullName;
        var id = "Mcp.Feature.Templates." + Guid.NewGuid().ToString("N");
        foreach (var version in new[] { "1.0.0", "2.0.0-preview.1" })
        {
            using var archive = ZipFile.Open(Path.Join(feed, $"{id}.{version}.nupkg"), ZipArchiveMode.Create);
            WriteEntry(archive, $"{id}.nuspec", $"""
                <package><metadata><id>{id}</id><version>{version}</version><authors>Test</authors>
                <description>Local template fixture</description><packageTypes><packageType name="Template" /></packageTypes>
                </metadata></package>
                """);
            WriteEntry(archive, "content/.template.config/template.json", $$"""
                {"$schema":"http://json.schemastore.org/template","author":"Test","identity":"{{id}}",
                "name":"Feature fixture","shortName":"{{id}}","sourceName":"Fixture"}
                """);
            WriteEntry(archive, "content/version.txt", version);
        }
        var tools = CreateTools();
        try
        {
            foreach (var (prerelease, explicitVersion, expected) in new[]
            {
                (false, (string?)null, "1.0.0"),
                (true, (string?)null, "2.0.0-preview.1"),
                (true, (string?)"1.0.0", "1.0.0")
            })
            {
                var result = await tools.DotnetSdk(DotnetSdkAction.InstallTemplatePack, templatePackage: id,
                    templateVersion: explicitVersion, nugetSource: feed, workingDirectory: root.Path, prerelease: prerelease);
                Assert.True(result.IsError != true, result.GetText());
                Assert.Contains(expected, result.GetText());
                var output = Path.Join(root.Path, expected + (prerelease ? "-preview" : "-stable"));
                var generated = await ScenarioHelpers.RunDotNetAsync(
                    $"new {id} -o {CommandLineArgument.Quote(output)} --no-update-check", root.Path, TestContext.Current.CancellationToken);
                Assert.Equal(0, generated.exitCode);
                Assert.Equal(expected, await File.ReadAllTextAsync(Path.Join(output, "version.txt"), TestContext.Current.CancellationToken));
                Assert.True((await tools.DotnetSdk(DotnetSdkAction.UninstallTemplatePack, templatePackage: id,
                    workingDirectory: root.Path)).IsError != true);
            }
        }
        finally
        {
            await tools.DotnetSdk(DotnetSdkAction.UninstallTemplatePack, templatePackage: id, workingDirectory: root.Path);
        }
    }

    [Fact]
    public async Task OlderSdk_RejectsNewOptInsAndIgnoresEnvironmentRunner()
    {
        var installed = await ScenarioHelpers.RunDotNetAsync("--list-sdks", AppContext.BaseDirectory, TestContext.Current.CancellationToken);
        var sdk10 = installed.stdout.Split('\n').LastOrDefault(line => line.StartsWith("10.", StringComparison.Ordinal))?.Split(' ')[0];
        if (sdk10 is null) Assert.Skip("Requires an installed .NET 10 SDK.");
        using var root = await CreateFixtureAsync(sdk10);
        var project = await WriteConsoleAsync(root.Path);
        var tools = CreateTools();
        foreach (var result in new[]
        {
            await tools.DotnetProject(DotnetProjectAction.Run, project: project, workingDirectory: root.Path,
                environmentVariables: new() { ["KEY"] = "value" }),
            await tools.DotnetProject(DotnetProjectAction.Format, project: project, workingDirectory: root.Path, framework: "net10.0"),
            await tools.DotnetSdk(DotnetSdkAction.InstallTemplatePack, templatePackage: "Example.Templates",
                workingDirectory: root.Path, prerelease: true)
        })
        {
            Assert.True(result.IsError);
            Assert.Contains("requires .NET SDK 11", result.GetText());
        }

        var previous = Environment.GetEnvironmentVariable("DOTNET_TEST_RUNNER");
        try
        {
            Environment.SetEnvironmentVariable("DOTNET_TEST_RUNNER", "Microsoft.Testing.Platform");
            var vstestProject = Path.Join(root.Path, "Selector.csproj");
            await File.WriteAllTextAsync(vstestProject,
                """<Project><Target Name="VSTest"><Message Importance="high" Text="VSTEST_SELECTED" /></Target></Project>""",
                TestContext.Current.CancellationToken);
            string result;
            var previousDirectory = DotNetCommandExecutor.WorkingDirectoryOverride.Value;
            try
            {
                DotNetCommandExecutor.WorkingDirectoryOverride.Value = root.Path;
                result = await tools.DotnetProjectTest(project: vstestProject, verbosity: "normal");
            }
            finally { DotNetCommandExecutor.WorkingDirectoryOverride.Value = previousDirectory; }
            Assert.Contains("VSTEST_SELECTED", result);
            Assert.DoesNotContain("--project", result);
        }
        finally { Environment.SetEnvironmentVariable("DOTNET_TEST_RUNNER", previous); }
    }

    [Fact]
    public async Task ExplicitRunner_OverridesInheritedRunnerOnlyInChildProcess()
    {
        using var root = await CreateFixtureAsync();
        var project = Path.Join(root.Path, "Selector.csproj");
        await File.WriteAllTextAsync(project,
            """<Project><Target Name="VSTest"><Message Importance="high" Text="VSTEST_SELECTED" /></Target></Project>""",
            TestContext.Current.CancellationToken);
        var previousEnvironment = Environment.GetEnvironmentVariable("DOTNET_TEST_RUNNER");
        var previousDirectory = DotNetCommandExecutor.WorkingDirectoryOverride.Value;
        try
        {
            Environment.SetEnvironmentVariable("DOTNET_TEST_RUNNER", "Microsoft.Testing.Platform");
            DotNetCommandExecutor.WorkingDirectoryOverride.Value = root.Path;
            foreach (var legacy in new[] { false, true })
            {
                var result = await CreateTools().DotnetProjectTest(project: project, verbosity: "normal",
                    testRunner: TestRunner.VSTest, useLegacyProjectArgument: legacy);
                Assert.Contains("VSTEST_SELECTED", result);
                Assert.Contains("Exit Code: 0", result);
                Assert.Equal("Microsoft.Testing.Platform", Environment.GetEnvironmentVariable("DOTNET_TEST_RUNNER"));
                Assert.Null(DotNetCommandExecutor.TestRunnerEnvironmentOverride.Value);
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_TEST_RUNNER", previousEnvironment);
            DotNetCommandExecutor.WorkingDirectoryOverride.Value = previousDirectory;
        }
    }

    [Fact]
    public async Task ConfigurationEnvironment_DefaultAndExplicitOverrideRemainSdkOwned()
    {
        using var root = await CreateFixtureAsync();
        var project = await WriteConsoleAsync(root.Path);
        var previous = Environment.GetEnvironmentVariable("Configuration");
        try
        {
            Environment.SetEnvironmentVariable("Configuration", "Release");
            var tools = CreateTools();
            var inherited = await tools.DotnetProject(DotnetProjectAction.Build, project: project, workingDirectory: root.Path);
            Assert.True(inherited.IsError != true, inherited.GetText());
            Assert.True(File.Exists(Path.Join(root.Path, "bin", "Release", "net10.0", "App.dll")));
            Assert.DoesNotContain(" -c ", inherited.GetText());
            var explicitResult = await tools.DotnetProject(DotnetProjectAction.Build, project: project,
                workingDirectory: root.Path, configuration: "Debug");
            Assert.True(explicitResult.IsError != true, explicitResult.GetText());
            Assert.True(File.Exists(Path.Join(root.Path, "bin", "Debug", "net10.0", "App.dll")));
        }
        finally { Environment.SetEnvironmentVariable("Configuration", previous); }
    }

    [Fact]
    public async Task Pack_CustomToolNuspecPreservesNetSdk1235WarningAsSuccess()
    {
        using var root = await CreateFixtureAsync();
        var project = await WriteConsoleAsync(root.Path);
        var xml = await File.ReadAllTextAsync(project, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(project, xml.Replace("</Project>",
            "<PropertyGroup><PackAsTool>true</PackAsTool><NuspecFile>custom.nuspec</NuspecFile></PropertyGroup></Project>"),
            TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Join(root.Path, "custom.nuspec"), """
            <package><metadata><id>SdkFeature.CustomTool</id><version>1.0.0</version><authors>Test</authors>
            <description>Warning fixture</description></metadata><files><file src="Program.cs" target="content" /></files></package>
            """, TestContext.Current.CancellationToken);
        var result = await CreateTools().DotnetProject(DotnetProjectAction.Pack, project: project, workingDirectory: root.Path);
        Assert.True(result.IsError != true, result.GetText());
        Assert.Contains("NETSDK1235", result.GetText());
        Assert.Single(Directory.GetFiles(Path.Join(root.Path, "bin", "Release"), "*.nupkg"));
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(path).Open());
        writer.Write(content);
    }

    internal static async Task<ScenarioHelpers.TempScenarioDirectory> CreateFixtureAsync(string? sdkVersion = null)
    {
        var root = ScenarioHelpers.CreateTempDirectory(nameof(Sdk11FeatureTests));
        var sdk = await ScenarioHelpers.RunDotNetAsync("--version", AppContext.BaseDirectory, TestContext.Current.CancellationToken);
        Assert.Equal(0, sdk.exitCode);
        await File.WriteAllTextAsync(Path.Join(root.Path, "global.json"), JsonSerializer.Serialize(new
        {
            sdk = new { version = sdkVersion ?? sdk.stdout.Trim(), rollForward = "disable", allowPrerelease = true }
        }), TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Join(root.Path, "NuGet.Config"),
            "<configuration><packageSources><clear /></packageSources></configuration>", TestContext.Current.CancellationToken);
        return root;
    }

    internal static async Task<string> WriteConsoleAsync(string directory, string source = "Console.WriteLine(\"ok\");")
    {
        var project = Path.Join(directory, "App.csproj");
        await File.WriteAllTextAsync(project, """
            <Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework>
            <OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>
            """, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Join(directory, "Program.cs"), source, TestContext.Current.CancellationToken);
        return project;
    }
}
