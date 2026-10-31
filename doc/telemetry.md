# Telemetry and Observability

This document describes the telemetry and observability features available in dotnet-mcp, using the MCP C# SDK 2.3.

## Overview

dotnet-mcp provides comprehensive telemetry and observability through:

1. **Built-in SDK Telemetry** - Request duration logging, distributed trace-context propagation, and OpenTelemetry semantic conventions provided by the MCP C# SDK
2. **Structured Logging** - Microsoft.Extensions.Logging with configurable log levels
3. **In-memory Metrics** - Tool invocation summaries and opt-in token savings estimates, separate from SDK instrumentation
4. **OpenTelemetry Export** - Optional source-built server customization; not enabled in the shipped tool

## Built-in SDK Telemetry

The MCP C# SDK provides an ActivitySource and Meter both named **`Experimental.ModelContextProtocol`**, aligned with OpenTelemetry MCP semantic conventions. Register this exact name when subscribing to traces or metrics; `ModelContextProtocol` and `DotNetMcp` are not substitutes.

When listeners enable instrumentation, SDK request spans identify the MCP method and target (tool name, resource URI, or prompt name). Request-duration histograms such as `mcp.server.operation.duration` use seconds.

### Request Duration Logging

All MCP request handlers automatically log request duration:

- **`LogRequestHandlerCompleted`** - Successful request completion with duration
- **`LogRequestHandlerException`** - Failed request with duration and exception details

These logs are emitted automatically by the SDK and include:

- Request method (tool invocation, resource access, etc.)
- Request parameters
- Execution duration (in milliseconds)
- Success/failure status
- Exception details (if applicable)

dotnet-mcp also layers MCP progress notifications on top of this for long-running operations such as restore, build, test, publish, pack, tool installation, and workload management. For task-augmented requests, the server keeps progress updates within the MCP request/task lifecycle rather than relying on ad-hoc console output.

### Log Examples

```text
info: ModelContextProtocol.Server.McpServer[LogRequestHandlerCompleted]
      Request handler completed: tools/call (DotnetSdkVersion) in 125ms
      
info: ModelContextProtocol.Server.McpServer[LogRequestHandlerException]
      Request handler failed: tools/call (DotnetProjectBuild) in 3450ms
      Exception: System.InvalidOperationException: Build failed with exit code 1
```

## Distributed Trace Context (SEP-414)

The SDK implements W3C Trace Context propagation across JSON-RPC messages, including dotnet-mcp's stdio transport. Trace fields travel as strings in **`params._meta`**, not HTTP headers or top-level JSON-RPC properties:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "dotnet_server_info",
    "_meta": {
      "traceparent": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
      "tracestate": "vendor=example"
    }
  }
}
```

For an instrumented client/server pair, the span chain is **caller -> SDK client request -> SDK server request**:

- The SDK injects the outgoing request activity's context into `traceparent` and, when present, `tracestate`.
- The server extracts the incoming context and starts an SDK server activity with that remote parent. Its trace ID and trace state are preserved, while it has its own span ID.
- `Activity.Current` in the server's tool handler and request filters is the server activity, so additional instrumented operations can join the same trace.
- Missing or invalid `traceparent` does not fail an otherwise valid tool call. With instrumentation enabled, it starts an unparented server span rather than reusing a previous request's parent.

This behavior is covered for both `2026-07-28` and legacy `2025-11-25` connections. Propagation stays SDK-owned; dotnet-mcp does not add a second extraction/injection layer or duplicate tool spans.

### Sampling and Export

The SDK creates request activities only when its ActivitySource has a listener and sampling permits an activity. Outgoing injection depends on the activity available to the SDK; it can also reuse an outer instrumented `execute_tool ...` activity. A listener that requests propagation data can enable context flow without exporting recorded spans.

The shipped `Community.Mcp.DotNet` server does **not** register an OpenTelemetry provider or exporter. No traces or metrics are sent to a collector by default. Incoming trace metadata alone does not configure tracing, and setting `OTEL_EXPORTER_OTLP_ENDPOINT` alone does not enable export. To record/export SDK spans, both client and server processes need appropriate instrumentation; see the source-built example below.

### Coexistence with Server Metrics

`ToolMetricsAccumulator` and the token savings services in `DotNetMcp/Telemetry` collect in-memory summaries exposed by `dotnet_server_metrics`. They do not define an ActivitySource or Meter and do not compete with `Experimental.ModelContextProtocol`. Their counts, timing summaries, and token estimates remain separate from SDK request-duration histograms; subscribing to the SDK Meter does not export these accumulators automatically.

## Structured Logging Configuration

### Default Configuration

dotnet-mcp uses Microsoft.Extensions.Logging with console output to stderr:

```csharp
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
```

### Log Levels

Configure log levels via environment variables or `appsettings.json`:

```bash
# Set minimum log level
export Logging__LogLevel__Default=Information

