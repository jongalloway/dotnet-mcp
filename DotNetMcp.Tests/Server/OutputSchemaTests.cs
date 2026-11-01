using System.IO.Pipelines;
using System.Diagnostics;
using DotNetMcp.Actions;
using System.Reflection;
using System.Text.Json;
using Json.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp.Tests;

[Collection("ProcessWideStateTests")]
public class OutputSchemaTests
{
    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task AllSixSchemas_ValidateRealActionFamiliesAndCompatibilityText(string protocol)
    {
        await using var host = await Host.CreateAsync(protocol);
        var tools = await host.Client.ListToolsAsync(cancellationToken: host.Token);
        Assert.Equal(6, tools.Count(t => t.ProtocolTool.OutputSchema.HasValue));
        foreach (var action in new[] { "Info", "FrameworkInfo", "CacheMetrics", "ListTemplates", "SearchTemplates", "TemplateInfo" })
        {
            var result = await host.Client.CallToolAsync("dotnet_sdk", new Dictionary<string, object?>
            {
                ["action"] = action, ["searchTerm"] = "console", ["templateShortName"] = "console"
            }, cancellationToken: host.Token);
            AssertContract(tools.Single(t => t.Name == "dotnet_sdk").ProtocolTool.OutputSchema!.Value, result);
            Assert.False(result.IsError == true, result.GetText());
        }
        var capabilities = await host.Client.CallToolAsync("dotnet_server_capabilities", cancellationToken: host.Token);
        AssertContract(tools.Single(t => t.Name == "dotnet_server_capabilities").ProtocolTool.OutputSchema!.Value, capabilities);
        Assert.Equal(protocol, capabilities.StructuredContent!.Value.GetProperty("protocolVersion").GetString());
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task DeterministicCommandProducers_ValidateListsDiagnosticsFallbackAndErrors(string protocol)
    {
        var output = "Exit Code: 0";
        await using var host = await Host.CreateAsync(protocol, executor: (_, _, _) => Task.FromResult(output));
        var tools = await host.Client.ListToolsAsync(cancellationToken: host.Token);
        async Task<CallToolResult> Call(string name, string action, bool failure = false)
        {
            var result = await host.Client.CallToolAsync(name,
                new Dictionary<string, object?> { ["action"] = action, ["project"] = "fixture.csproj", ["solution"] = "fixture.slnx", ["startMode"] = "Foreground" },
                cancellationToken: host.Token);
            AssertContract(tools.Single(t => t.Name == name).ProtocolTool.OutputSchema!.Value, result);
            Assert.Equal(failure, result.IsError == true);
            return result;
        }

        foreach (var (name, action, field) in new[]
        {
            ("dotnet_sdk", "ListSdks", "sdks"), ("dotnet_sdk", "ListRuntimes", "runtimes"),
            ("dotnet_package", "List", "packages"), ("dotnet_solution", "List", "projects")
        })
        {
            output = "Exit Code: 0";
            var empty = await Call(name, action);
            Assert.Equal(0, empty.StructuredContent!.Value.GetProperty(field).GetArrayLength());
            output = action switch
            {
                "ListSdks" => "10.0.100 [sdk]\nExit Code: 0",
                "ListRuntimes" => "Microsoft.NETCore.App 10.0.0 [runtime]\nExit Code: 0",
                _ when name == "dotnet_package" => "> Sample.Package 1.0.0 1.0.1\nExit Code: 0",
                _ => "Project(s)\n----------\nSample.csproj\nExit Code: 0"
            };
            var nonempty = await Call(name, action);
            Assert.Equal(1, nonempty.StructuredContent!.Value.GetProperty(field).GetArrayLength());
            output = "Command failed.\nExit Code: 1";
            var failed = await Call(name, action, failure: true);
            Assert.False(failed.StructuredContent!.Value.TryGetProperty(field, out _));
        }
        output = "Build succeeded.\n0 Warning(s)\n0 Error(s)\nExit Code: 0";
        var build = await Call("dotnet_project", "Build");
        Assert.Equal("Build succeeded. 0 error(s), 0 warning(s).", build.GetText());
        output = "fixture.csproj(1,2): error CS0103: Missing name\nBuild FAILED.\nExit Code: 1";
        var failedBuild = await Call("dotnet_project", "Build", failure: true);
        Assert.NotEmpty(failedBuild.StructuredContent!.Value.GetProperty("errors").EnumerateArray());
        output = "Passed! - Failed: 0, Passed: 2, Skipped: 1, Total: 3, Duration: 1 s\nExit Code: 0";
        await Call("dotnet_project", "Test");
        output = "Failed! - Failed: 1, Passed: 2, Skipped: 0, Total: 3, Duration: 1 s\nExit Code: 1";
        await Call("dotnet_project", "Test", failure: true);
        output = "Error is an ordinary word in stdout.\nExit Code: 0";
        foreach (var action in new[] { "Restore", "Clean", "Pack", "Format", "Publish", "Run" })
            await Call("dotnet_project", action);
        output = "Exit Code: 1";
        await Call("dotnet_project", "Publish", failure: true);
        var capabilityError = await host.Client.CallToolAsync("dotnet_server_capabilities", cancellationToken: host.Token);
        Assert.True(capabilityError.IsError);
        AssertContract(tools.Single(t => t.Name == "dotnet_server_capabilities").ProtocolTool.OutputSchema!.Value, capabilityError);
        output = "Template pack already installed.\nExit Code: 106";
        var alreadyInstalled = await host.Client.CallToolAsync("dotnet_sdk", new Dictionary<string, object?>
        { ["action"] = "InstallTemplatePack", ["templatePackage"] = "Sample.Templates" }, cancellationToken: host.Token);
        Assert.False(alreadyInstalled.IsError == true);
        AssertContract(tools.Single(t => t.Name == "dotnet_sdk").ProtocolTool.OutputSchema!.Value, alreadyInstalled);
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task ProjectMetadataActions_PreserveExistingJsonAndExposeTypedData(string protocol)
    {
        var directory = Path.Join(Environment.CurrentDirectory, "artifacts", $"schema-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var project = Path.Join(directory, "Fixture.csproj");
        await File.WriteAllTextAsync(project, """
            <Project>
              <PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Library</OutputType></PropertyGroup>
              <ItemGroup><PackageReference Include="Sample.Package" Version="1.0.0"/><Content Include="data.txt"/></ItemGroup>
            </Project>
            """, TestContext.Current.CancellationToken);
        try
        {
            await using var host = await Host.CreateAsync(protocol);
            var tools = await host.Client.ListToolsAsync(cancellationToken: host.Token);
            var schema = tools.Single(t => t.Name == "dotnet_project").ProtocolTool.OutputSchema!.Value;
            foreach (var action in new[] { "Analyze", "Dependencies", "Validate", "SetProperty", "GetProperty", "RemoveProperty", "AddItem", "ListItems", "RemoveItem" })
            {
                var result = await host.Client.CallToolAsync("dotnet_project", new Dictionary<string, object?>
                {
                    ["action"] = action, ["project"] = project, ["projectPath"] = project, ["propertyName"] = "SchemaTest",
                    ["propertyValue"] = "value", ["itemType"] = "Content", ["include"] = "schema-data.txt"
                }, cancellationToken: host.Token);
                Assert.False(result.IsError == true, result.GetText());
                AssertContract(schema, result);
                Assert.True(Parse(result.GetText()).GetProperty("success").GetBoolean());
                Assert.False(result.StructuredContent!.Value.TryGetProperty("action", out _));
            }
            var missing = await host.Client.CallToolAsync("dotnet_project", new Dictionary<string, object?>
            { ["action"] = "Analyze", ["projectPath"] = Path.Join(directory, "Missing.csproj") }, cancellationToken: host.Token);
            Assert.True(missing.IsError);
            AssertContract(schema, missing);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task MissingMetricsAndValidation_ReturnSchemaCoveredApplicationErrors(string protocol)
    {
        await using var host = await Host.CreateAsync(protocol, missingMetrics: true);
        var tools = await host.Client.ListToolsAsync(cancellationToken: host.Token);
        foreach (var action in Enum.GetNames<DotnetServerMetricsAction>())
        {
            var result = await host.Client.CallToolAsync("dotnet_server_metrics",
                new Dictionary<string, object?> { ["action"] = action }, cancellationToken: host.Token);
            Assert.True(result.IsError);
            AssertContract(tools.Single(t => t.Name == "dotnet_server_metrics").ProtocolTool.OutputSchema!.Value, result);
            Assert.Contains("CAPABILITY_NOT_AVAILABLE", result.GetText());
        }
        foreach (var (name, action) in new[] { ("dotnet_sdk", "TemplateInfo"), ("dotnet_package", "Add"), ("dotnet_solution", "Create"), ("dotnet_project", "SetProperty") })
        {
            var result = await host.Client.CallToolAsync(name, new Dictionary<string, object?> { ["action"] = action }, cancellationToken: host.Token);
            Assert.True(result.IsError, result.GetText());
            AssertContract(tools.Single(t => t.Name == name).ProtocolTool.OutputSchema!.Value, result);
        }
    }

    [Fact]
    public async Task InvalidMetricsAction_DoesNotFallThroughToGet()
    {
        var tools = new DotNetCliTools(Microsoft.Extensions.Logging.Abstractions.NullLogger<DotNetCliTools>.Instance,
            new ConcurrencyManager(), new ProcessSessionManager());
        var result = await tools.DotnetServerMetrics((DotnetServerMetricsAction)999);
        Assert.True(result.IsError);
        Assert.Contains("999", result.GetText());
        AssertContract(ToolOutputSchemas.CreateTool(typeof(DotNetCliTools).GetMethod(nameof(DotNetCliTools.DotnetServerMetrics))!).ProtocolTool.OutputSchema!.Value, result);
    }

    [Fact]
    public async Task OptionalRuntimeFailure_WarnsButCancellationPropagates()
    {
        var tools = new DotNetCliTools(Microsoft.Extensions.Logging.Abstractions.NullLogger<DotNetCliTools>.Instance,
            new ConcurrencyManager(), new ProcessSessionManager())
        {
            CommandExecutorOverride = (args, _, _) => Task.FromResult(args == "--list-sdks" ? "10.0.100 [sdk]\nExit Code: 0" : "Exit Code: 1")
        };
        var result = await tools.DotnetSdk(DotnetSdkAction.ListSdks);
        Assert.False(result.IsError == true);
        Assert.False(result.StructuredContent!.Value.TryGetProperty("runtimes", out _));
        Assert.True(result.StructuredContent.Value.TryGetProperty("warning", out _));
        AssertContract(ToolOutputSchemas.CreateTool(typeof(DotNetCliTools).GetMethod(nameof(DotNetCliTools.DotnetSdk))!).ProtocolTool.OutputSchema!.Value, result);
        var cancelled = new DotNetCliTools(Microsoft.Extensions.Logging.Abstractions.NullLogger<DotNetCliTools>.Instance,
            new ConcurrencyManager(), new ProcessSessionManager())
        {
            CommandExecutorOverride = (args, _, _) => args == "--list-sdks" ? Task.FromResult("Exit Code: 0") : throw new OperationCanceledException()
        };
        await Assert.ThrowsAsync<OperationCanceledException>(() => cancelled.DotnetSdk(DotnetSdkAction.ListSdks));
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task EnumeratedActions_HaveExplicitSuccessContracts(string protocol)
    {
        await using var host = await Host.CreateAsync(protocol);
        var tools = await host.Client.ListToolsAsync(cancellationToken: host.Token);
        foreach (var (name, actions, domain) in new[]
        {
            ("dotnet_sdk", Enum.GetNames<DotnetSdkAction>(), new[] { "Version", "ListSdks", "ListRuntimes", "FrameworkInfo", "CacheMetrics" }),
            ("dotnet_project", Enum.GetNames<DotnetProjectAction>(), new[] { "Build", "Test", "Run", "Publish", "Analyze", "Dependencies", "Validate", "SetProperty", "GetProperty", "RemoveProperty", "AddItem", "RemoveItem", "ListItems" }),
            ("dotnet_package", Enum.GetNames<DotnetPackageAction>(), new[] { "List" }),
            ("dotnet_solution", Enum.GetNames<DotnetSolutionAction>(), new[] { "List" })
        })
        {
            var schema = tools.Single(t => t.Name == name).ProtocolTool.OutputSchema!.Value;
            foreach (var action in actions)
            {
                var fallback = StructuredContentHelper.ToCallToolResult("unchanged", new ActionTextResult { Action = action, Text = "unchanged" });
                if (domain.Contains(action))
                    AssertInvalid(schema, fallback.StructuredContent!.Value);
                else
                    AssertContract(schema, fallback);
            }
            AssertInvalid(schema, Parse("""{"action":"Unknown","text":"unchanged"}"""));
            AssertInvalid(schema, Parse("""{"action":"List","text":12}"""));
            if (name == "dotnet_package")
                AssertInvalid(schema, Parse("""{"packages":[{}]}"""));
            if (name == "dotnet_solution")
                AssertInvalid(schema, Parse("""{"projects":[false]}"""));
        }
    }

    [Fact]
    public void ProjectSchema_RejectsCatchAllAndMalformedNestedData()
    {
        var schema = ToolOutputSchemas.CreateTool(typeof(DotNetCliTools).GetMethod(nameof(DotNetCliTools.DotnetProject))!).ProtocolTool.OutputSchema!.Value;
        foreach (var json in new[]
        {
            "{}", """{"success":true}""", """{"lockInfo":{ "lockScope":12 }}""",
            """{"success":true,"errorCount":"zero","warningCount":0,"summary":"ok","lockInfo":{"lockScope":"project","lockKey":"fixture"}}""",
            """{"action":"Build","text":"ok"}""", """{"action":"GetProperty","text":"ok"}""",
            """{"success":true,"projectPath":"fixture","propertyName":"OutputType"}""",
            """{"success":true,"projectPath":"fixture","itemType":"Content","count":1,"items":[{"include":false}]}"""
        })
            AssertInvalid(schema, Parse(json));
    }

    private static void AssertContract(JsonElement schema, CallToolResult result)
    {
        var payload = Assert.IsType<JsonElement>(result.StructuredContent);
        Assert.Equal(JsonValueKind.Object, payload.ValueKind);
        Assert.False(payload.TryGetProperty("result", out _));
        Assert.NotEmpty(result.GetText());
        AssertValid(schema, payload);
        Assert.Contains(result.Content.OfType<TextContentBlock>(), block =>
        {
            try { return JsonElement.DeepEquals(Parse(block.Text), payload); }
            catch (JsonException) { return false; }
        });
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task TemplateProducerFixtures_ValidateDomainMetadataAndOptionalParameters(string protocol)
    {
        await using var catalog = await Host.CreateAsync(protocol);
        var schema = (await catalog.Client.ListToolsAsync(cancellationToken: catalog.Token))
            .Single(t => t.Name == "dotnet_sdk").ProtocolTool.OutputSchema!.Value;
        var template = new TemplateDisplayInfo("console", ["console"], "C#", "project", null, "Console App", null,
            [new TemplateParameterDisplayInfo("framework", null, "choice", "net10.0")]);
        foreach (var (text, payload) in new (string, object)[]
        {
            (TemplateFormatter.FormatInstalledTemplates([]), new TemplateListResult([])),
            (TemplateFormatter.FormatInstalledTemplates([template]), new TemplateListResult([template])),
            (TemplateFormatter.FormatTemplateDetails(template), new TemplateDetailsResult(template))
        })
        {
            var tool = McpServerTool.Create(() => StructuredContentHelper.ToCallToolResult(text, payload),
                new() { Name = "dotnet_sdk", UseStructuredContent = true, OutputSchema = schema });
            await using var host = await Host.CreateAsync(protocol, tool);
            var listed = Assert.Single(await host.Client.ListToolsAsync(cancellationToken: host.Token));
            var result = await host.Client.CallToolAsync(listed.Name, cancellationToken: host.Token);
            Assert.Equal(text, result.GetText());
            AssertContract(listed.ProtocolTool.OutputSchema!.Value, result);
        }
        AssertInvalid(schema, Parse("""{"templates":[{"shortName":"console","parameters":[{"name":"framework","dataType":false}]}]}"""));
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task TemplateCliFallback_PreservesTextAndDoesNotInventEmptyDomainResults(string protocol)
    {
        var originalLoader = TemplateEngineHelper.LoadTemplatesOverride;
        var originalExecutor = TemplateEngineHelper.ExecuteDotNetForTemplatesAsync;
        try
        {
            TemplateEngineHelper.LoadTemplatesOverride = () => Task.FromResult<IEnumerable<Microsoft.TemplateEngine.Abstractions.ITemplateInfo>>([]);
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = (_, _) => Task.FromResult("CLI template metadata");
            await using var host = await Host.CreateAsync(protocol);
            var schema = (await host.Client.ListToolsAsync(cancellationToken: host.Token)).Single(t => t.Name == "dotnet_sdk").ProtocolTool.OutputSchema!.Value;
            foreach (var action in new[] { "ListTemplates", "SearchTemplates", "TemplateInfo" })
            {
                var result = await host.Client.CallToolAsync("dotnet_sdk", new Dictionary<string, object?>
                {
                    ["action"] = action, ["searchTerm"] = "console", ["templateShortName"] = "console", ["forceReload"] = true
                }, cancellationToken: host.Token);
                Assert.False(result.IsError == true);
                AssertContract(schema, result);
                Assert.Equal(action, result.StructuredContent!.Value.GetProperty("action").GetString());
                Assert.Contains("CLI template metadata", result.GetText());
            }
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = (_, _) => throw new InvalidOperationException("CLI failed");
            foreach (var action in new[] { "ListTemplates", "SearchTemplates", "TemplateInfo" })
            {
                var result = await host.Client.CallToolAsync("dotnet_sdk", new Dictionary<string, object?>
                {
                    ["action"] = action, ["searchTerm"] = "console", ["templateShortName"] = "console", ["forceReload"] = true
                }, cancellationToken: host.Token);
                Assert.True(result.IsError);
                AssertContract(schema, result);
                Assert.False(result.StructuredContent!.Value.TryGetProperty("templates", out _));
            }
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = (_, _) => throw new OperationCanceledException();
            await Assert.ThrowsAsync<OperationCanceledException>(() => TemplateEngineHelper.QueryStructuredAsync(
                DotnetSdkAction.ListTemplates, true, null, null, null, null, null));
        }
        finally
        {
            TemplateEngineHelper.LoadTemplatesOverride = originalLoader;
            TemplateEngineHelper.ExecuteDotNetForTemplatesAsync = originalExecutor;
            await TemplateEngineHelper.ClearCacheAsync();
        }
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task ContendedProjectActions_RetainLockMetadataAndSetIsError(string protocol)
    {
        var executions = 0;
        await using var host = await Host.CreateAsync(protocol, executor: (_, _, _) =>
        {
            executions++;
            return Task.FromResult("Exit Code: 0");
        });
        var schema = (await host.Client.ListToolsAsync(cancellationToken: host.Token)).Single(t => t.Name == "dotnet_project").ProtocolTool.OutputSchema!.Value;
        var project = Path.Join(Environment.CurrentDirectory, "Fixture.csproj");
        Assert.True(host.Concurrency.TryAcquireOperation("build", project, out _));
        try
        {
            foreach (var action in new[] { "Build", "Test", "Run", "Publish" })
            {
                var result = await host.Client.CallToolAsync("dotnet_project", new Dictionary<string, object?>
                { ["action"] = action, ["project"] = project, ["startMode"] = "Foreground" }, cancellationToken: host.Token);
                Assert.True(result.IsError);
                AssertContract(schema, result);
                Assert.True(result.StructuredContent!.Value.GetProperty("lockInfo").GetProperty("lockContended").GetBoolean());
                if (action is "Build" or "Test")
                {
                    Assert.False(result.StructuredContent.Value.GetProperty("success").GetBoolean());
                    Assert.DoesNotContain("succeeded", result.StructuredContent.Value.GetProperty("summary").GetString()!,
                        StringComparison.OrdinalIgnoreCase);
                }
            }
            Assert.Equal(0, executions);
        }
        finally { host.Concurrency.ReleaseOperation("build", project); }
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task BuildAndTestValidationFailures_HaveConsistentStructuredOutcomes(string protocol)
    {
        var executions = 0;
        await using var host = await Host.CreateAsync(protocol, executor: (_, _, _) =>
        {
            executions++;
            return Task.FromResult("Exit Code: 0");
        });
        var schema = (await host.Client.ListToolsAsync(cancellationToken: host.Token))
            .Single(t => t.Name == "dotnet_project").ProtocolTool.OutputSchema!.Value;
        foreach (var action in new[] { "Build", "Test" })
        {
            var result = await host.Client.CallToolAsync("dotnet_project", new Dictionary<string, object?>
            {
                ["action"] = action, ["project"] = "fixture.csproj", ["configuration"] = "Invalid"
            }, cancellationToken: host.Token);
            Assert.True(result.IsError);
            AssertContract(schema, result);
            Assert.False(result.StructuredContent!.Value.GetProperty("success").GetBoolean());
            Assert.DoesNotContain("succeeded", result.StructuredContent.Value.GetProperty("summary").GetString()!,
                StringComparison.OrdinalIgnoreCase);
            Assert.True(result.StructuredContent.Value.TryGetProperty("lockInfo", out _));
        }
        Assert.Equal(0, executions);
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task ExecutorStartupFailures_NeverProduceSuccessfulPayloads(string protocol)
    {
        const string failure = "dotnet command could not be started: executable unavailable";
        await using var host = await Host.CreateAsync(protocol, executor: (_, _, _) => Task.FromResult(failure));
        var tools = await host.Client.ListToolsAsync(cancellationToken: host.Token);
        foreach (var (name, action) in new[]
        {
            ("dotnet_sdk", "Version"), ("dotnet_sdk", "ListSdks"), ("dotnet_sdk", "ListRuntimes"),
            ("dotnet_package", "List"), ("dotnet_solution", "List"),
            ("dotnet_project", "Build"), ("dotnet_project", "Test"),
            ("dotnet_server_capabilities", (string?)null)
        })
        {
            var result = await host.Client.CallToolAsync(name, new Dictionary<string, object?>
            {
                ["action"] = action, ["project"] = "fixture.csproj", ["solution"] = "fixture.slnx"
            }, cancellationToken: host.Token);
            Assert.True(result.IsError, $"{name}/{action}: {result.GetText()}");
            AssertContract(tools.Single(t => t.Name == name).ProtocolTool.OutputSchema!.Value, result);
            Assert.False(result.StructuredContent!.Value.GetProperty("success").GetBoolean());
            foreach (var field in new[] { "sdks", "runtimes", "packages", "projects", "serverVersion" })
                Assert.False(result.StructuredContent.Value.TryGetProperty(field, out _));
        }
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task LogsContainingFailureText_RemainSuccessfulRetrievals(string protocol)
    {
        await using var host = await Host.CreateAsync(protocol);
        var schema = (await host.Client.ListToolsAsync(cancellationToken: host.Token))
            .Single(t => t.Name == "dotnet_project").ProtocolTool.OutputSchema!.Value;
        var startInfo = new ProcessStartInfo
        {
            FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(OperatingSystem.IsWindows() ? "/c" : "-c");
        startInfo.ArgumentList.Add("echo Exit Code: 1");
        using var process = Process.Start(startInfo)!;
        var sessionId = Guid.NewGuid().ToString("N");
        Assert.True(host.Sessions.RegisterSession(sessionId, process, "run", "fixture"));
        try
        {
            await process.WaitForExitAsync(host.Token);
            process.WaitForExit();
            var result = await host.Client.CallToolAsync("dotnet_project", new Dictionary<string, object?>
            { ["action"] = "Logs", ["sessionId"] = sessionId }, cancellationToken: host.Token);
            Assert.Contains("Exit Code: 1", result.GetText());
            Assert.False(result.IsError == true);
            AssertContract(schema, result);
            Assert.Equal("Logs", result.StructuredContent!.Value.GetProperty("action").GetString());
            Assert.Equal(result.GetText(), result.StructuredContent.Value.GetProperty("text").GetString());
            foreach (var arguments in new Dictionary<string, object?>[]
            {
                new() { ["action"] = "Logs" },
                new() { ["action"] = "Logs", ["sessionId"] = "missing" },
                new() { ["action"] = "Logs", ["sessionId"] = sessionId, ["tailLines"] = 0 },
                new() { ["action"] = "Logs", ["sessionId"] = sessionId, ["since"] = "invalid" }
            })
            {
                var invalid = await host.Client.CallToolAsync("dotnet_project", arguments, cancellationToken: host.Token);
                Assert.True(invalid.IsError);
                AssertContract(schema, invalid);
            }
        }
        finally { Assert.True(host.Sessions.TryStopSession(sessionId, out _)); }
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task ProductionSchemas_ValidateActualOutputsWithoutChangingEnvelopes(string protocol)
    {
        await using var host = await Host.CreateAsync(protocol);
        var tools = await host.Client.ListToolsAsync(cancellationToken: host.Token);
        foreach (var action in new[] { "Version", "ListSdks", "ListRuntimes" })
        {
            var result = await host.Client.CallToolAsync("dotnet_sdk",
                new Dictionary<string, object?> { ["action"] = action }, cancellationToken: host.Token);
            var content = Assert.IsType<JsonElement>(result.StructuredContent);
            Assert.Equal(JsonValueKind.Object, content.ValueKind);
            Assert.False(content.TryGetProperty("result", out _));
            AssertValid(tools.Single(tool => tool.Name == "dotnet_sdk").ProtocolTool.OutputSchema!.Value, content);
        }

        host.Metrics.RecordInvocation("dotnet_sdk", 25, success: true);
        host.TokenSavings.RecordWorkflow(new TokenSavingsEstimator().EstimateWorkflow(
            "create-project-package-build",
            [new TokenSavingsStepInput { StepId = "schema-test", StepKind = "discovery", PromptText = "Find SDKs" }]));
        foreach (var action in new[] { "Get", "TokenSavingsGet", "TokenSavingsReset", "Reset", "Get" })
        {
            var result = await host.Client.CallToolAsync("dotnet_server_metrics",
                new Dictionary<string, object?> { ["action"] = action }, cancellationToken: host.Token);
            var content = Assert.IsType<JsonElement>(result.StructuredContent);
            Assert.Equal(JsonValueKind.Object, content.ValueKind);
            Assert.False(content.TryGetProperty("result", out _));
            AssertValid(tools.Single(tool => tool.Name == "dotnet_server_metrics").ProtocolTool.OutputSchema!.Value, content);
            if (action == "TokenSavingsGet")
                Assert.Equal(1, content.GetProperty("workflowTokenSavings").GetArrayLength());
            if (action == "TokenSavingsReset")
                Assert.Equal(0, content.GetProperty("workflowTokenSavings").GetArrayLength());
        }
    }

    [Fact]
    public void SdkSchema_ValidatesOptionalRuntimesAndRejectsInvalidShapes()
    {
        var schema = ToolOutputSchemas.CreateTool(typeof(DotNetCliTools).GetMethod(nameof(DotNetCliTools.DotnetSdk))!)
            .ProtocolTool.OutputSchema!.Value;
        foreach (var json in new[]
        {
            """{"version":"10.0.100"}""",
            """{"sdks":[]}""",
            """{"sdks":[{"version":"10.0.100","path":"sdk"}]}""",
            """{"sdks":[],"runtimes":[]}""",
            """{"runtimes":[{"name":"Microsoft.NETCore.App","version":"10.0.0","path":"runtime"}]}"""
        })
            AssertValid(schema, Parse(json));
        foreach (var json in new[]
        {
            "{}", """{"unrelated":true}""", """{"version":12}""", """{"version":null}""",
            """{"sdks":null}""", """{"sdks":"invalid"}""", """{"runtimes":null}""",
            """{"sdks":[{"version":false}]}""", """{"runtimes":[{"name":12}]}""",
            """{"sdks":[{}]}""", """{"runtimes":[{"name":"runtime"}]}"""
        })
            AssertInvalid(schema, Parse(json));
    }

    [Fact]
    public void MetricsSchema_RejectsMissingFieldsAndWrongNestedTypes()
    {
        var schema = ToolOutputSchemas.CreateTool(typeof(DotNetCliTools).GetMethod(nameof(DotNetCliTools.DotnetServerMetrics))!)
            .ProtocolTool.OutputSchema!.Value;
        foreach (var json in new[]
        {
            "{}", """{"content":[]}""", """{"success":true}""", """{"success":"yes","message":"reset"}""",
            """{"toolMetrics":{},"totalInvocations":0}""",
            """{"success":true,"message":"snapshot","workflowTokenSavings":[]}""",
            """{"toolMetrics":{"dotnet_sdk":{"invocationCount":"invalid"}},"totalInvocations":1,"totalSuccesses":1,"totalFailures":0,"tokenSavingsEnabled":true}"""
        })
            AssertInvalid(schema, Parse(json));
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task Discovery_AllInputsAreRequiredAndMetadataIsPreserved(string protocol)
    {
        await using var host = await Host.CreateAsync(protocol);
        var tools = await host.Client.ListToolsAsync(cancellationToken: host.Token);
        var methods = typeof(DotNetCliTools).GetMethods()
            .Where(method => method.IsDefined(typeof(McpServerToolAttribute))).ToArray();
        Assert.Equal(methods.Length, tools.Count);
        foreach (var method in methods)
        {
            var original = McpServerTool.Create(method, context => context.Services!.GetRequiredService<DotNetCliTools>()).ProtocolTool;
            var listed = tools.Single(tool => tool.Name == original.Name).ProtocolTool;
            Assert.Equal(JsonValueKind.Object, listed.InputSchema.ValueKind);
            Assert.Equal(original.InputSchema.GetRawText(), listed.InputSchema.GetRawText());
            Assert.Equal(original.Title, listed.Title);
            Assert.Equal(original.Description, listed.Description);
            Assert.Equal(JsonSerializer.Serialize(original.Annotations), JsonSerializer.Serialize(listed.Annotations));
            Assert.Equal(JsonSerializer.Serialize(original.Icons), JsonSerializer.Serialize(listed.Icons));
            Assert.Equal(original.Meta?.ToJsonString(), listed.Meta?.ToJsonString());
            var serialized = JsonSerializer.Serialize(listed, McpJsonUtilities.DefaultOptions);
            Assert.NotNull(JsonSerializer.Deserialize<Tool>(serialized, McpJsonUtilities.DefaultOptions));
        }
    }

    [Theory]
    [InlineData("2026-07-28", false)]
    [InlineData("2025-11-25", false)]
    [InlineData("2026-07-28", true)]
    [InlineData("2025-11-25", true)]
    public async Task NonObjectSchema_ReferencesResolveAfterSdkWrapping(string protocol, bool recursive)
    {
        var naturalSchema = Parse(recursive ? """
            {
              "anyOf":[{"type":"object"}],
              "properties":{"name":{"type":"string","minLength":1},"child":{"$ref":"#"}},
              "required":["name"]
            }
            """ : """
            {
              "$schema":"https://json-schema.org/draft/2020-12/schema",
              "anyOf":[{"type":"array","items":{"$ref":"#/$defs/item"}}],
              "$defs":{"item":{"type":"object","properties":{"name":{"type":"string","minLength":1}},"required":["name"]}}
            }
            """);
        var payload = Parse(recursive ? """{"name":"sdk","child":{"name":"runtime"}}""" : """[{"name":"sdk"}]""");
        var options = new McpServerToolCreateOptions { Name = "reference_fixture", UseStructuredContent = true, OutputSchema = naturalSchema };
        var tool = McpServerTool.Create(() => payload, options);
        await using var host = await Host.CreateAsync(protocol, tool);
        var listed = Assert.Single(await host.Client.ListToolsAsync(cancellationToken: host.Token)).ProtocolTool;
        Assert.Equal(JsonValueKind.Object, listed.InputSchema.ValueKind);
        Assert.NotNull(JsonSerializer.Deserialize<Tool>(JsonSerializer.Serialize(listed, McpJsonUtilities.DefaultOptions),
            McpJsonUtilities.DefaultOptions));
        var result = await host.Client.CallToolAsync(listed.Name, cancellationToken: host.Token);
        var content = Assert.IsType<JsonElement>(result.StructuredContent);
        var wrapped = protocol == "2025-11-25";
        Assert.Equal(payload.GetRawText(), (wrapped ? content.GetProperty("result") : content).GetRawText());
        AssertValid(listed.OutputSchema!.Value, content);
        var invalid = Parse(recursive ? """{"name":"sdk","child":{"name":""}}""" : """[{"name":""}]""");
        if (wrapped)
            invalid = JsonSerializer.SerializeToElement(new { result = invalid });
        AssertInvalid(listed.OutputSchema.Value, invalid);
        Assert.Equal(naturalSchema.GetRawText(), tool.ProtocolTool.OutputSchema!.Value.GetRawText());
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task ExplicitObjectResult_ComposedReferencesPreserveSchemaMeaning(string protocol)
    {
        var naturalSchema = Parse("""
            {
              "type":"object",
              "anyOf":[{"$ref":"#/$defs/item"}],
              "$defs":{"item":{"type":"object","properties":{"name":{"type":"string","minLength":1}},"required":["name"]}}
            }
            """);
        var payload = Parse("""{"name":"sdk"}""");
        var tool = McpServerTool.Create(() => new CallToolResult
        {
            Content = [new TextContentBlock { Text = "unchanged" }],
            StructuredContent = payload
        }, new() { Name = "explicit_fixture", UseStructuredContent = true, OutputSchema = naturalSchema });
        await using var host = await Host.CreateAsync(protocol, tool);
        var listed = Assert.Single(await host.Client.ListToolsAsync(cancellationToken: host.Token)).ProtocolTool;
        var result = await host.Client.CallToolAsync(listed.Name, cancellationToken: host.Token);
        Assert.Equal(payload.GetRawText(), result.StructuredContent!.Value.GetRawText());
        Assert.Equal("unchanged", Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
        AssertValid(listed.OutputSchema!.Value, result.StructuredContent.Value);
        AssertInvalid(listed.OutputSchema.Value, Parse("""{"name":""}"""));
        AssertInvalid(listed.OutputSchema.Value, Parse("{}"));
    }

    private static JsonElement Parse(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    private static void AssertValid(JsonElement schema, JsonElement content)
    {
        var result = JsonSchema.FromText(schema.GetRawText(), new BuildOptions { Dialect = Dialect.Draft202012 })
            .Evaluate(content);
        Assert.True(result.IsValid, $"Schema rejected {content.GetRawText()}: {JsonSerializer.Serialize(result)}");
    }

    private static void AssertInvalid(JsonElement schema, JsonElement content) =>
        Assert.False(JsonSchema.FromText(schema.GetRawText(), new BuildOptions { Dialect = Dialect.Draft202012 })
            .Evaluate(content).IsValid, $"Schema accepted {content.GetRawText()}");

    private sealed class Host : IAsyncDisposable
    {
        private readonly CancellationTokenSource _timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        private ServiceProvider? _services;
        private Task? _run;
        public McpClient Client { get; private set; } = null!;
        public ToolMetricsAccumulator Metrics { get; } = new();
        public TokenSavingsAccumulator TokenSavings { get; } = new();
        public ConcurrencyManager Concurrency { get; } = new();
        public ProcessSessionManager Sessions { get; } = new();
        public CancellationToken Token => _timeout.Token;

        public static async Task<Host> CreateAsync(string protocol, McpServerTool? tool = null,
            Func<string, CancellationToken, string?, Task<string>>? executor = null, bool missingMetrics = false)
        {
            var host = new Host();
            host._timeout.CancelAfter(TimeSpan.FromSeconds(60));
            try
            {
                Pipe requests = new(), responses = new();
                var services = new ServiceCollection();
                services.AddLogging();
                services.AddSingleton(host.Concurrency);
                services.AddSingleton(host.Sessions);
                services.AddSingleton(host.Metrics);
                services.AddSingleton(host.TokenSavings);
                services.AddSingleton<TokenSavingsEstimator>();
                var builder = services.AddMcpServer()
                    .WithStreamServerTransport(requests.Reader.AsStream(), responses.Writer.AsStream());
                if (tool is null)
                {
                    builder.WithDotNetCliTools();
                    if (executor is not null || missingMetrics)
                        services.AddSingleton(provider => new DotNetCliTools(provider.GetRequiredService<ILogger<DotNetCliTools>>(),
                            provider.GetRequiredService<ConcurrencyManager>(), provider.GetRequiredService<ProcessSessionManager>(),
                            missingMetrics ? null : host.Metrics, missingMetrics ? null : host.TokenSavings,
                            missingMetrics ? null : provider.GetRequiredService<TokenSavingsEstimator>()) { CommandExecutorOverride = executor });
                }
                else
                    builder.WithTools([tool]);
                host._services = services.BuildServiceProvider();
                host._run = host._services.GetRequiredService<McpServer>().RunAsync(host.Token);
                host.Client = await McpClient.CreateAsync(
                    new StreamClientTransport(requests.Writer.AsStream(), responses.Reader.AsStream()),
                    new() { ProtocolVersion = protocol, DiscoverProbeTimeout = Timeout.InfiniteTimeSpan },
                    cancellationToken: host.Token);
                return host;
            }
            catch
            {
                await host.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Client is not null)
                await Client.DisposeAsync();
            await _timeout.CancelAsync();
            if (_run is not null)
            {
                try { await _run; }
                catch (OperationCanceledException) when (_timeout.IsCancellationRequested)
                {
                    Assert.True(_run.IsCanceled, "Server shutdown should cancel the run task rather than fault it.");
                }
            }
            if (_services is not null)
                await _services.DisposeAsync();
            _timeout.Dispose();
        }
    }
}
