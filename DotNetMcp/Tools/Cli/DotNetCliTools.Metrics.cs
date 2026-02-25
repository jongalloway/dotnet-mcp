using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DotNetMcp;

/// <summary>
/// Tool for retrieving and resetting in-memory MCP server metrics.
/// Metrics are collected automatically by the telemetry filter wired into the MCP request pipeline.
/// </summary>
public sealed partial class DotNetCliTools
{
    /// <summary>
    /// Get or reset in-memory telemetry metrics for MCP tool invocations.
    /// Returns per-tool counts, average durations, and success/failure rates.
    /// Metrics are collected automatically via a message filter — no code changes needed in individual tools.
    /// No PII is stored; only tool names and timing data are tracked.
    /// </summary>
    /// <param name="action">The metrics operation to perform: Get (return current snapshot) or Reset (clear all counters)</param>
    [McpServerTool(Title = "Server Metrics", ReadOnly = false, Idempotent = false, IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Bar%20Chart/Flat/bar_chart_flat.svg")]
    [McpMeta("category", "telemetry")]
    [McpMeta("priority", 5.0)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["Get","Reset"]""")]
    public partial Task<CallToolResult> DotnetServerMetrics(DotnetServerMetricsAction action)
    {
        if (_metricsAccumulator is null)
        {
            var error = ErrorResultFactory.ReturnCapabilityNotAvailable(
                "server metrics",
                "Metrics accumulator is not registered. Ensure ToolMetricsAccumulator is registered in the DI container.",
                alternatives: null);
            return Task.FromResult(StructuredContentHelper.ToCallToolResult(ErrorResultFactory.ToJson(error)));
        }

        switch (action)
        {
            case DotnetServerMetricsAction.Reset:
                _metricsAccumulator.Reset();
                var resetResponse = new MetricsResetResponse { Success = true, Message = "All metrics have been reset." };
                return Task.FromResult(StructuredContentHelper.ToCallToolResult(ErrorResultFactory.ToJson(resetResponse)));

            case DotnetServerMetricsAction.Get:
            default:
                var snapshot = _metricsAccumulator.GetSnapshot();
                var metricsResponse = new ServerMetricsResponse
                {
                    ToolMetrics = snapshot
                        .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                        .ToDictionary(kv => kv.Key, kv => kv.Value),
                    TotalInvocations = snapshot.Values.Sum(m => m.InvocationCount),
                    TotalSuccesses = snapshot.Values.Sum(m => m.SuccessCount),
                    TotalFailures = snapshot.Values.Sum(m => m.FailureCount)
                };
                var json = ErrorResultFactory.ToJson(metricsResponse);
                return Task.FromResult(StructuredContentHelper.ToCallToolResult(json, metricsResponse));
        }
    }
}

/// <summary>
/// JSON response for the dotnet_server_metrics Get action.
/// </summary>
public sealed class ServerMetricsResponse
{
    /// <summary>Per-tool invocation metrics, keyed by tool name.</summary>
    [JsonPropertyName("toolMetrics")]
    public Dictionary<string, ToolMetricSnapshot> ToolMetrics { get; init; } = new();

    /// <summary>Total invocations across all tools since last reset.</summary>
    [JsonPropertyName("totalInvocations")]
    public long TotalInvocations { get; init; }

    /// <summary>Total successful invocations across all tools since last reset.</summary>
    [JsonPropertyName("totalSuccesses")]
    public long TotalSuccesses { get; init; }

    /// <summary>Total failed invocations across all tools since last reset.</summary>
    [JsonPropertyName("totalFailures")]
    public long TotalFailures { get; init; }
}

/// <summary>
/// JSON response for the dotnet_server_metrics Reset action.
/// </summary>
public sealed class MetricsResetResponse
{
    /// <summary>Whether the reset succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>Human-readable confirmation message.</summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}