# Enable debug logging for MCP SDK
export Logging__LogLevel__ModelContextProtocol=Debug

# Enable trace logging for dotnet-mcp
export Logging__LogLevel__DotNetMcp=Trace
```

Or via `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "ModelContextProtocol": "Debug",
      "DotNetMcp": "Trace"
    }
  }
}
```

> **Note**: The `appsettings.json` files in this repository are for local development of dotnet-mcp and are not packaged with the `Community.Mcp.DotNet` tool. When using the installed tool (for example via `dnx`), environment variables are the recommended way to configure logging and other settings, since they work regardless of installation method. If you prefer configuration files, create an `appsettings.json` in your working directory with the desired settings.

### Log Categories

- **`ModelContextProtocol.*`** - SDK-level logs (request handling, transport, serialization)
- **`DotNetMcp.*`** - Server-level logs (tool execution, resource access, errors)
- **`Microsoft.Hosting.*`** - Hosting infrastructure logs

## OpenTelemetry Integration (Optional)

The following is an **optional customization of a source-built MCP server**, not a runtime configuration feature of the installed `Community.Mcp.DotNet` tool. The package is supported only as an MCP server, not as a library for other applications.

### Installation

In a source checkout, add the hosting and OTLP exporter packages to the server project (choose compatible stable versions):

```powershell
dotnet add DotNetMcp\DotNetMcp.csproj package OpenTelemetry.Extensions.Hosting
dotnet add DotNetMcp\DotNetMcp.csproj package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

### Integration Code

In `DotNetMcp/Program.cs`, after creating `builder` and before building the host, explicitly opt into tracing and metrics export:

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("dotnet-mcp"))
    .WithTracing(tracing => tracing
        .AddSource("Experimental.ModelContextProtocol")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter("Experimental.ModelContextProtocol")
        .AddOtlpExporter());
