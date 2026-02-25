using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace DotNetMcp;

/// <summary>
/// Thread-safe in-memory accumulator for MCP tool invocation metrics.
/// Tracks per-tool call counts, total durations, and success/failure counts.
/// No personally identifiable information (PII) is stored — only tool names and timing data.
/// </summary>
public sealed class ToolMetricsAccumulator
{
    private readonly ConcurrentDictionary<string, ToolMetricEntry> _entries = new(StringComparer.Ordinal);

    /// <summary>
    /// Records a single tool invocation with its duration and outcome.
    /// </summary>
    /// <param name="toolName">The name of the invoked tool (e.g., "dotnet_project")</param>
    /// <param name="durationMs">Elapsed wall-clock time of the invocation in milliseconds</param>
    /// <param name="success">Whether the invocation completed without throwing an exception</param>
    public void RecordInvocation(string toolName, long durationMs, bool success)
    {
        var entry = _entries.GetOrAdd(toolName, _ => new ToolMetricEntry());
        entry.RecordInvocation(durationMs, success);
    }

    /// <summary>
    /// Returns a read-only snapshot of all collected metrics, keyed by tool name.
    /// </summary>
    public IReadOnlyDictionary<string, ToolMetricSnapshot> GetSnapshot()
    {
        var result = new Dictionary<string, ToolMetricSnapshot>(StringComparer.Ordinal);
        foreach (var (name, entry) in _entries)
        {
            result[name] = entry.GetSnapshot();
        }
        return result;
    }

    /// <summary>
    /// Resets all accumulated metrics to zero.
    /// </summary>
    public void Reset() => _entries.Clear();
}

/// <summary>
/// Thread-safe mutable entry for a single tool's accumulated metrics.
/// </summary>
internal sealed class ToolMetricEntry
{
    private long _invocationCount;
    private long _totalDurationMs;
    private long _successCount;
    private long _failureCount;

    public void RecordInvocation(long durationMs, bool success)
    {
        Interlocked.Increment(ref _invocationCount);
        Interlocked.Add(ref _totalDurationMs, durationMs);
        if (success)
            Interlocked.Increment(ref _successCount);
        else
            Interlocked.Increment(ref _failureCount);
    }

    public ToolMetricSnapshot GetSnapshot()
    {
        long count = Interlocked.Read(ref _invocationCount);
        long total = Interlocked.Read(ref _totalDurationMs);
        long success = Interlocked.Read(ref _successCount);
        long failure = Interlocked.Read(ref _failureCount);
        double avgDuration = count > 0 ? (double)total / count : 0.0;
        return new ToolMetricSnapshot(count, avgDuration, success, failure);
    }
}

/// <summary>
/// An immutable snapshot of metrics for a single tool.
/// </summary>
public sealed class ToolMetricSnapshot
{
    /// <summary>Total number of invocations recorded since last reset.</summary>
    [JsonPropertyName("invocationCount")]
    public long InvocationCount { get; }

    /// <summary>Average duration in milliseconds across all recorded invocations.</summary>
    [JsonPropertyName("avgDurationMs")]
    public double AvgDurationMs { get; }

    /// <summary>Number of invocations that completed without an exception.</summary>
    [JsonPropertyName("successCount")]
    public long SuccessCount { get; }

    /// <summary>Number of invocations that threw an exception.</summary>
    [JsonPropertyName("failureCount")]
    public long FailureCount { get; }

    /// <summary>
    /// Initializes a new snapshot with the given values.
    /// </summary>
    public ToolMetricSnapshot(long invocationCount, double avgDurationMs, long successCount, long failureCount)
    {
        InvocationCount = invocationCount;
        AvgDurationMs = Math.Round(avgDurationMs, 2);
        SuccessCount = successCount;
        FailureCount = failureCount;
    }
}
