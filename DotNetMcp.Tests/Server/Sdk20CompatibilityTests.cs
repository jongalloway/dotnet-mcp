using System.IO.Pipelines;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Xml.Linq;
using System.Threading.Channels;
using AgentGovernance.Extensions.ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Extensions.Apps;
using ModelContextProtocol.Extensions.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp.Tests;

public class Sdk20CompatibilityTests
{
    private const string SdkTelemetryName = "Experimental.ModelContextProtocol";

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task TraceContext_PropagatesAcrossTheWireAndCoexistsWithSdkMetrics(string protocol)
    {
        using var listener = ListenToSdkActivities();
        Activity? clientActivity = null, serverActivity = null;
        JsonNode? wireRequest = null;
        var measurement = new TaskCompletionSource<(string Name, string? Unit, double Duration)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, owner) =>
            {
                if (instrument.Meter.Name == SdkTelemetryName && instrument is Histogram<double>)
                    owner.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((instrument, duration, tags, _) =>
        {
            if (serverActivity is null || !ReferenceEquals(Activity.Current, serverActivity))
                return;
            foreach (var tag in tags)
            {
                if (tag.Key == "mcp.method.name" && Equals(tag.Value, RequestMethods.ToolsCall))
                    measurement.TrySetResult((instrument.Name, instrument.Unit, duration));
            }
        });
        meterListener.Start();

        await using var host = await TestServer.CreateAsync(protocol,
            observeToolActivity: activity => serverActivity = activity,
            transformToolRequest: request =>
            {
                clientActivity = Activity.Current;
                wireRequest = JsonSerializer.SerializeToNode(request, McpJsonUtilities.DefaultOptions);
            });
        using var caller = new Activity("trace-context-caller").SetIdFormat(ActivityIdFormat.W3C);
        caller.TraceStateString = "vendor=test";
        caller.Start();

        var result = await host.Client.CallToolAsync("dotnet_server_info", cancellationToken: host.Token);
        Assert.NotEqual(true, result.IsError);
        Assert.Contains("stdio", result.GetText());
        Assert.NotNull(clientActivity);
        Assert.NotNull(serverActivity);
        Assert.Equal(SdkTelemetryName, clientActivity.Source.Name);
        Assert.Equal(SdkTelemetryName, serverActivity.Source.Name);
        Assert.Equal(ActivityKind.Client, clientActivity.Kind);
        Assert.Equal(ActivityKind.Server, serverActivity.Kind);
        Assert.Equal(caller.TraceId, clientActivity.TraceId);
        Assert.Equal(caller.SpanId, clientActivity.ParentSpanId);
        Assert.Equal(clientActivity.TraceId, serverActivity.TraceId);
        Assert.Equal(clientActivity.SpanId, serverActivity.ParentSpanId);
        Assert.True(serverActivity.HasRemoteParent);
        Assert.Equal(caller.TraceStateString, serverActivity.TraceStateString);
        Assert.Equal(clientActivity.Id, wireRequest!["params"]!["_meta"]!["traceparent"]!.GetValue<string>());
        Assert.Equal(caller.TraceStateString, wireRequest["params"]!["_meta"]!["tracestate"]!.GetValue<string>());
        Assert.Same(caller, Activity.Current);

        var recorded = await measurement.Task.WaitAsync(host.Token);
        Assert.Equal("mcp.server.operation.duration", recorded.Name);
        Assert.Equal("s", recorded.Unit);
        Assert.True(recorded.Duration >= 0);
    }

    [Theory]
    [InlineData("2026-07-28", null)]
    [InlineData("2025-11-25", null)]
    [InlineData("2026-07-28", "invalid")]
    [InlineData("2025-11-25", "invalid")]
    [InlineData("2026-07-28", "00-00000000000000000000000000000000-0000000000000000-01")]
    [InlineData("2025-11-25", "00-00000000000000000000000000000000-0000000000000000-01")]
    public async Task TraceContext_ExtractsExternalParentAndDoesNotLeakIntoUnparentedRequests(
        string protocol, string? invalidTraceparent)
    {
        using var listener = ListenToSdkActivities();
        Activity? serverActivity = null;
        var remote = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.Recorded, "vendor=external", isRemote: true);
        string? traceparent = $"00-{remote.TraceId}-{remote.SpanId}-01";
        string? tracestate = remote.TraceState;
        await using var host = await TestServer.CreateAsync(protocol,
            observeToolActivity: activity => serverActivity = activity,
            transformToolRequest: request =>
            {
                var meta = Assert.IsType<JsonObject>(request.Params!["_meta"]);
                meta.Remove("traceparent");
                meta.Remove("tracestate");
                if (traceparent is not null)
                    meta["traceparent"] = traceparent;
                if (tracestate is not null)
                    meta["tracestate"] = tracestate;
            });

        using var caller = new Activity("unrelated-caller").SetIdFormat(ActivityIdFormat.W3C).Start();
        Assert.NotEqual(true, (await host.Client.CallToolAsync("dotnet_server_info",
            cancellationToken: host.Token)).IsError);
        Assert.NotNull(serverActivity);
        Assert.Equal(remote.TraceId, serverActivity.TraceId);
        Assert.Equal(remote.SpanId, serverActivity.ParentSpanId);
        Assert.Equal(remote.TraceState, serverActivity.TraceStateString);
        Assert.True(serverActivity.HasRemoteParent);
        Assert.NotEqual(caller.TraceId, serverActivity.TraceId);

        traceparent = invalidTraceparent;
        tracestate = null;
        Assert.NotEqual(true, (await host.Client.CallToolAsync("dotnet_server_info",
            cancellationToken: host.Token)).IsError);
        Assert.NotNull(serverActivity);
        Assert.Equal(default, serverActivity.ParentSpanId);
        Assert.False(serverActivity.HasRemoteParent);
        Assert.NotEqual(remote.TraceId, serverActivity.TraceId);
        Assert.NotEqual(caller.TraceId, serverActivity.TraceId);
        Assert.Null(serverActivity.TraceStateString);
        var unparentedTraceId = serverActivity.TraceId;