```

Keep the existing server registrations and stderr console logging. Run the customized server with `dotnet run --project DotNetMcp\DotNetMcp.csproj` through an MCP client. Once the provider/exporter has been added, standard OpenTelemetry settings such as `OTEL_EXPORTER_OTLP_ENDPOINT` configure the collector connection. Choose a sampler appropriate for your deployment.

**Do not send telemetry to stdout.** The default OpenTelemetry console exporter writes there and can corrupt the stdio JSON-RPC stream. Use OTLP export to an explicitly configured collector; any console diagnostics must go to stderr.

## Performance Metrics

### Tool Execution Times

The SDK automatically tracks and logs execution duration for all tool calls:

- **Fast tools** (< 100ms): `DotnetSdkVersion`, `DotnetHelp`
- **Medium tools** (100-500ms): `DotnetTemplateList`, `DotnetPackageSearch`
- **Slow tools** (> 500ms): `DotnetProjectBuild`, `DotnetProjectTest`, `DotnetProjectPublish`

See [doc/performance-baseline.md](./performance-baseline.md) for baseline performance measurements.

### Resource Access Patterns

Resource access is logged with duration:

```text
info: ModelContextProtocol.Server.McpServer[LogRequestHandlerCompleted]
      Request handler completed: resources/read (dotnet://info) in 45ms
```

> **Note**: The SDK logs request duration automatically. Cache status indicators like "(cached)" would require custom logging in your resource implementation.

### Error Rates

Failed requests are automatically logged with:

- Error type and message
- Request duration
- Tool/resource name
- Stack trace (in debug mode)

## Token Savings Estimation

dotnet-mcp also exposes an opt-in workflow-level token savings estimator through the server metrics surface. This feature does not report exact billable token counts. Instead, it provides a transparent estimate that compares:

- MCP-side payload size using a documented approximation of `chars / 4`
- A baseline non-MCP workflow built from versioned heuristics

The initial assumptions profile is versioned as `v1` and is designed to be easy to inspect and tune. It includes workflow heuristics for:

- `create-project-package-build`
- `run-tests-summarize-failures`
- `scaffold-webapi-ef-setup`

### Confidence Levels

- `high` when measured MCP and baseline values are available
- `medium` when measured and heuristic values are mixed
- `low` when the estimate is heuristic-only

### Caveats

- Estimates are intentionally lightweight and reproducible, not billing-accurate.
- Baseline heuristics are transparent and may evolve as more workflow data is collected.
- Workflow summaries are keyed by explicit `workflowId` values to keep rollups deterministic.
- The `tokenSavings` capability flag and `dotnet_server_metrics (TokenSavingsGet)` surface are available once `TokenSavingsAccumulator` and `TokenSavingsEstimator` are registered in DI. The accumulator is populated only when callers explicitly invoke `RecordWorkflow`; no automatic recording at the tool-call boundary is performed in this release.

## Monitoring Best Practices

### 1. Enable Appropriate Log Levels

For **development**:

```bash
export Logging__LogLevel__Default=Debug
```

For **production**:

```bash
export Logging__LogLevel__Default=Information
export Logging__LogLevel__ModelContextProtocol=Warning
```

### 2. Monitor Key Metrics

Track these key performance indicators:

- **P95 latency** - 95th percentile request duration
- **Error rate** - Failed requests / total requests
- **Slow operations** - Requests > 1000ms
- **Cache hit rate** - Cached responses / total responses

### 3. Set Up Alerts

Configure alerts for:

- Error rate > 5%
- P95 latency > 2x baseline
- Any request > 10 seconds

### 4. Use Distributed Tracing

For multi-service deployments, use OpenTelemetry OTLP exporters to send traces to:

- **Jaeger** - Open-source distributed tracing
- **Zipkin** - Distributed tracing system
- **Azure Monitor** - Cloud-native observability
- **Grafana Cloud** - Managed observability platform

## Telemetry Data Privacy

The shipped server keeps telemetry local by default:

- **Redaction** - dotnet-mcp redacts sensitive values in its own error/reporting paths; this is not a blanket guarantee for all SDK instrumentation
- **Opt-in export** - OpenTelemetry export requires explicit source customization
- **Local by default** - Logs are written to stderr, not sent externally

SDK span attributes can include tool names, resource URIs, and session/request identifiers; error span descriptions can include tool error content or exception messages. Trace state is also forwarded from the caller. Review instrumentation, log levels, collector access, redaction, and retention before enabling export, especially when arguments, paths, or tool output may be sensitive.

## Troubleshooting

### Enable Debug Logging

```bash
# Enable all debug logs
export Logging__LogLevel__Default=Debug

# Run the server
dotnet-mcp
```

### View Request Durations

All request durations are logged automatically at `Information` level:

```bash
# Filter for request completion logs
dotnet-mcp 2>&1 | grep "Request handler completed"
```

### Analyze Performance Issues

1. Enable trace logging: `export Logging__LogLevel__DotNetMcp=Trace`
2. Look for slow operations in logs (duration > 1000ms)
3. Check for repeated slow operations (cache misses)
4. Compare against baseline metrics in `doc/performance-baseline.md`

## References

- [MCP C# SDK v2.3.0 Release Notes](https://github.com/modelcontextprotocol/csharp-sdk/releases/tag/v2.3.0)
- [SDK Trace Context Implementation](https://github.com/modelcontextprotocol/csharp-sdk/blob/v2.3.0/src/ModelContextProtocol.Core/Diagnostics.cs)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)
- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/languages/net/)
- [Performance Baseline Measurements](./performance-baseline.md)

## Future Enhancements

Planned telemetry improvements:

- Additional operation-level instrumentation where SDK request spans are insufficient
- Metrics for cache hit/miss rates
- Performance budgets with automated regression detection
- Integration with Application Insights
- Grafana dashboard templates
