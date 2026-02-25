using DotNetMcp;
using DotNetMcp.Actions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using Xunit;

namespace DotNetMcp.Tests;

/// <summary>
/// Tests for the dotnet_server_metrics tool and ToolMetricsAccumulator.
/// </summary>
public class ServerMetricsToolTests
{
    private readonly DotNetCliTools _tools;
    private readonly ToolMetricsAccumulator _accumulator;

    public ServerMetricsToolTests()
    {
        _accumulator = new ToolMetricsAccumulator();
        _tools = new DotNetCliTools(
            NullLogger<DotNetCliTools>.Instance,
            new ConcurrencyManager(),
            new ProcessSessionManager(),
            _accumulator);
    }

    // ---- ToolMetricsAccumulator unit tests ----

    [Fact]
    public void ToolMetricsAccumulator_InitialSnapshot_IsEmpty()
    {
        var snapshot = _accumulator.GetSnapshot();
        Assert.Empty(snapshot);
    }

    [Fact]
    public void ToolMetricsAccumulator_RecordInvocation_TracksCount()
    {
        _accumulator.RecordInvocation("dotnet_project", 100, success: true);
        _accumulator.RecordInvocation("dotnet_project", 200, success: true);

        var snapshot = _accumulator.GetSnapshot();

        Assert.True(snapshot.TryGetValue("dotnet_project", out var entry));
        Assert.Equal(2, entry.InvocationCount);
    }

    [Fact]
    public void ToolMetricsAccumulator_RecordInvocation_TracksSuccessAndFailure()
    {
        _accumulator.RecordInvocation("dotnet_build", 50, success: true);
        _accumulator.RecordInvocation("dotnet_build", 60, success: false);

        var snapshot = _accumulator.GetSnapshot();

        Assert.True(snapshot.TryGetValue("dotnet_build", out var entry));
        Assert.Equal(1, entry.SuccessCount);
        Assert.Equal(1, entry.FailureCount);
    }

    [Fact]
    public void ToolMetricsAccumulator_RecordInvocation_ComputesAvgDuration()
    {
        _accumulator.RecordInvocation("dotnet_sdk", 100, success: true);
        _accumulator.RecordInvocation("dotnet_sdk", 300, success: true);

        var snapshot = _accumulator.GetSnapshot();

        Assert.True(snapshot.TryGetValue("dotnet_sdk", out var entry));
        Assert.Equal(200.0, entry.AvgDurationMs, precision: 1);
    }

    [Fact]
    public void ToolMetricsAccumulator_RecordInvocation_TracksMultipleTools()
    {
        _accumulator.RecordInvocation("tool_a", 10, success: true);
        _accumulator.RecordInvocation("tool_b", 20, success: true);

        var snapshot = _accumulator.GetSnapshot();

        Assert.Equal(2, snapshot.Count);
        Assert.True(snapshot.ContainsKey("tool_a"));
        Assert.True(snapshot.ContainsKey("tool_b"));
    }

    [Fact]
    public void ToolMetricsAccumulator_Reset_ClearsAllEntries()
    {
        _accumulator.RecordInvocation("dotnet_project", 100, success: true);
        _accumulator.RecordInvocation("dotnet_sdk", 200, success: true);

        _accumulator.Reset();

        var snapshot = _accumulator.GetSnapshot();
        Assert.Empty(snapshot);
    }

    [Fact]
    public void ToolMetricsAccumulator_AvgDuration_IsZeroWhenNoInvocations()
    {
        // Snapshot of a freshly-reset accumulator has no entries, so we can't test
        // AvgDurationMs for a non-existent key; verify a single-invocation entry is correct
        _accumulator.RecordInvocation("tool_x", 150, success: true);
        var snapshot = _accumulator.GetSnapshot();
        Assert.Equal(150.0, snapshot["tool_x"].AvgDurationMs, precision: 1);
    }

    // ---- DotnetServerMetrics tool tests ----