        var nextRemote = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.Recorded, "vendor=next", isRemote: true);
        traceparent = $"00-{nextRemote.TraceId}-{nextRemote.SpanId}-01";
        tracestate = nextRemote.TraceState;
        Assert.NotEqual(true, (await host.Client.CallToolAsync("dotnet_server_info",
            cancellationToken: host.Token)).IsError);
        Assert.NotNull(serverActivity);
        Assert.Equal(nextRemote.TraceId, serverActivity.TraceId);
        Assert.Equal(nextRemote.SpanId, serverActivity.ParentSpanId);
        Assert.Equal(nextRemote.TraceState, serverActivity.TraceStateString);
        Assert.NotEqual(unparentedTraceId, serverActivity.TraceId);
        Assert.True(serverActivity.HasRemoteParent);
        Assert.Same(caller, Activity.Current);
    }

    private static ActivityListener ListenToSdkActivities()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == SdkTelemetryName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task DiscoveryAndInlineCalls_WorkOnBothRevisions(string protocol)
    {
        await using var host = await TestServer.CreateAsync(protocol);
        Assert.Equal(protocol, host.Client.NegotiatedProtocolVersion);
        var tools = await host.Client.ListToolsAsync(cancellationToken: host.Token);
        Assert.Equal(13, tools.Count);
        foreach (var tool in tools)
        {
            Assert.Equal(JsonValueKind.Object, tool.ProtocolTool.InputSchema.ValueKind);
            Assert.NotNull(JsonSerializer.Deserialize<Tool>(
                JsonSerializer.Serialize(tool.ProtocolTool, McpJsonUtilities.DefaultOptions), McpJsonUtilities.DefaultOptions));
            if (tool.Name is "dotnet_project" or "dotnet_solution" or "dotnet_package" or "dotnet_server_capabilities")
                Assert.False(tool.ProtocolTool.InputSchema.GetProperty("properties").TryGetProperty("context", out _));
            Assert.False(tool.ProtocolTool.InputSchema.GetProperty("properties").TryGetProperty("cancellationToken", out _));
        }

        var result = await host.Client.CallToolAsync("dotnet_server_info", cancellationToken: host.Token);
        Assert.Contains("stdio", result.GetText());
        Assert.NotEmpty(await host.Client.ListResourcesAsync(cancellationToken: host.Token));
        Assert.NotEmpty(await host.Client.ListPromptsAsync(cancellationToken: host.Token));

#pragma warning disable MCPEXP003 // Apps discovery remains available to non-Apps clients on both protocol revisions.
        Assert.True(host.Client.ServerCapabilities.Extensions?.ContainsKey(McpApps.ExtensionId));
        foreach (var (toolName, uri) in new[]
        {
            ("dotnet_sdk", "ui://dotnet-mcp/sdk-dashboard"),
            ("dotnet_server_metrics", "ui://dotnet-mcp/server-metrics")
        })
        {
            var tool = Assert.Single(tools, tool => tool.Name == toolName).ProtocolTool;
            Assert.NotNull(tool.Meta);
            var ui = tool.Meta["ui"]!.Deserialize<McpUiToolMeta>(McpApps.SerializerOptions);
            Assert.NotNull(ui);
            Assert.Equal(uri, ui.ResourceUri);
            Assert.Null(ui.Visibility);
            Assert.Equal(uri, tool.Meta["ui/resourceUri"]!.GetValue<string>());
            var contents = await host.Client.ReadResourceAsync(uri, cancellationToken: host.Token);
            var content = Assert.IsType<TextResourceContents>(Assert.Single(contents.Contents));
            Assert.Equal(uri, content.Uri);
            Assert.Equal(McpApps.HtmlMimeType, content.MimeType);
            Assert.StartsWith("<!DOCTYPE html>", content.Text);
            Assert.NotNull(content.Meta);
            var resourceUi = content.Meta["ui"]!.Deserialize<McpUiResourceMeta>(McpApps.SerializerOptions);
            Assert.NotNull(resourceUi);
            Assert.True(resourceUi.PrefersBorder);
        }
#pragma warning restore MCPEXP003
    }

    [Theory]
    [InlineData("2026-07-28", -32602)]
    [InlineData("2025-11-25", -32002)]
    public async Task MissingResource_WireErrorFollowsNegotiatedRevision(string protocol, int expectedCode)
    {
        await using var host = await TestServer.CreateAsync(protocol);
        Assert.Equal(protocol, host.Client.NegotiatedProtocolVersion);
        var error = await Assert.ThrowsAsync<McpProtocolException>(
            async () => await host.Client.ReadResourceAsync("dotnet://missing", cancellationToken: host.Token));
        Assert.Equal(expectedCode, (int)error.ErrorCode);
    }

    [Theory]
    [InlineData("2026-07-28", "NU1101", -32002)]
    [InlineData("2025-11-25", "NU1101", -32002)]
    [InlineData("2026-07-28", "capability", -32001)]
    [InlineData("2025-11-25", "capability", -32001)]
    [InlineData("2026-07-28", "capabilityWithReason", -32603)]
    [InlineData("2025-11-25", "capabilityWithReason", -32603)]
    public async Task ApplicationErrors_PreserveAnnotationsAndToolResultEnvelope(
        string protocol, string scenario, int expectedCode)
    {
        var response = scenario switch
        {
            "capability" => ErrorResultFactory.ReturnCapabilityNotAvailable("test feature", details: "Disabled"),
            "capabilityWithReason" => ErrorResultFactory.ReturnCapabilityNotAvailable("test feature", reason: "Disabled"),
            _ => Assert.IsType<ErrorResponse>(
                ErrorResultFactory.CreateResult("", $"error {scenario}: Resource not found", 1, "dotnet build"))
        };
        await using var host = await TestServer.CreateAsync(protocol, controlledError: response);
        Assert.Equal(protocol, host.Client.NegotiatedProtocolVersion);
        host.Release.TrySetResult();

        var result = await host.Client.CallToolAsync("controlled", cancellationToken: host.Token);
        Assert.True(result.IsError);
        var structured = Assert.IsType<JsonElement>(result.StructuredContent);
        Assert.Equal(JsonValueKind.Object, structured.ValueKind);
        Assert.False(structured.GetProperty("success").GetBoolean());
        Assert.Equal(response.ExitCode, structured.GetProperty("exitCode").GetInt32());
        Assert.False(structured.TryGetProperty("result", out _));
        var error = Assert.Single(structured.GetProperty("errors").EnumerateArray());
        Assert.Equal(Assert.Single(response.Errors).Code, error.GetProperty("code").GetString());
        Assert.Equal(expectedCode, error.GetProperty("mcpErrorCode").GetInt32());
        using var text = JsonDocument.Parse(Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
        Assert.True(JsonElement.DeepEquals(text.RootElement, structured));
    }

    [Fact]
    public async Task NativeResourceSubscriptions_RouteUpdatesAndStopOnCancellation()
    {
        await using var host = await TestServer.CreateAsync("2026-07-28");
        var acknowledgements = Channel.CreateUnbounded<JsonNode>();
        var updates = Channel.CreateUnbounded<JsonNode>();
        await using var acknowledgementHandler = host.Client.RegisterNotificationHandler(
            NotificationMethods.SubscriptionsAcknowledgedNotification, (notification, _) =>
            {
                acknowledgements.Writer.TryWrite(notification.Params!.DeepClone());
                return ValueTask.CompletedTask;
            });
        await using var updateHandler = host.Client.RegisterNotificationHandler(
            NotificationMethods.ResourceUpdatedNotification, (notification, _) =>
            {
                updates.Writer.TryWrite(notification.Params!.DeepClone());
                return ValueTask.CompletedTask;
            });
        using var firstCancellation = CancellationTokenSource.CreateLinkedTokenSource(host.Token);
        using var secondCancellation = CancellationTokenSource.CreateLinkedTokenSource(host.Token);
        var first = Listen("templates", "dotnet://templates", firstCancellation.Token);
        var second = Listen("sdk", "dotnet://sdk-info", secondCancellation.Token);
        try
        {
            await acknowledgements.Reader.ReadAsync(host.Token);
            await acknowledgements.Reader.ReadAsync(host.Token);
            await ClearCache();
            var received = new Dictionary<string, string>();
            for (var i = 0; i < 2; i++)
            {
                var update = await updates.Reader.ReadAsync(host.Token);
                received.Add(update["_meta"]![MetaKeys.SubscriptionId]!.GetValue<string>(), update["uri"]!.GetValue<string>());
            }
            Assert.Equal("dotnet://templates", received["templates"]);
            Assert.Equal("dotnet://sdk-info", received["sdk"]);
            Assert.False(updates.Reader.TryRead(out _));

            await host.Client.SendNotificationAsync(NotificationMethods.CancelledNotification,
                new CancelledNotificationParams { RequestId = new RequestId("templates") },
                cancellationToken: host.Token);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
            while (host.Subscriptions.IsSubscribed("dotnet://templates"))
                await Task.Delay(TimeSpan.FromMilliseconds(20), host.Token);
            await ClearCache();
            var remaining = await updates.Reader.ReadAsync(host.Token);
            Assert.Equal("sdk", remaining["_meta"]![MetaKeys.SubscriptionId]!.GetValue<string>());
            Assert.False(updates.Reader.TryRead(out _));
        }
        finally
        {
            await firstCancellation.CancelAsync();
            await secondCancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);
        }

        Task<JsonRpcResponse> Listen(string id, string uri, CancellationToken ct) => host.Client.SendRequestAsync(new()
        {
            Id = new RequestId(id),
            Method = RequestMethods.SubscriptionsListen,
            Params = JsonSerializer.SerializeToNode(new SubscriptionsListenRequestParams
            {
                Notifications = new() { ResourceSubscriptions = [uri] }
            }, McpJsonUtilities.DefaultOptions)
        }, ct);

        async Task ClearCache() => Assert.NotEqual(true, (await host.Client.CallToolAsync("dotnet_sdk",
            new Dictionary<string, object?> { ["action"] = "ClearTemplateCache" }, cancellationToken: host.Token)).IsError);
    }

    [Fact]
    public async Task LegacyResourceSubscription_ReceivesUpdatesWithoutSubscriptionId()
    {
        await using var host = await TestServer.CreateAsync("2025-11-25");
        var updated = new TaskCompletionSource<ResourceUpdatedNotificationParams>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var subscription = await host.Client.SubscribeToResourceAsync("dotnet://templates",
            (notification, _) =>
            {
                updated.TrySetResult(notification);
                return ValueTask.CompletedTask;
            }, cancellationToken: host.Token);
        await host.Client.CallToolAsync("dotnet_sdk",
            new Dictionary<string, object?> { ["action"] = "ClearTemplateCache" }, cancellationToken: host.Token);
        var result = await updated.Task.WaitAsync(host.Token);
        Assert.Equal("dotnet://templates", result.Uri);
        Assert.False(result.Meta?.ContainsKey(MetaKeys.SubscriptionId) == true);
    }

    [Fact]
    public void ToolDeserialization_RequiresInputSchema()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Tool>("""{"name":"missing-schema"}"""));
        Assert.NotNull(JsonSerializer.Deserialize<Tool>("""{"name":"valid","inputSchema":{"type":"object"}}"""));
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task CapabilitySnapshot_ReflectsTheConnection(string protocol)
    {
        await using var host = await TestServer.CreateAsync(protocol);
        var result = await host.Client.CallToolAsync("dotnet_server_capabilities", cancellationToken: host.Token);
        var capabilities = Assert.IsType<JsonElement>(result.StructuredContent);
        Assert.Equal(protocol, capabilities.GetProperty("protocolVersion").GetString());
        var supports = capabilities.GetProperty("supports");
        Assert.False(supports.GetProperty("asyncTasks").GetBoolean());
        Assert.False(supports.GetProperty("elicitation").GetBoolean());
        Assert.False(supports.GetProperty("sampling").GetBoolean());
        if (protocol == "2025-11-25")
        {
            await host.Client.PingAsync(cancellationToken: host.Token);
            await using var subscription = await host.Client.SubscribeToResourceAsync(
                "dotnet://templates", (_, _) => ValueTask.CompletedTask, cancellationToken: host.Token);
        }
    }

    [Fact]
