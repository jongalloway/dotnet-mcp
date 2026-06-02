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
    [McpServerTool(Title = "Server Metrics", ReadOnly = false, Idempotent = false, UseStructuredContent = true, IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/62ecdc0d7ca5c6df32148c169556bc8d3782fca4/assets/Bar%20Chart/Flat/bar_chart_flat.svg")]
    [McpMeta("category", "telemetry")]
    [McpMeta("priority", 5.0)]
    [McpMeta("consolidatedTool", true)]
    [McpMeta("actions", JsonValue = """["Get","TokenSavingsGet","TokenSavingsReset","Reset"]""")]
    [McpMeta("ui", JsonValue = """{"resourceUri": "ui://dotnet-mcp/server-metrics"}""")]
    [McpMeta("ui/resourceUri", "ui://dotnet-mcp/server-metrics")]
    public partial Task<CallToolResult> DotnetServerMetrics(DotnetServerMetricsAction action)
    {
        if ((_metricsAccumulator is null) && action is DotnetServerMetricsAction.Get or DotnetServerMetricsAction.Reset)
        {
            var error = ErrorResultFactory.ReturnCapabilityNotAvailable(
                "server metrics",
                "Metrics accumulator is not registered. Ensure ToolMetricsAccumulator is registered in the DI container.",
                alternatives: null);
            return Task.FromResult(StructuredContentHelper.ToCallToolResult(ErrorResultFactory.ToJson(error)));
        }

        if ((_tokenSavingsAccumulator is null || _tokenSavingsEstimator is null) && action is DotnetServerMetricsAction.TokenSavingsGet or DotnetServerMetricsAction.TokenSavingsReset)
        {
            var error = ErrorResultFactory.ReturnCapabilityNotAvailable(
                "token savings metrics",
                "Token savings services are not registered. Ensure TokenSavingsAccumulator and TokenSavingsEstimator are registered in the DI container.",
                alternatives: null);
            return Task.FromResult(StructuredContentHelper.ToCallToolResult(ErrorResultFactory.ToJson(error)));
        }

        switch (action)
        {
            case DotnetServerMetricsAction.Reset:
                _metricsAccumulator!.Reset();
                var resetResponse = new ServerMetricsResetResponse
                {
                    Success = true,
                    Message = "Server metrics have been reset."
                };
                return Task.FromResult(StructuredContentHelper.ToCallToolResult(ErrorResultFactory.ToJson(resetResponse), resetResponse));

            case DotnetServerMetricsAction.TokenSavingsReset:
                _tokenSavingsAccumulator!.Reset();
                var tokenResetResponse = new ServerMetricsTokenSavingsResponse
                {
                    Success = true,
                    Message = "Token savings estimates have been reset."
                };
                return Task.FromResult(StructuredContentHelper.ToCallToolResult(ErrorResultFactory.ToJson(tokenResetResponse), tokenResetResponse));

            case DotnetServerMetricsAction.TokenSavingsGet:
                var tokenSnapshot = _tokenSavingsAccumulator!.GetSnapshot();
                var tokenResponse = new ServerMetricsTokenSavingsResponse
                {
                    Success = true,
                    Message = "Token savings estimates retrieved.",
                    WorkflowTokenSavings = tokenSnapshot
                        .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                        .Select(kv => kv.Value)
                        .ToArray(),
                    TotalWorkflowSavingsTokens = tokenSnapshot.Values.Sum(item => item.EstimatedSavingsTokens),
                    TotalWorkflowMcpTokens = tokenSnapshot.Values.Sum(item => item.McpEstimatedTokens),
                    TotalWorkflowBaselineTokens = tokenSnapshot.Values.Sum(item => item.BaselineEstimatedTokens),
                    AssumptionsVersion = tokenSnapshot.Values.Select(item => item.AssumptionsProfile.AssumptionsVersion).FirstOrDefault() ?? "v1"
                };
                return Task.FromResult(StructuredContentHelper.ToCallToolResult(ErrorResultFactory.ToJson(tokenResponse), tokenResponse));

            case DotnetServerMetricsAction.Get:
            default:
                var snapshot = _metricsAccumulator!.GetSnapshot();
                var metricsResponse = new ServerMetricsResponse
                {
                    ToolMetrics = snapshot
                        .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                        .ToDictionary(kv => kv.Key, kv => kv.Value),
                    TotalInvocations = snapshot.Values.Sum(m => m.InvocationCount),
                    TotalSuccesses = snapshot.Values.Sum(m => m.SuccessCount),
                    TotalFailures = snapshot.Values.Sum(m => m.FailureCount),
                    TokenSavingsEnabled = _tokenSavingsAccumulator is not null && _tokenSavingsEstimator is not null
                };
                return Task.FromResult(StructuredContentHelper.ToCallToolResult(ErrorResultFactory.ToJson(metricsResponse), metricsResponse));
        }
    }
}

/// <summary>
/// JSON response for the dotnet_server_metrics Reset action.
/// </summary>
public sealed class ServerMetricsResetResponse
{
    /// <summary>Indicates whether the reset was successful.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>Human-readable confirmation message.</summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
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

    /// <summary>Whether token savings reporting is available on this server instance.</summary>
    [JsonPropertyName("tokenSavingsEnabled")]
    public bool TokenSavingsEnabled { get; init; }
}

/// <summary>
/// JSON response for token savings metric operations.
/// </summary>
public sealed class ServerMetricsTokenSavingsResponse
{
    /// <summary>Indicates whether the operation was successful.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>Human-readable confirmation or summary message.</summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    /// <summary>Workflow estimates currently stored in the accumulator.</summary>
    [JsonPropertyName("workflowTokenSavings")]
    public TokenSavingsWorkflowEstimate[] WorkflowTokenSavings { get; init; } = [];

    /// <summary>Total estimated workflow token savings across the snapshot.</summary>
    [JsonPropertyName("totalWorkflowSavingsTokens")]
    public long TotalWorkflowSavingsTokens { get; init; }

    /// <summary>Total estimated MCP-side workflow tokens across the snapshot.</summary>
    [JsonPropertyName("totalWorkflowMcpTokens")]
    public long TotalWorkflowMcpTokens { get; init; }

    /// <summary>Total estimated baseline workflow tokens across the snapshot.</summary>
    [JsonPropertyName("totalWorkflowBaselineTokens")]
    public long TotalWorkflowBaselineTokens { get; init; }

    /// <summary>Assumptions profile version used by the snapshot.</summary>
    [JsonPropertyName("assumptionsVersion")]
    public string AssumptionsVersion { get; init; } = "v1";
}
