using System.IO.Pipelines;
using System.Reflection;
using System.Text.Json;
using Json.Schema;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp.Tests;

public class OutputSchemaTests
{
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
            """{"sdks":[{"version":false}]}""", """{"runtimes":[{"name":12}]}"""
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
        public CancellationToken Token => _timeout.Token;

        public static async Task<Host> CreateAsync(string protocol, McpServerTool? tool = null)
        {
            var host = new Host();
            host._timeout.CancelAfter(TimeSpan.FromSeconds(60));
            try
            {
                Pipe requests = new(), responses = new();
                var services = new ServiceCollection();
                services.AddLogging();
                services.AddSingleton<ConcurrencyManager>();
                services.AddSingleton<ProcessSessionManager>();
                services.AddSingleton(host.Metrics);
                services.AddSingleton(host.TokenSavings);
                services.AddSingleton<TokenSavingsEstimator>();
                var builder = services.AddMcpServer()
                    .WithStreamServerTransport(requests.Reader.AsStream(), responses.Writer.AsStream());
                if (tool is null)
                    builder.WithDotNetCliTools();
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
                catch (OperationCanceledException) when (_timeout.IsCancellationRequested) { }
            }
            if (_services is not null)
                await _services.DisposeAsync();
            _timeout.Dispose();
        }
    }
}
