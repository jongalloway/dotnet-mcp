using System.Diagnostics;
using System.IO.Pipelines;
using System.Text.Json;
using DotNetMcp.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp.Tests;

public class ToolAnnotationTests
{
    private static readonly string[] OpenWorldTools =
    [
        "dotnet_project", "dotnet_package", "dotnet_solution", "dotnet_ef",
        "dotnet_workload", "dotnet_tool", "dotnet_sdk", "dotnet_dev_certs", "dotnet_help"
    ];

    private static readonly string[] QueryTools =
        ["dotnet_sdk_query", "dotnet_project_query", "dotnet_server_metrics_query"];

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task Discovery_ExplicitHintsCoverEveryProductionTool(string protocol)
    {
        await using var host = await Host.CreateAsync(protocol);
        var tools = await host.Client.ListToolsAsync(cancellationToken: host.Token);
        var readOnlyTools = QueryTools.Concat(["dotnet_server_info", "dotnet_server_capabilities"]).ToArray();
        var expectedNames = OpenWorldTools.Concat(readOnlyTools).Append("dotnet_server_metrics").Order().ToArray();
        Assert.Equal(expectedNames, tools.Select(tool => tool.Name).Order().ToArray());

        foreach (var (name, parameter) in new[]
        {
            ("dotnet_project", "environmentVariables"),
            ("dotnet_solution", "parentSolution"),
            ("dotnet_sdk", "prerelease")
        })
        {
            var tool = tools.Single(tool => tool.Name == name).ProtocolTool;
            Assert.True(tool.InputSchema.GetProperty("properties").TryGetProperty(parameter, out var schema));
            Assert.False(string.IsNullOrWhiteSpace(schema.GetProperty("description").GetString()));
            Assert.DoesNotContain(parameter, tool.InputSchema.GetProperty("required").EnumerateArray().Select(value => value.GetString()));
        }

        foreach (var tool in tools)
        {
            var json = JsonSerializer.SerializeToElement(tool.ProtocolTool, McpJsonUtilities.DefaultOptions);
            var annotations = json.GetProperty("annotations");
            var readOnly = readOnlyTools.Contains(tool.Name);
            Assert.Equal(readOnly, annotations.GetProperty("readOnlyHint").GetBoolean());
            Assert.Equal(!readOnly, annotations.GetProperty("destructiveHint").GetBoolean());
            Assert.Equal(readOnly, annotations.GetProperty("idempotentHint").GetBoolean());
            Assert.Equal(OpenWorldTools.Contains(tool.Name), annotations.GetProperty("openWorldHint").GetBoolean());
        }

        foreach (var (name, actions, parameters) in new[]
        {
            ("dotnet_sdk_query", new[] { "Version", "Info", "ListSdks", "ListRuntimes", "FrameworkInfo", "CacheMetrics" },
                new[] { "action", "framework", "workingDirectory" }),
            ("dotnet_project_query", new[] { "Logs" }, new[] { "action", "sessionId", "tailLines", "since" }),
            ("dotnet_server_metrics_query", new[] { "Get", "TokenSavingsGet" }, new[] { "action" })
        })
        {
            var tool = tools.Single(tool => tool.Name == name).ProtocolTool;
            var properties = tool.InputSchema.GetProperty("properties");
            Assert.Equal(parameters.Order(), properties.EnumerateObject().Select(property => property.Name).Order());
            Assert.Equal(actions, properties.GetProperty("action").GetProperty("enum").EnumerateArray()
                .Select(value => value.GetString()));
            Assert.Equal(actions, tool.Meta!["actions"]!.Deserialize<string[]>());
            Assert.False(tool.Meta.ContainsKey("ui"));
            Assert.False(tool.Meta.ContainsKey("ui/resourceUri"));
        }
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task Discovery_UnspecifiedSdkHintsAreOmittedAndUseProtocolDefaults(string protocol)
    {
        await using var host = await Host.CreateAsync(protocol, includeDefaultTool: true);
        var tools = await host.Client.ListToolsAsync(cancellationToken: host.Token);
        var tool = tools.Single(tool => tool.Name == "default_hints").ProtocolTool;
        var json = JsonSerializer.SerializeToElement(tool, McpJsonUtilities.DefaultOptions);
        if (json.TryGetProperty("annotations", out var annotations))
        {
            foreach (var name in new[] { "readOnlyHint", "destructiveHint", "idempotentHint", "openWorldHint" })
                Assert.False(annotations.TryGetProperty(name, out _));
        }

        Assert.False(tool.Annotations?.ReadOnlyHint ?? false);
        Assert.True(tool.Annotations?.DestructiveHint ?? true);
        Assert.False(tool.Annotations?.IdempotentHint ?? false);
        Assert.True(tool.Annotations?.OpenWorldHint ?? true);
        var attribute = new McpServerToolAttribute();
        Assert.False(attribute.ReadOnly);
        Assert.True(attribute.Destructive);
        Assert.False(attribute.Idempotent);
        Assert.True(attribute.OpenWorld);
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task QueryCalls_RejectEveryExcludedActionWithoutExecutionOrReset(string protocol)
    {
        await using var host = await Host.CreateAsync(protocol);
        host.Metrics.RecordInvocation("baseline", 12, success: true);
        host.TokenSavings.RecordWorkflow(new TokenSavingsEstimator().EstimateWorkflow(
            "baseline", [new TokenSavingsStepInput { StepId = "baseline", PromptText = "query" }]));
        var metricsBefore = JsonSerializer.Serialize(host.Metrics.GetSnapshot());
        var savingsBefore = JsonSerializer.Serialize(host.TokenSavings.GetSnapshot());

        foreach (var (name, aggregate, allowed) in new[]
        {
            ("dotnet_sdk_query", Enum.GetValues<DotnetSdkAction>().Select(value => (value.ToString(), (int)value)),
                Enum.GetNames<DotnetSdkQueryAction>()),
            ("dotnet_project_query", Enum.GetValues<DotnetProjectAction>().Select(value => (value.ToString(), (int)value)),
                Enum.GetNames<DotnetProjectQueryAction>()),
            ("dotnet_server_metrics_query", Enum.GetValues<DotnetServerMetricsAction>().Select(value => (value.ToString(), (int)value)),
                Enum.GetNames<DotnetServerMetricsQueryAction>())
        })
        {
            var excluded = aggregate.Where(value => !allowed.Contains(value.Item1));
            foreach (var action in excluded.SelectMany(value => new object[] { value.Item1, value.Item2 })
                .Concat(["NotAnAction", 999]))
            {
                var arguments = new Dictionary<string, object?> { ["action"] = action };
                if (name == "dotnet_project_query")
                    arguments["sessionId"] = "not-started";
                var result = await host.Client.CallToolAsync(name, arguments, cancellationToken: host.Token);
                Assert.True(result.IsError, $"{name} must reject action {action}");
                if (action is int)
                    Assert.Contains("Invalid action", result.GetText());
            }
        }

        Assert.Empty(host.Commands);
        Assert.Equal(metricsBefore, JsonSerializer.Serialize(host.Metrics.GetSnapshot()));
        Assert.Equal(savingsBefore, JsonSerializer.Serialize(host.TokenSavings.GetSnapshot()));
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task QueryCalls_ReuseSdkAndMetricsResultsWithoutMutation(string protocol)
    {
        await using var host = await Host.CreateAsync(protocol);
        foreach (var action in Enum.GetNames<DotnetSdkQueryAction>())
        {
            var arguments = new Dictionary<string, object?> { ["action"] = action, ["framework"] = "net10.0" };
            var query = await host.Client.CallToolAsync("dotnet_sdk_query", arguments, cancellationToken: host.Token);
            var aggregate = await host.Client.CallToolAsync("dotnet_sdk", arguments, cancellationToken: host.Token);
            Assert.NotEqual(true, query.IsError);
            Assert.EndsWith(query.GetText(), aggregate.GetText());
            Assert.DoesNotContain("displayed in the SDK dashboard", query.GetText());
            Assert.Equal(aggregate.StructuredContent?.GetRawText(), query.StructuredContent?.GetRawText());
        }

        host.Metrics.RecordInvocation("baseline", 12, success: true);
        foreach (var action in Enum.GetNames<DotnetServerMetricsQueryAction>())
        {
            var arguments = new Dictionary<string, object?> { ["action"] = action };
            var query = await host.Client.CallToolAsync("dotnet_server_metrics_query", arguments, cancellationToken: host.Token);
            var aggregate = await host.Client.CallToolAsync("dotnet_server_metrics", arguments, cancellationToken: host.Token);
            Assert.NotEqual(true, query.IsError);
            Assert.Equal(aggregate.StructuredContent?.GetRawText(), query.StructuredContent?.GetRawText());
        }
        Assert.Equal(1, host.Metrics.GetSnapshot()["baseline"].InvocationCount);
        Assert.All(host.Commands, command => Assert.Contains(command, new[] { "--version", "--info", "--list-sdks", "--list-runtimes" }));
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task ProjectQuery_ReadsCapturedLogsWithoutStoppingTheSession(string protocol)
    {
        await using var host = await Host.CreateAsync(protocol);
        using var process = Process.Start(new ProcessStartInfo("dotnet", "--version")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        })!;
        Assert.True(host.Sessions.RegisterSession("query-test", process, "run", "fixture"));
        try
        {
            await process.WaitForExitAsync(host.Token);
            process.WaitForExit();
            var arguments = new Dictionary<string, object?> { ["action"] = "Logs", ["sessionId"] = "query-test", ["tailLines"] = 1 };
            var query = await host.Client.CallToolAsync("dotnet_project_query", arguments, cancellationToken: host.Token);
            var aggregate = await host.Client.CallToolAsync("dotnet_project", arguments, cancellationToken: host.Token);
            Assert.NotEqual(true, query.IsError);
            Assert.Equal(aggregate.GetText(), query.GetText());
            Assert.Contains("Logs for session 'query-test'", query.GetText());
            Assert.True(host.Sessions.TryGetSession("query-test", out _));
            Assert.Empty(host.Commands);

            arguments["action"] = (int)DotnetProjectAction.Build;
            var excluded = await host.Client.CallToolAsync("dotnet_project_query", arguments, cancellationToken: host.Token);
            Assert.True(excluded.IsError);
            Assert.Contains("Invalid action", excluded.GetText());
            arguments["action"] = "Logs";
            arguments["tailLines"] = 0;
            var invalid = await host.Client.CallToolAsync("dotnet_project_query", arguments, cancellationToken: host.Token);
            Assert.True(invalid.IsError);
            Assert.True(host.Sessions.TryGetSession("query-test", out _));
        }
        finally
        {
            Assert.True(host.Sessions.TryStopSession("query-test", out _));
        }
    }

    [Theory]
    [InlineData(DotnetServerMetricsQueryAction.Get)]
    [InlineData(DotnetServerMetricsQueryAction.TokenSavingsGet)]
    public async Task MetricsQuery_UnavailableServicesReturnAnError(DotnetServerMetricsQueryAction action)
    {
        var tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance,
            new ConcurrencyManager(), new ProcessSessionManager());
        var result = await tools.DotnetServerMetricsQuery(action);
        Assert.True(result.IsError);
        Assert.Contains("not registered", result.GetText());
    }

    [Theory]
    [InlineData(DotnetSdkQueryAction.Version)]
    [InlineData(DotnetSdkQueryAction.Info)]
    [InlineData(DotnetSdkQueryAction.ListSdks)]
    [InlineData(DotnetSdkQueryAction.ListRuntimes)]
    public async Task SdkQuery_ValidationAndCommandFailuresAreErrors(DotnetSdkQueryAction action)
    {
        var realTools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance,
            new ConcurrencyManager(), new ProcessSessionManager());
        var invalid = await realTools.DotnetSdkQuery(action,
            workingDirectory: Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        Assert.True(invalid.IsError);
        Assert.Contains("Directory not found", invalid.GetText());

        var executions = 0;
        var tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance,
            new ConcurrencyManager(), new ProcessSessionManager())
        {
            CommandExecutorOverride = (_, _, _) =>
            {
                executions++;
                return Task.FromResult("Error: SDK unavailable\nExit Code: 1");
            }
        };
        var failed = await tools.DotnetSdkQuery(action);
        Assert.True(failed.IsError);
        Assert.Equal(1, executions);
    }

    private sealed class Host : IAsyncDisposable
    {
        private readonly CancellationTokenSource _timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        private ServiceProvider? _services;
        private Task? _run;
        public McpClient Client { get; private set; } = null!;
        public List<string> Commands { get; } = [];
        public ToolMetricsAccumulator Metrics { get; } = new();
        public TokenSavingsAccumulator TokenSavings { get; } = new();
        public ProcessSessionManager Sessions { get; } = new();
        public CancellationToken Token => _timeout.Token;

        public static async Task<Host> CreateAsync(string protocol, bool includeDefaultTool = false)
        {
            var host = new Host();
            host._timeout.CancelAfter(TimeSpan.FromSeconds(60));
            try
            {
                Pipe requests = new(), responses = new();
                var services = new ServiceCollection();
                services.AddLogging();
                var builder = services.AddMcpServer()
                    .WithStreamServerTransport(requests.Reader.AsStream(), responses.Writer.AsStream())
                    .WithDotNetCliTools();
                services.AddSingleton(new DotNetCliTools(NullLogger<DotNetCliTools>.Instance,
                    new ConcurrencyManager(), host.Sessions, host.Metrics, host.TokenSavings, new TokenSavingsEstimator())
                {
                    CommandExecutorOverride = (arguments, _, _) =>
                    {
                        host.Commands.Add(arguments);
                        return Task.FromResult(arguments switch
                        {
                            "--version" => "10.0.100\nExit Code: 0",
                            "--list-sdks" => "10.0.100 [sdk]\nExit Code: 0",
                            "--list-runtimes" => "Microsoft.NETCore.App 10.0.0 [runtime]\nExit Code: 0",
                            "--info" => ".NET SDK:\nVersion: 10.0.100\nExit Code: 0",
                            _ => throw new InvalidOperationException($"Unexpected query command: {arguments}")
                        });
                    }
                });
                if (includeDefaultTool)
                    builder.WithTools([McpServerTool.Create(() => "default", new() { Name = "default_hints" })]);
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
                    Assert.True(_run.IsCanceled);
                }
            }
            if (_services is not null)
                await _services.DisposeAsync();
            _timeout.Dispose();
        }
    }
}
