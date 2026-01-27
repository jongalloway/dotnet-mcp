# Telemetry and Observability

This document describes the telemetry and observability features available in dotnet-mcp, leveraging the MCP C# SDK v0.6.0-preview.1.

## Overview

dotnet-mcp provides comprehensive telemetry and observability through:

1. **Built-in SDK Telemetry** - Request duration logging and OpenTelemetry semantic conventions (SDK v0.6+)
2. **Structured Logging** - Microsoft.Extensions.Logging with configurable log levels
3. **OpenTelemetry Integration** - Optional instrumentation for tools, resources, and operations

## Built-in SDK Telemetry (v0.6+)

The MCP C# SDK v0.6.0-preview.1 automatically provides telemetry aligned with OpenTelemetry semantic conventions.

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

### Log Examples

```
info: ModelContextProtocol.Server.McpServer[LogRequestHandlerCompleted]
      Request handler completed: tools/call (DotnetSdkVersion) in 125ms
      
info: ModelContextProtocol.Server.McpServer[LogRequestHandlerException]
      Request handler failed: tools/call (DotnetProjectBuild) in 3450ms
      Exception: System.InvalidOperationException: Build failed with exit code 1
```

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

### Log Categories

- **`ModelContextProtocol.*`** - SDK-level logs (request handling, transport, serialization)
- **`DotNetMcp.*`** - Server-level logs (tool execution, resource access, errors)
- **`Microsoft.Hosting.*`** - Hosting infrastructure logs

## OpenTelemetry Integration (Optional)

For production deployments, you can integrate OpenTelemetry for distributed tracing, metrics, and advanced observability.

### Installation

Add OpenTelemetry packages to your deployment environment (latest stable versions recommended):

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Exporter.Console
dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
```

### Configuration Example

Create an `appsettings.OpenTelemetry.json` configuration file:

```json
{
  "OpenTelemetry": {
    "ServiceName": "dotnet-mcp",
    "ServiceVersion": "1.0.0",
    "Traces": {
      "Enabled": true,
      "ConsoleExporter": false,
      "OtlpExporter": {
        "Enabled": true,
        "Endpoint": "http://localhost:4317"
      }
    },
    "Metrics": {
      "Enabled": true,
      "ConsoleExporter": false,
      "OtlpExporter": {
        "Enabled": true,
        "Endpoint": "http://localhost:4317"
      }
    }
  }
}
```

### Integration Code

Modify `Program.cs` to add OpenTelemetry:

```csharp
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = Host.CreateApplicationBuilder(args);

// Add OpenTelemetry configuration
var openTelemetryConfig = builder.Configuration.GetSection("OpenTelemetry");
var serviceName = openTelemetryConfig.GetValue<string>("ServiceName") ?? "dotnet-mcp";
var serviceVersion = openTelemetryConfig.GetValue<string>("ServiceVersion") ?? "1.0.0";

// Configure OpenTelemetry tracing
var tracesEnabled = openTelemetryConfig.GetSection("Traces").GetValue<bool>("Enabled");
if (tracesEnabled)
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService(serviceName, serviceVersion: serviceVersion))
        .WithTracing(tracing =>
        {
            tracing
                .AddSource("DotNetMcp.*")
                .AddSource("ModelContextProtocol.*");
            
            var consoleExporter = openTelemetryConfig.GetValue<bool>("Traces:ConsoleExporter");
            if (consoleExporter)
                tracing.AddConsoleExporter();
            
            var otlpEnabled = openTelemetryConfig.GetValue<bool>("Traces:OtlpExporter:Enabled");
            if (otlpEnabled)
            {
                var endpoint = openTelemetryConfig.GetValue<string>("Traces:OtlpExporter:Endpoint");
                tracing.AddOtlpExporter(options =>
                {
                    if (!string.IsNullOrEmpty(endpoint))
                        options.Endpoint = new Uri(endpoint);
                });
            }
        });
}

// Configure OpenTelemetry metrics
var metricsEnabled = openTelemetryConfig.GetSection("Metrics").GetValue<bool>("Enabled");
if (metricsEnabled)
{
    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
            metrics
                .AddMeter("DotNetMcp.*")
                .AddMeter("ModelContextProtocol.*");
            
            var consoleExporter = openTelemetryConfig.GetValue<bool>("Metrics:ConsoleExporter");
            if (consoleExporter)
                metrics.AddConsoleExporter();
            
            var otlpEnabled = openTelemetryConfig.GetValue<bool>("Metrics:OtlpExporter:Enabled");
            if (otlpEnabled)
            {
                var endpoint = openTelemetryConfig.GetValue<string>("Metrics:OtlpExporter:Endpoint");
                metrics.AddOtlpExporter(options =>
                {
                    if (!string.IsNullOrEmpty(endpoint))
                        options.Endpoint = new Uri(endpoint);
                });
            }
        });
}

// Continue with standard MCP server configuration...
builder.Services.AddMcpServer(options => { /* ... */ });
```

## Performance Metrics

### Tool Execution Times

The SDK automatically tracks and logs execution duration for all tool calls:

- **Fast tools** (< 100ms): `DotnetSdkVersion`, `DotnetHelp`
- **Medium tools** (100-500ms): `DotnetTemplateList`, `DotnetPackageSearch`
- **Slow tools** (> 500ms): `DotnetProjectBuild`, `DotnetProjectTest`, `DotnetProjectPublish`

See [doc/performance-baseline.md](./performance-baseline.md) for baseline performance measurements.

### Resource Access Patterns

Resource access is logged with duration and caching information:

```
info: ModelContextProtocol.Server.McpServer[LogRequestHandlerCompleted]
      Request handler completed: resources/read (dotnet://info) in 45ms (cached)
```

### Error Rates

Failed requests are automatically logged with:
- Error type and message
- Request duration
- Tool/resource name
- Stack trace (in debug mode)

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

dotnet-mcp telemetry respects user privacy:

- **No sensitive data** - Command output containing secrets or credentials is never logged
- **Sanitized parameters** - Sensitive parameters are redacted in logs
- **Opt-in only** - OpenTelemetry integration requires explicit configuration
- **Local by default** - Logs are written to stderr, not sent externally

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

- [MCP C# SDK v0.6 Release Notes](https://github.com/modelcontextprotocol/csharp-sdk/releases/tag/v0.6.0-preview.1)
- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/languages/net/)
- [Performance Baseline Measurements](./performance-baseline.md)

## Future Enhancements

Planned telemetry improvements:

- Custom ActivitySource for tool execution spans
- Metrics for cache hit/miss rates
- Performance budgets with automated regression detection
- Integration with Application Insights
- Grafana dashboard templates