#pragma warning disable MCP9005 // Exercise the retained per-request logging feature.
    public async Task CurrentLogging_IsOptedIntoPerRequest()
    {
        await using var host = await TestServer.CreateAsync("2026-07-28");
        var enabled = await host.Client.SendRequestAsync(new()
        {
            Id = new RequestId("logging-opt-in"),
            Method = RequestMethods.ToolsCall,
            Params = new JsonObject { ["name"] = "dotnet_server_capabilities" },
            Context = new() { LogLevel = LoggingLevel.Info }
        }, host.Token);
        Assert.True(enabled.Result!["structuredContent"]!["supports"]!["mcpLogging"]!.GetValue<bool>());
        var disabled = await host.Client.CallToolAsync("dotnet_server_capabilities", cancellationToken: host.Token);
        Assert.False(Assert.IsType<JsonElement>(disabled.StructuredContent).GetProperty("supports").GetProperty("mcpLogging").GetBoolean());
        var reenabled = await host.Client.SendRequestAsync(new()
        {
            Id = new RequestId("logging-opt-in-again"),
            Method = RequestMethods.ToolsCall,
            Params = new JsonObject { ["name"] = "dotnet_server_capabilities" },
            Context = new() { LogLevel = LoggingLevel.Warning }
        }, host.Token);
        Assert.True(reenabled.Result!["structuredContent"]!["supports"]!["mcpLogging"]!.GetValue<bool>());
    }

    [Fact]
    public async Task CurrentLogging_NotificationsDoNotLeakAcrossRequests()
    {
        await using var host = await TestServer.CreateAsync("2026-07-28");
        var notifications = Channel.CreateUnbounded<JsonNode>();
        await using var handler = host.Client.RegisterNotificationHandler(
            NotificationMethods.LoggingMessageNotification, (notification, _) =>
            {
                notifications.Writer.TryWrite(notification.Params!.DeepClone());
                return ValueTask.CompletedTask;
            });

        var requestNumber = 0;
        foreach (var level in new LoggingLevel?[] { LoggingLevel.Info, null, LoggingLevel.Info })
        {
            var project = $"missing{requestNumber}.csproj";
            await host.Client.SendRequestAsync(new()
            {
                Id = new RequestId($"logging-isolation-{requestNumber++}"),
                Method = RequestMethods.ToolsCall,
                Params = new JsonObject
                {
                    ["name"] = "dotnet_project",
                    ["arguments"] = new JsonObject { ["action"] = "Build", ["project"] = project }
                },
                Context = new() { LogLevel = level }
            }, host.Token);
            if (level is not null)
            {
                var notification = await notifications.Reader.ReadAsync(host.Token);
                Assert.Equal($"Building \"{project}\"...", notification["data"]!.GetValue<string>());
            }
            else
            {
                using var quietPeriod = CancellationTokenSource.CreateLinkedTokenSource(host.Token);
                quietPeriod.CancelAfter(TimeSpan.FromMilliseconds(200));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    async () => await notifications.Reader.ReadAsync(quietPeriod.Token));
            }
        }
        Assert.False(notifications.Reader.TryRead(out _));
    }

    [Theory]
    [InlineData("2026-07-28", null, false)]
    [InlineData("2026-07-28", LoggingLevel.Debug, true)]
    [InlineData("2026-07-28", LoggingLevel.Info, true)]
    [InlineData("2026-07-28", LoggingLevel.Warning, false)]
    [InlineData("2026-07-28", LoggingLevel.Error, false)]
    [InlineData("2025-11-25", null, true)]
    [InlineData("2025-11-25", LoggingLevel.Debug, true)]
    [InlineData("2025-11-25", LoggingLevel.Info, true)]
    [InlineData("2025-11-25", LoggingLevel.Warning, false)]
    [InlineData("2025-11-25", LoggingLevel.Error, false)]
    public async Task Logging_RespectsNegotiatedThresholdAndNotificationPayload(
        string protocol, LoggingLevel? level, bool expectedNotification)
    {
        await using var host = await TestServer.CreateAsync(protocol);
        var notifications = Channel.CreateUnbounded<JsonNode>();
        await using var handler = host.Client.RegisterNotificationHandler(
            NotificationMethods.LoggingMessageNotification, (notification, _) =>
            {
                notifications.Writer.TryWrite(notification.Params!.DeepClone());
                return ValueTask.CompletedTask;
            });
        if (protocol == "2025-11-25" && level is { } sessionLevel)
        {
            await host.Client.SendRequestAsync(new()
            {
                Id = new RequestId("session-log-level"),
                Method = RequestMethods.LoggingSetLevel,
                Params = new JsonObject { ["level"] = sessionLevel.ToString().ToLowerInvariant() }
            }, host.Token);
        }

        var response = await host.Client.SendRequestAsync(new()
        {
            Id = new RequestId("build-log-threshold"),
            Method = RequestMethods.ToolsCall,
            Params = new JsonObject
            {
                ["name"] = "dotnet_project",
                ["arguments"] = new JsonObject { ["action"] = "Build", ["project"] = "missing.csproj" }
            },
            Context = new() { LogLevel = protocol == "2026-07-28" ? level : null }
        }, host.Token);
        Assert.NotNull(response.Result);
        if (expectedNotification)
        {
            var notification = await notifications.Reader.ReadAsync(host.Token);
            Assert.Equal("info", notification["level"]!.GetValue<string>());
            Assert.Equal("dotnet-mcp", notification["logger"]!.GetValue<string>());
            Assert.Equal("Building \"missing.csproj\"...", notification["data"]!.GetValue<string>());
        }
        else
        {
            using var quietPeriod = CancellationTokenSource.CreateLinkedTokenSource(host.Token);
            quietPeriod.CancelAfter(TimeSpan.FromMilliseconds(200));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await notifications.Reader.ReadAsync(quietPeriod.Token));
        }
        Assert.False(notifications.Reader.TryRead(out _));
    }