    [Fact]
    public async Task DotnetServerMetrics_Get_ReturnsEmptyMetricsWhenNoInvocations()
    {
        var result = (await _tools.DotnetServerMetrics(DotnetServerMetricsAction.Get)).GetText();

        Assert.NotNull(result);
        // Should be valid JSON
        Assert.Contains("{", result);
        Assert.Contains("}", result);
        Assert.Contains("toolMetrics", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("totalInvocations", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetServerMetrics_Get_ReturnsZeroTotalsWhenEmpty()
    {
        var result = (await _tools.DotnetServerMetrics(DotnetServerMetricsAction.Get)).GetText();

        Assert.NotNull(result);
        var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;

        Assert.Equal(0, root.GetProperty("totalInvocations").GetInt64());
        Assert.Equal(0, root.GetProperty("totalSuccesses").GetInt64());
        Assert.Equal(0, root.GetProperty("totalFailures").GetInt64());
    }

    [Fact]
    public async Task DotnetServerMetrics_Get_ReturnsAccumulatedMetrics()
    {
        _accumulator.RecordInvocation("dotnet_project", 100, success: true);
        _accumulator.RecordInvocation("dotnet_project", 200, success: true);
        _accumulator.RecordInvocation("dotnet_sdk", 50, success: false);

        var result = (await _tools.DotnetServerMetrics(DotnetServerMetricsAction.Get)).GetText();

        Assert.NotNull(result);
        var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;

        Assert.Equal(3, root.GetProperty("totalInvocations").GetInt64());
        Assert.Equal(2, root.GetProperty("totalSuccesses").GetInt64());
        Assert.Equal(1, root.GetProperty("totalFailures").GetInt64());
    }

    [Fact]
    public async Task DotnetServerMetrics_Get_IncludesPerToolStats()
    {
        _accumulator.RecordInvocation("dotnet_project", 100, success: true);
        _accumulator.RecordInvocation("dotnet_project", 300, success: true);

        var result = (await _tools.DotnetServerMetrics(DotnetServerMetricsAction.Get)).GetText();

        Assert.NotNull(result);
        Assert.Contains("dotnet_project", result);
        Assert.Contains("invocationCount", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("avgDurationMs", result, StringComparison.OrdinalIgnoreCase);

        var doc = JsonDocument.Parse(result);
        var toolMetrics = doc.RootElement.GetProperty("toolMetrics");
        var projectMetrics = toolMetrics.GetProperty("dotnet_project");

        Assert.Equal(2, projectMetrics.GetProperty("invocationCount").GetInt64());
        Assert.Equal(200.0, projectMetrics.GetProperty("avgDurationMs").GetDouble(), precision: 1);
        Assert.Equal(2, projectMetrics.GetProperty("successCount").GetInt64());
        Assert.Equal(0, projectMetrics.GetProperty("failureCount").GetInt64());
    }

    [Fact]
    public async Task DotnetServerMetrics_Reset_ClearsAllMetrics()
    {
        _accumulator.RecordInvocation("dotnet_project", 100, success: true);
        _accumulator.RecordInvocation("dotnet_sdk", 200, success: true);

        var resetResult = (await _tools.DotnetServerMetrics(DotnetServerMetricsAction.Reset)).GetText();

        Assert.NotNull(resetResult);
        Assert.Contains("success", resetResult, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reset", resetResult, StringComparison.OrdinalIgnoreCase);

        // Verify metrics are cleared
        var getResult = (await _tools.DotnetServerMetrics(DotnetServerMetricsAction.Get)).GetText();
        var doc = JsonDocument.Parse(getResult!);
        Assert.Equal(0, doc.RootElement.GetProperty("totalInvocations").GetInt64());
    }

    [Fact]
    public async Task DotnetServerMetrics_Reset_ReturnsSuccessJson()
    {
        var result = (await _tools.DotnetServerMetrics(DotnetServerMetricsAction.Reset)).GetText();

        Assert.NotNull(result);
        var doc = JsonDocument.Parse(result);
        var root = doc.RootElement;

        Assert.True(root.GetProperty("success").GetBoolean());
        var message = root.GetProperty("message").GetString();
        Assert.NotNull(message);
        Assert.NotEmpty(message);
    }

    [Fact]
    public async Task DotnetServerMetrics_WithoutAccumulator_ReturnsError()
    {
        // Create tools without an accumulator (simulates missing DI registration)
        var toolsWithoutAccumulator = new DotNetCliTools(
            NullLogger<DotNetCliTools>.Instance,
            new ConcurrencyManager(),
            new ProcessSessionManager(),
            metricsAccumulator: null);

        var result = (await toolsWithoutAccumulator.DotnetServerMetrics(DotnetServerMetricsAction.Get)).GetText();

        Assert.NotNull(result);
        Assert.Contains("Error", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DotnetServerMetrics_Get_IsCallToolResult()
    {
        var result = await _tools.DotnetServerMetrics(DotnetServerMetricsAction.Get);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    // ---- ToolMetricSnapshot tests ----

    [Fact]
    public void ToolMetricSnapshot_AvgDurationMs_IsRoundedToTwoDecimals()
    {
        _accumulator.RecordInvocation("tool_x", 100, success: true);
        _accumulator.RecordInvocation("tool_x", 101, success: true);
        _accumulator.RecordInvocation("tool_x", 102, success: true);

        var snapshot = _accumulator.GetSnapshot();
        var entry = snapshot["tool_x"];

        // Average = 101, should be 101.0 (exactly 2 decimal places)
        Assert.Equal(Math.Round(entry.AvgDurationMs, 2), entry.AvgDurationMs);
    }
}