#pragma warning restore MCP9005

    [Theory]
    [InlineData("2026-07-28", "Clean", "accept", true)]
    [InlineData("2026-07-28", "Clean", "accept", false)]
    [InlineData("2026-07-28", "Clean", "decline", true)]
    [InlineData("2026-07-28", "Clean", "cancel", true)]
    [InlineData("2025-11-25", "Clean", "accept", true)]
    [InlineData("2025-11-25", "Clean", "accept", false)]
    [InlineData("2025-11-25", "Clean", "decline", true)]
    [InlineData("2025-11-25", "Clean", "cancel", true)]
    [InlineData("2026-07-28", "Remove", "accept", true)]
    [InlineData("2026-07-28", "Remove", "accept", false)]
    [InlineData("2026-07-28", "Remove", "decline", true)]
    [InlineData("2026-07-28", "Remove", "cancel", true)]
    [InlineData("2025-11-25", "Remove", "accept", true)]
    [InlineData("2025-11-25", "Remove", "accept", false)]
    [InlineData("2025-11-25", "Remove", "decline", true)]
    [InlineData("2025-11-25", "Remove", "cancel", true)]
    public async Task Confirmation_RespectsActionAndBoolean(string protocol, string action, string answer, bool confirmed)
    {
        var confirmations = 0;
        await using var host = await TestServer.CreateAsync(protocol, (_, _) =>
        {
            Interlocked.Increment(ref confirmations);
            return ValueTask.FromResult(Confirmation(answer, confirmed));
        });
        var request = DestructiveRequest(action, host.DirectoryPath);
        var result = await host.Client.CallToolAsync(request, host.Token);
        Assert.Equal(1, confirmations);
        if (answer == "accept" && confirmed)
            Assert.DoesNotContain("operation cancelled", result.GetText());
        else
            Assert.Contains("operation cancelled", result.GetText());
    }

    [Theory]
    [InlineData("Clean")]
    [InlineData("Remove")]
    public async Task Confirmation_RejectsUnboundContinuation(string action)
    {
        await using var host = await TestServer.CreateAsync("2026-07-28", (_, _) =>
            ValueTask.FromResult(Confirmation("accept", true)));
        var request = DestructiveRequest(action, host.DirectoryPath);
        request.RequestState = "not-bound-to-this-operation";
        request.InputResponses = new Dictionary<string, InputResponse>
        {
            ["confirmation"] = InputResponse.FromElicitResult(Confirmation("accept", true))
        };
        var error = await Assert.ThrowsAsync<McpProtocolException>(
            async () => await host.Client.CallToolAsync(request, host.Token));
        Assert.Equal(McpErrorCode.InvalidParams, error.ErrorCode);
    }

    [Theory]
    [InlineData("2026-07-28")]
    [InlineData("2025-11-25")]
    public async Task ConfirmedClean_ExecutesOnlyOnceAfterInput(string protocol)
    {
        string? marker = null;
        var confirmations = 0;
        await using var host = await TestServer.CreateAsync(protocol, (_, _) =>
        {
            Assert.False(File.Exists(marker));
            Interlocked.Increment(ref confirmations);
            return ValueTask.FromResult(Confirmation("accept", true));
        });
        marker = Path.Join(host.DirectoryPath, "clean-count.txt");
        var project = Path.Join(host.DirectoryPath, "clean.csproj");
        await File.WriteAllTextAsync(project, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
              <Target Name="CountClean" AfterTargets="Clean">
                <WriteLinesToFile File="clean-count.txt" Lines="clean" Overwrite="false" />
              </Target>
            </Project>
            """, host.Token);
        var result = await host.Client.CallToolAsync("dotnet_project",
            new Dictionary<string, object?> { ["action"] = "Clean", ["project"] = project },
            cancellationToken: host.Token);
        Assert.NotEqual(true, result.IsError);
        Assert.Equal(1, confirmations);
        Assert.Single(await File.ReadAllLinesAsync(marker, host.Token));
    }

    [Fact]
    public async Task TaskCapability_IsScopedToEachRequestOnTheSameConnection()
    {
        await using var host = await TestServer.CreateAsync("2026-07-28");
        Assert.True(host.Client.ServerCapabilities.Extensions?.ContainsKey(TasksProtocol.ExtensionId));

        await AssertTaskSnapshot();
        var inline = await host.Client.CallToolAsync("dotnet_server_capabilities", cancellationToken: host.Token);
        Assert.NotEqual(true, inline.IsError);
        Assert.Equal("complete", inline.ResultType);
        Assert.False(Assert.IsType<JsonElement>(inline.StructuredContent).GetProperty("supports").GetProperty("asyncTasks").GetBoolean());
        await AssertTaskSnapshot();

        async Task AssertTaskSnapshot()
        {
            var task = await host.Client.CallToolAsTaskAsync(new() { Name = "dotnet_server_capabilities" }, host.Token);
            Assert.True(task.IsTask);
            Assert.Null(task.Result);
            Assert.Equal("task", task.TaskCreated!.ResultType);
            var completed = Assert.IsType<CompletedTaskResult>(
                await PollAsync(host, task.TaskCreated.TaskId, r => r is not WorkingTaskResult));
            var result = completed.Result.Deserialize<CallToolResult>();
            Assert.NotNull(result);
            Assert.NotEqual(true, result.IsError);
            Assert.True(Assert.IsType<JsonElement>(result.StructuredContent).GetProperty("supports").GetProperty("asyncTasks").GetBoolean());
        }
    }

    [Theory]
    [InlineData("2026-07-28", false)]
    [InlineData("2025-11-25", false)]
    [InlineData("2025-11-25", true)]
    public async Task TaskCapability_RequiresRegistrationAndCurrentProtocol(string protocol, bool enableTasks)
    {
        await using var host = await TestServer.CreateAsync(protocol, enableTasks: enableTasks);
        Assert.Equal(enableTasks, host.Client.ServerCapabilities.Extensions?.ContainsKey(TasksProtocol.ExtensionId) == true);
        var response = await host.Client.CallToolAsTaskAsync(new() { Name = "dotnet_server_capabilities" }, host.Token);
        Assert.False(response.IsTask);
        Assert.Null(response.TaskCreated);
        var result = Assert.IsType<CallToolResult>(response.Result);
        Assert.NotEqual(true, result.IsError);
        Assert.Equal(protocol == "2026-07-28" ? "complete" : null, result.ResultType);
        var capabilities = Assert.IsType<JsonElement>(result.StructuredContent);
        Assert.Equal(protocol, capabilities.GetProperty("protocolVersion").GetString());
        Assert.False(capabilities.GetProperty("supports").GetProperty("asyncTasks").GetBoolean());
    }

    [Fact]
    public async Task TaskProtocolFailure_UsesFailedState()
    {
        await using var host = await TestServer.CreateAsync("2026-07-28");
        var task = await host.Client.CallToolAsTaskAsync(new() { Name = "missing-tool" }, host.Token);
        Assert.True(task.IsTask);
        Assert.IsType<FailedTaskResult>(
            await PollAsync(host, task.TaskCreated!.TaskId, r => r is not WorkingTaskResult));
    }

    [Fact]
    public async Task TaskLifecycle_PollsToCompletionAndRetainsApplicationErrors()
    {
        await using var host = await TestServer.CreateAsync("2026-07-28");
        var task = await host.Client.CallToolAsTaskAsync(new() { Name = "controlled" }, host.Token);
        Assert.True(task.IsTask);
        await host.Entered.Task.WaitAsync(host.Token);
        Assert.IsType<WorkingTaskResult>(await host.Client.GetTaskAsync(task.TaskCreated!.TaskId, host.Token));
        host.Release.TrySetResult();
        var completed = Assert.IsType<CompletedTaskResult>(await PollAsync(host, task.TaskCreated.TaskId, r => r is not WorkingTaskResult));
        var result = completed.Result.Deserialize<CallToolResult>();
        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.Equal("controlled application error", result.GetText());
        Assert.Equal(1, host.Invocations);
    }

    [Fact]
    public async Task TaskCancellation_StopsTheRunningHandler()
    {
        await using var host = await TestServer.CreateAsync("2026-07-28");
        var task = await host.Client.CallToolAsTaskAsync(new() { Name = "controlled" }, host.Token);
        Assert.True(task.IsTask);
        await host.Entered.Task.WaitAsync(host.Token);
        await host.Client.CancelTaskAsync(task.TaskCreated!.TaskId, host.Token);
        Assert.IsType<CancelledTaskResult>(await host.Client.GetTaskAsync(task.TaskCreated.TaskId, host.Token));
        await host.Exited.Task.WaitAsync(host.Token);
    }

#pragma warning disable MCP9005 // Exercise retained Roots/Sampling compatibility and cancellation.
    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public async Task LegacyRoots_DiscoverOnlyUnambiguousProjectAndSolution(int fileCount, bool hasRoot)
    {
        var roots = new List<Root>();
        var requests = 0;
        await using var host = await TestServer.CreateAsync("2025-11-25", handlers: new()
        {
            RootsHandler = (_, _) =>
            {
                Interlocked.Increment(ref requests);
                return ValueTask.FromResult(new ListRootsResult
                {
                    Roots = roots
                });
            }
        });
        if (hasRoot)
            roots.Add(new Root { Uri = new Uri(host.DirectoryPath + Path.DirectorySeparatorChar).AbsoluteUri });
        for (var i = 0; i < fileCount; i++)
        {
            await File.WriteAllTextAsync(Path.Join(host.DirectoryPath, $"project{i}.csproj"), "<Project />", host.Token);
            await File.WriteAllTextAsync(Path.Join(host.DirectoryPath, $"solution{i}.slnx"), "<Solution />", host.Token);
        }

        var project = await WorkspaceDiscovery.TryFindProjectInRootsAsync(host.Server, host.Token);
        var solution = await WorkspaceDiscovery.TryFindSolutionInRootsAsync(host.Server, host.Token);
        Assert.Equal(fileCount == 1 ? Path.Join(host.DirectoryPath, "project0.csproj") : null, project);
        Assert.Equal(fileCount == 1 ? Path.Join(host.DirectoryPath, "solution0.slnx") : null, solution);
        Assert.Equal(2, requests);

        if (fileCount == 1)
        {
            var discoveredSolution = await host.Client.CallToolAsync("dotnet_solution",
                new Dictionary<string, object?> { ["action"] = "List" }, cancellationToken: host.Token);
            Assert.NotEqual(true, discoveredSolution.IsError);
            Assert.Equal(3, requests);
            var discoveredProject = await host.Client.CallToolAsync("dotnet_project",
                new Dictionary<string, object?> { ["action"] = "Build" }, cancellationToken: host.Token);
            MachineReadableCommandAssertions.AssertExecutedDotnetCommand(discoveredProject.GetText(),
                $"dotnet build \"{project}\"");
            Assert.Equal(4, requests);
        }
        var discoveryRequests = requests;
        var explicitSolution = Path.Join(host.DirectoryPath, "explicit.slnx");
        await File.WriteAllTextAsync(explicitSolution, "<Solution />", host.Token);
        var result = await host.Client.CallToolAsync("dotnet_solution",
            new Dictionary<string, object?> { ["action"] = "List", ["solution"] = explicitSolution },
            cancellationToken: host.Token);
        Assert.NotEqual(true, result.IsError);
        var explicitProject = await host.Client.CallToolAsync("dotnet_project",
            new Dictionary<string, object?> { ["action"] = "Build", ["project"] = "explicit.csproj" },
            cancellationToken: host.Token);
        MachineReadableCommandAssertions.AssertExecutedDotnetCommand(explicitProject.GetText(),
            "dotnet build \"explicit.csproj\"");
        Assert.Equal(discoveryRequests, requests);
    }

    [Theory]
    [InlineData("Build", false)]
    [InlineData("Test", false)]
    [InlineData("Build", true)]
    public async Task LegacySampling_AnalyzesOnlyCommandFailures(string action, bool succeeds)
    {
        var requests = 0;
        await using var host = await TestServer.CreateAsync("2025-11-25", handlers: new()
        {
            SamplingHandler = (_, _, _) =>
            {
                Interlocked.Increment(ref requests);
                return ValueTask.FromResult(new CreateMessageResult
                {
                    Role = Role.Assistant,
                    Model = "test-model",
                    Content = [new TextContentBlock { Text = "Check that the project file exists." }]
                });
            }
        });
        var snapshot = await host.Client.CallToolAsync("dotnet_server_capabilities", cancellationToken: host.Token);
        Assert.True(Assert.IsType<JsonElement>(snapshot.StructuredContent)
            .GetProperty("supports").GetProperty("sampling").GetBoolean());
        if (succeeds)
        {
            await File.WriteAllTextAsync(Path.Join(host.DirectoryPath, "project.csproj"),
                """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>""",
                host.Token);
        }
        var result = await host.Client.CallToolAsync("dotnet_project",
            new Dictionary<string, object?>
            {
                ["action"] = action, ["project"] = succeeds ? "project.csproj" : "missing.csproj",
                ["noRestore"] = !succeeds
            }, cancellationToken: host.Token);
        if (succeeds)
        {
            Assert.Contains("Build succeeded.", result.GetText());
            Assert.DoesNotContain("AI Analysis:", result.GetText());
        }
        else
        {
            Assert.Contains("AI Analysis:\nCheck that the project file exists.", result.GetText());
        }
        Assert.Equal(succeeds ? 0 : 1, requests);
    }

    [Theory]
    [InlineData("2025-11-25", "Build")]
    [InlineData("2025-11-25", "Test")]
    [InlineData("2026-07-28", "Build")]
    [InlineData("2026-07-28", "Test")]
    public async Task Sampling_WithoutClientSupportPreservesCommandFailure(string protocol, string action)
    {
        await using var host = await TestServer.CreateAsync(protocol);
        var result = await host.Client.CallToolAsync("dotnet_project",
            new Dictionary<string, object?> { ["action"] = action, ["project"] = "missing.csproj", ["noRestore"] = true },
            cancellationToken: host.Token);
        Assert.Contains("Exit Code:", result.GetText());
        Assert.DoesNotContain("Exit Code: 0", result.GetText());
        Assert.DoesNotContain("AI Analysis:", result.GetText());
    }

    [Fact]
    public async Task CurrentSampling_CapabilityMetadataDoesNotLeakAcrossRequests()
    {
        var advertiseSampling = true;
        await using var host = await TestServer.CreateAsync("2026-07-28", handlers: new()
        {
            SamplingHandler = (_, _, _) => throw new InvalidOperationException("Capability discovery must not sample.")
        }, transformToolRequest: request =>
        {
            if (!advertiseSampling)
                Assert.IsType<JsonObject>(request.Params!["_meta"])[MetaKeys.ClientCapabilities] = new JsonObject();
        });

        foreach (var enabled in new[] { true, false, true })
        {
            advertiseSampling = enabled;
            var result = await host.Client.CallToolAsync("dotnet_server_capabilities", cancellationToken: host.Token);
            Assert.Equal(enabled, Assert.IsType<JsonElement>(result.StructuredContent)
                .GetProperty("supports").GetProperty("sampling").GetBoolean());
            if (!enabled)
            {
                var failure = await host.Client.CallToolAsync("dotnet_project",
                    new Dictionary<string, object?> { ["action"] = "Build", ["project"] = "missing.csproj" },
                    cancellationToken: host.Token);
                Assert.Contains("Exit Code:", failure.GetText());
                Assert.DoesNotContain("AI Analysis:", failure.GetText());
            }
        }
    }

    [Theory]
    [InlineData("dotnet_project", "Build")]
    [InlineData("dotnet_package", "List")]
    [InlineData("dotnet_solution", "List")]
    public async Task TaskCancellation_StopsPendingRootsDiscovery(string tool, string action)
    {
        await using var host = await TestServer.CreateAsync("2026-07-28", handlers: new()
        {
            RootsHandler = (_, _) => throw new InvalidOperationException("Task input must be handled through tasks/update.")
        });
        var task = await host.Client.CallToolAsTaskAsync(new()
        {
            Name = tool,
            Arguments = new Dictionary<string, JsonElement> { ["action"] = JsonSerializer.SerializeToElement(action) }
        }, host.Token);
        Assert.True(task.IsTask);
        var input = Assert.IsType<InputRequiredTaskResult>(
            await PollAsync(host, task.TaskCreated!.TaskId, r => r is not WorkingTaskResult));
        Assert.Equal("roots/list", Assert.Single(input.InputRequests!).Value.Method);

        await host.Client.CancelTaskAsync(task.TaskCreated.TaskId, host.Token);
        await host.InvocationExited.Task.WaitAsync(TimeSpan.FromSeconds(10), host.Token);
        Assert.True(host.InvocationCancelled.Task.IsCompletedSuccessfully);
        Assert.IsType<CancelledTaskResult>(await host.Client.GetTaskAsync(task.TaskCreated.TaskId, host.Token));
    }

    [Theory]
    [InlineData("Build")]
    [InlineData("Test")]
    public async Task TaskCancellation_StopsPendingSampling(string action)
    {
        await using var host = await TestServer.CreateAsync("2026-07-28", handlers: new()
        {
            SamplingHandler = (_, _, _) => throw new InvalidOperationException("Task input must be handled through tasks/update.")
        });
        var task = await host.Client.CallToolAsTaskAsync(new()
        {
            Name = "dotnet_project",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["action"] = JsonSerializer.SerializeToElement(action),
                ["project"] = JsonSerializer.SerializeToElement(Path.Join(host.DirectoryPath, "missing.csproj")),
                ["noRestore"] = JsonSerializer.SerializeToElement(true)
            }
        }, host.Token);
        Assert.True(task.IsTask);
        var input = Assert.IsType<InputRequiredTaskResult>(
            await PollAsync(host, task.TaskCreated!.TaskId, r => r is not WorkingTaskResult));
        Assert.Equal("sampling/createMessage", Assert.Single(input.InputRequests!).Value.Method);

        await host.Client.CancelTaskAsync(task.TaskCreated.TaskId, host.Token);
        await host.InvocationExited.Task.WaitAsync(TimeSpan.FromSeconds(10), host.Token);
        Assert.True(host.InvocationCancelled.Task.IsCompletedSuccessfully);
        Assert.IsType<CancelledTaskResult>(await host.Client.GetTaskAsync(task.TaskCreated.TaskId, host.Token));
    }
#pragma warning restore MCP9005

    [Fact]
    public async Task TaskCancellation_TerminatesTheCliProcessTree()
    {
        await using var host = await TestServer.CreateAsync("2026-07-28", handlers: new()
        {
#pragma warning disable MCP9005 // A cancelled build must not enter the retained Sampling path.
            SamplingHandler = (_, _, _) => throw new InvalidOperationException("A cancelled build must not request analysis.")
#pragma warning restore MCP9005
        });
        var marker = Path.Join(host.DirectoryPath, "child.pid");
        var command = OperatingSystem.IsWindows()
            ? $"powershell.exe -NoProfile -NonInteractive -Command \"[IO.File]::WriteAllText('{marker.Replace("'", "''")}.tmp', [string]$PID); [IO.File]::Move('{marker.Replace("'", "''")}.tmp', '{marker.Replace("'", "''")}'); Start-Sleep -Seconds 60\""
            : $"/bin/sh -c 'echo $$ > \"{marker}.tmp\"; mv \"{marker}.tmp\" \"{marker}\"; exec sleep 60'";
        var project = Path.Join(host.DirectoryPath, "wait.csproj");
        var document = new XDocument(new XElement("Project",
            new XElement("Target", new XAttribute("Name", "Build"),
                new XElement("Exec", new XAttribute("Command", command)))));
        await File.WriteAllTextAsync(project, document.ToString(), host.Token);
        var task = await host.Client.CallToolAsTaskAsync(new()
        {
            Name = "dotnet_project",
            Arguments = new Dictionary<string, JsonElement>
            {
                ["action"] = JsonSerializer.SerializeToElement("Build"),
                ["project"] = JsonSerializer.SerializeToElement(project),
                ["noRestore"] = JsonSerializer.SerializeToElement(true)
            }
        }, host.Token);
        Assert.True(task.IsTask);
        var taskId = task.TaskCreated!.TaskId;
        Process? child = null;
        try
        {
            while (!File.Exists(marker))
            {
                Assert.IsType<WorkingTaskResult>(await host.Client.GetTaskAsync(taskId, host.Token));
                await Task.Delay(TimeSpan.FromMilliseconds(20), host.Token);
            }
            var pidText = await File.ReadAllTextAsync(marker, host.Token);
            child = Process.GetProcessById(int.Parse(pidText.Trim(), System.Globalization.CultureInfo.InvariantCulture));
            await host.Client.CancelTaskAsync(taskId, host.Token);
            await child.WaitForExitAsync(host.Token).WaitAsync(TimeSpan.FromSeconds(10), host.Token);
            await host.InvocationExited.Task.WaitAsync(TimeSpan.FromSeconds(10), host.Token);
            Assert.True(host.InvocationCancelled.Task.IsCompletedSuccessfully);
            Assert.True(child.HasExited);
            Assert.IsType<CancelledTaskResult>(await host.Client.GetTaskAsync(taskId, host.Token));
        }
        finally
        {
            if (child is not null)
            {
                if (!child.HasExited)
                    child.Kill(entireProcessTree: true);
                child.Dispose();
            }
            if (await host.Client.GetTaskAsync(taskId, host.Token) is WorkingTaskResult or InputRequiredTaskResult)
                await host.Client.CancelTaskAsync(taskId, host.Token);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("allow")]
    public async Task TaskConfirmation_UsesInputRequiredAndUpdateWithoutReexecuting(string? governance)
    {
        await using var host = await TestServer.CreateAsync("2026-07-28", (_, _) =>
            throw new InvalidOperationException("Manual task polling must not automatically resolve input."), governance);
        var task = await host.Client.CallToolAsTaskAsync(DestructiveRequest("Clean", host.DirectoryPath), host.Token);
        Assert.True(task.IsTask);
        var input = Assert.IsType<InputRequiredTaskResult>(
            await PollAsync(host, task.TaskCreated!.TaskId, r => r is not WorkingTaskResult));
        Assert.NotNull(input.InputRequests);
        var request = Assert.Single(input.InputRequests);
        Assert.Equal("elicitation/create", request.Value.Method);
        await host.Client.UpdateTaskAsync(new()
        {
            TaskId = task.TaskCreated.TaskId,
            InputResponses = new Dictionary<string, InputResponse>
            {
                [request.Key] = InputResponse.FromElicitResult(Confirmation("decline", false))
            }
        }, host.Token);
        var completed = Assert.IsType<CompletedTaskResult>(
            await PollAsync(host, task.TaskCreated.TaskId, r => r is CompletedTaskResult or FailedTaskResult or CancelledTaskResult));
        Assert.Contains("Clean operation cancelled.", completed.Result.Deserialize<CallToolResult>()!.GetText());
    }

    [Fact]
    public async Task TaskConfirmation_CanBeCancelledWhileAwaitingInput()
    {
        await using var host = await TestServer.CreateAsync("2026-07-28", (_, _) =>
            throw new InvalidOperationException("Manual task polling must not automatically resolve input."));
        var task = await host.Client.CallToolAsTaskAsync(DestructiveRequest("Clean", host.DirectoryPath), host.Token);
        Assert.IsType<InputRequiredTaskResult>(
            await PollAsync(host, task.TaskCreated!.TaskId, r => r is not WorkingTaskResult));
        await host.Client.CancelTaskAsync(task.TaskCreated.TaskId, host.Token);
        Assert.IsType<CancelledTaskResult>(await host.Client.GetTaskAsync(task.TaskCreated.TaskId, host.Token));
    }

    [Fact]
    public async Task RequestCancellation_ReachesExecutorAndRestoresPreviousScope()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var previous = DotNetCommandExecutor.RequestCancellationToken.Value;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await DotNetCommandExecutor.WithRequestCancellationAsync<string>(cancelled.Token,
                async () => await DotNetCommandExecutor.ExecuteCommandAsync("--info", cancellationToken: TestContext.Current.CancellationToken)));
        Assert.Equal(previous, DotNetCommandExecutor.RequestCancellationToken.Value);
    }

    [Fact]
    public async Task Governance_FullCatalogFailsClosedOnExistingPasswordSchema()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await TestServer.CreateAsync("2026-07-28", governance: "allow", fullCatalog: true));
        Assert.Contains("dotnet_dev_certs", error.Message);
        Assert.Contains("\"password\"", error.Message);
    }

    [Fact]
    public async Task Governance_EnforcesPoliciesAcrossMrtrConfirmation()
    {
        var confirmations = 0;
        await using var host = await TestServer.CreateAsync("2026-07-28", (_, _) =>
        {
            Interlocked.Increment(ref confirmations);
            return ValueTask.FromResult(Confirmation("decline", false));
        }, "allow");
        var result = await host.Client.CallToolAsync(DestructiveRequest("Clean", host.DirectoryPath), host.Token);
        Assert.Equal(1, confirmations);
        Assert.Contains("Clean operation cancelled.", result.GetText());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Governance_EnforcesPoliciesForInlineAndTaskCalls(bool deny, bool asTask)
    {
        await using var host = await TestServer.CreateAsync("2026-07-28", governance: deny ? "deny" : "allow");
        host.Release.TrySetResult();
        CallToolResult result;
        if (asTask)
        {
            var task = await host.Client.CallToolAsTaskAsync(new() { Name = "dotnet_server_info" }, host.Token);
            Assert.True(task.IsTask);
            var completed = Assert.IsType<CompletedTaskResult>(
                await PollAsync(host, task.TaskCreated!.TaskId, r => r is not WorkingTaskResult));
            result = completed.Result.Deserialize<CallToolResult>()!;
        }
        else
        {
            result = await host.Client.CallToolAsync("dotnet_server_info", cancellationToken: host.Token);
        }
        Assert.Equal(deny, result.IsError == true);
        Assert.Equal(deny, result.GetText().Contains("blocked by governance policy", StringComparison.Ordinal));
    }

    private static ElicitResult Confirmation(string action, bool confirmed) => new()
    {
        Action = action,
        Content = new Dictionary<string, JsonElement> { ["confirmed"] = JsonSerializer.SerializeToElement(confirmed) }
    };

    private static CallToolRequestParams DestructiveRequest(string action, string directory) => new()
    {
        Name = action == "Clean" ? "dotnet_project" : "dotnet_solution",
        Arguments = new Dictionary<string, JsonElement>
        {
            ["action"] = JsonSerializer.SerializeToElement(action),
            [action == "Clean" ? "project" : "solution"] = JsonSerializer.SerializeToElement(Path.Join(directory, action == "Clean" ? "missing.csproj" : "missing.sln")),
            ["projects"] = JsonSerializer.SerializeToElement(new[] { "missing.csproj" })
        }
    };

    private static async Task<GetTaskResult> PollAsync(TestServer host, string taskId, Func<GetTaskResult, bool> finished)
    {
        while (true)
        {
            var result = await host.Client.GetTaskAsync(taskId, host.Token);
            if (finished(result))
                return result;
            await Task.Delay(TimeSpan.FromMilliseconds(20), host.Token);
        }
    }

    private sealed class TestServer : IAsyncDisposable
    {
        private readonly CancellationTokenSource _timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        private ServiceProvider? _provider;
        private Task? _serverRun;
        public McpClient Client { get; private set; } = null!;
        public string DirectoryPath { get; } = Path.Join(Path.GetTempPath(), $"dotnet-mcp-sdk20-{Guid.NewGuid():N}");
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Exited { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource InvocationExited { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource InvocationCancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Invocations;
        public CancellationToken Token => _timeout.Token;
        public ResourceSubscriptionManager Subscriptions => _provider!.GetRequiredService<ResourceSubscriptionManager>();
        public McpServer Server => _provider!.GetRequiredService<McpServer>();

        public static async Task<TestServer> CreateAsync(
            string protocol,
            Func<ElicitRequestParams?, CancellationToken, ValueTask<ElicitResult>>? elicit = null,
            string? governance = null,
            bool fullCatalog = false,
            McpClientHandlers? handlers = null,
            Action<Activity?>? observeToolActivity = null,
            Action<JsonRpcRequest>? transformToolRequest = null,
            bool enableTasks = true,
            ErrorResponse? controlledError = null)
        {
            var host = new TestServer();
            host._timeout.CancelAfter(TimeSpan.FromSeconds(60));
            Directory.CreateDirectory(host.DirectoryPath);
            try
            {
                Pipe requests = new(), responses = new();
                var services = new ServiceCollection();
                services.AddLogging();
                services.AddSingleton<ConcurrencyManager>();
                services.AddSingleton<ProcessSessionManager>();
                services.AddSingleton<ResourceSubscriptionManager>();
                services.AddOptions<McpServerOptions>()
                    .Configure<ResourceSubscriptionManager>((options, subscriptions) => subscriptions.Configure(options));
                services.AddTransient<DotNetCliTools>();
                var builder = services.AddMcpServer(options =>
                    options.Filters.Request.CallToolFilters.Add(next => async (context, ct) =>
                    {
                        observeToolActivity?.Invoke(Activity.Current);
                        var previous = DotNetCommandExecutor.WorkingDirectoryOverride.Value;
                        DotNetCommandExecutor.WorkingDirectoryOverride.Value = host.DirectoryPath;
                        try
                        {
                            return await DotNetCommandExecutor.WithRequestCancellationAsync(ct, () => next(context, ct));
                        }
                        catch (OperationCanceledException) when (ct.IsCancellationRequested)
                        {
                            host.InvocationCancelled.TrySetResult();
                            throw;
                        }
                        finally
                        {
                            DotNetCommandExecutor.WorkingDirectoryOverride.Value = previous;
                            host.InvocationExited.TrySetResult();
                        }
                    }))
                    .WithStreamServerTransport(requests.Reader.AsStream(), responses.Writer.AsStream())
                    .WithTools([McpServerTool.Create(async (CancellationToken ct) =>
                    {
                        Assert.Equal(ct, DotNetCommandExecutor.RequestCancellationToken.Value);
                        Interlocked.Increment(ref host.Invocations);
                        host.Entered.TrySetResult();
                        try
                        {
                            await host.Release.Task.WaitAsync(ct);
                            if (controlledError is not null)
                            {
                                var result = StructuredContentHelper.ToCallToolResult(
                                    ErrorResultFactory.ToJson(controlledError), controlledError);
                                result.IsError = true;
                                return result;
                            }
                            return new CallToolResult { IsError = true, Content = [new TextContentBlock { Text = "controlled application error" }] };
                        }
                        finally { host.Exited.TrySetResult(); }
                    }, new() { Name = "controlled" })])
                    .WithResources<DotNetResources>()
                    .WithResources<McpAppsResources>()
                    .WithSubscribeToResourcesHandler((context, _) =>
                    {
                        host.Subscriptions.Subscribe(context.Params!.Uri);
                        return ValueTask.FromResult(new EmptyResult());
                    })
                    .WithUnsubscribeFromResourcesHandler((context, _) =>
                    {
                        host.Subscriptions.Unsubscribe(context.Params!.Uri);
                        return ValueTask.FromResult(new EmptyResult());
                    })
                    .WithPrompts<DotNetPrompts>();
                if (enableTasks)
                    builder.WithTasks(new InMemoryMcpTaskStore());
                if (governance is null || fullCatalog)
                {
                    builder.WithDotNetCliTools();
                }
                else
                {
                    // Keep the scanner enabled. These production tools avoid the catalog's
                    // existing password field, whose fail-closed rejection is tested separately.
                    builder.WithTools(new[] { nameof(DotNetCliTools.DotnetServerInfo), nameof(DotNetCliTools.DotnetProject) }
                        .Select(name => ToolOutputSchemas.CreateTool(typeof(DotNetCliTools).GetMethod(name)!)));
                }
#pragma warning disable MCPEXP003 // Match production's experimental MCP Apps registration.
                builder.WithMcpApps();
#pragma warning restore MCPEXP003
                if (governance is not null)
                {
                    var policy = Path.Join(host.DirectoryPath, "policy.yaml");
                    await File.WriteAllTextAsync(policy, $"name: sdk20-test\nversion: '1.0'\ndefault_action: {governance}\nrules: []\n", host.Token);
                    builder.WithGovernance(options =>
                    {
                        options.RequireAuthenticatedAgentId = false;
                        options.PolicyPaths.Add(policy);
                    });
                }
                host._provider = services.BuildServiceProvider();
                host._serverRun = host._provider.GetRequiredService<McpServer>().RunAsync(host.Token);
                IClientTransport transport = new PerRequestLoggingTransport(
                    new StreamClientTransport(requests.Writer.AsStream(), responses.Reader.AsStream()));
                if (transformToolRequest is not null)
                    transport = new ToolRequestTransport(transport, transformToolRequest);
                host.Client = await McpClient.CreateAsync(
                    transport,
                    new()
                    {
                        ProtocolVersion = protocol,
                        // Use the fixture timeout rather than misclassifying a busy modern server as legacy.
                        DiscoverProbeTimeout = Timeout.InfiniteTimeSpan,
                        Handlers = handlers ?? new() { ElicitationHandler = elicit }
                    },
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
            Release.TrySetResult();
            if (Client is not null)
                await Client.DisposeAsync();
            await _timeout.CancelAsync();
            if (_serverRun is not null)
            {
                try { await _serverRun; }
                catch (OperationCanceledException) when (_timeout.IsCancellationRequested) { }
            }
            if (_provider is not null)
                await _provider.DisposeAsync();
            _timeout.Dispose();
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }

    // Transform after SDK metadata injection, before stream serialization, to test
    // extraction from the wire independently of the client's ambient activity.
    private sealed class ToolRequestTransport(IClientTransport inner, Action<JsonRpcRequest> transform) : IClientTransport
    {
        public string Name => inner.Name;

        public async Task<ITransport> ConnectAsync(CancellationToken cancellationToken = default)
            => new ToolTransport(await inner.ConnectAsync(cancellationToken), transform);

        private sealed class ToolTransport(ITransport inner, Action<JsonRpcRequest> transform) : ITransport
        {
            public string? SessionId => inner.SessionId;
            public ChannelReader<JsonRpcMessage> MessageReader => inner.MessageReader;

            public Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default)
            {
                if (message is JsonRpcRequest { Method: RequestMethods.ToolsCall } request)
                    transform(request);
                return inner.SendMessageAsync(message, cancellationToken);
            }

            public ValueTask DisposeAsync() => inner.DisposeAsync();
        }
    }
}
