using System.Diagnostics;
using DotNetMcp;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetMcp.Tests.Performance;

/// <summary>
/// Performance smoke tests for dotnet-mcp v1.0.
/// 
/// These tests measure end-to-end overhead for representative tool invocations
/// to catch obvious performance regressions. They are intentionally lightweight
/// and report-only (non-blocking for CI).
/// 
/// Design principles:
/// - Use real tool invocations (not mocks) for realistic measurements
/// - Include warmup iterations to stabilize JIT compilation and caching
/// - Run multiple iterations for statistical validity
/// - Report basic statistics (mean, min, max, median, p95, p99, stddev)
/// - Non-blocking: tests report results but don't fail builds
/// 
/// Future work tracked in: https://github.com/jongalloway/dotnet-mcp/issues/151
/// - BenchmarkDotNet integration for micro-benchmarks
/// - Performance budgets and regression gates
/// - More comprehensive tool coverage
/// </summary>
public class PerformanceSmokeTests
{
    // Configuration constants
    private const int WarmupIterations = 3;
    private const int MeasurementIterations = 10;
    
    // Performance thresholds (informational only - not enforced)
    // These are rough baselines and may vary by hardware/CI environment
    private const double ExpectedMeanMs = 500.0;  // Expected mean execution time
    private const double ExpectedP95Ms = 1000.0;  // Expected 95th percentile
    
    private readonly DotNetCliTools _tools;
    
    public PerformanceSmokeTests()
    {
        var concurrencyManager = new ConcurrencyManager();
        _tools = new DotNetCliTools(NullLogger<DotNetCliTools>.Instance, concurrencyManager);
    }
    
    /// <summary>
    /// Performance smoke test for DotnetTemplateList - a representative tool that:
    /// - Uses caching (good for testing cache performance)
    /// - Calls the SDK (realistic workload)
    /// - Returns structured data (typical response size)
    /// </summary>
    [Fact]
    public async Task DotnetTemplateList_PerformanceSmoke()
    {
        // Warmup: stabilize JIT compilation, caching, and process state
        for (int i = 0; i < WarmupIterations; i++)
        {
            await _tools.DotnetTemplateList(forceReload: false);
        }
        
        // Clear cache before measurements to ensure consistent starting state
        await _tools.DotnetTemplateClearCache();
        
        // Measurement iterations
        var measurements = new List<double>(MeasurementIterations);
        var sw = new Stopwatch();
        
        for (int i = 0; i < MeasurementIterations; i++)
        {
            sw.Restart();
            var result = await _tools.DotnetTemplateList(forceReload: false);
            sw.Stop();
            
            // Verify the call succeeded
            Assert.NotNull(result);
            Assert.DoesNotContain("Error:", result);
            
            measurements.Add(sw.Elapsed.TotalMilliseconds);
        }
        
        // Calculate statistics
        var stats = CalculateStatistics(measurements);
        
        // Report results (informational - not enforced)
        var report = FormatPerformanceReport("DotnetTemplateList", stats, ExpectedMeanMs, ExpectedP95Ms);
        
        // Write to test output for visibility in test results
        // In xUnit v3, we use the standard output which is captured in test results
        Console.WriteLine(report);
        
        // Note: We intentionally do NOT assert on performance thresholds
        // This test is informational only and should not block CI
        // Future work (issue #151) will add proper performance gates
    }
    
    /// <summary>
    /// Performance smoke test for DotnetSdkVersion - a fast, simple tool for baseline overhead measurement
    /// </summary>
    [Fact]
    public async Task DotnetSdkVersion_PerformanceSmoke()
    {
        // Warmup
        for (int i = 0; i < WarmupIterations; i++)
        {
            await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.Version);
        }
        
        // Measurement iterations
        var measurements = new List<double>(MeasurementIterations);
        var sw = new Stopwatch();
        
        for (int i = 0; i < MeasurementIterations; i++)
        {
            sw.Restart();
            var result = await _tools.DotnetSdk(action: DotNetMcp.Actions.DotnetSdkAction.Version);
            sw.Stop();
            
            // Verify the call succeeded
            Assert.NotNull(result);
            Assert.DoesNotContain("Error:", result);
            
            measurements.Add(sw.Elapsed.TotalMilliseconds);
        }
        
        // Calculate statistics
        var stats = CalculateStatistics(measurements);
        
        // Report results (expected to be much faster than template operations)
        var report = FormatPerformanceReport("DotnetSdkVersion", stats, 100.0, 200.0);
        Console.WriteLine(report);
    }
    
    /// <summary>
    /// Calculate basic statistics from a set of measurements
    /// </summary>
    private static PerformanceStatistics CalculateStatistics(List<double> measurements)
    {
        measurements.Sort();
        
        var min = measurements[0];
        var max = measurements[^1];
        var mean = measurements.Average();
        
        // Correct median calculation: average the two middle elements for even-sized arrays
        var median = measurements.Count % 2 == 0
            ? (measurements[measurements.Count / 2 - 1] + measurements[measurements.Count / 2]) / 2.0
            : measurements[measurements.Count / 2];
        
        // Calculate percentiles using linear interpolation method (more accurate for small samples)
        // This matches the method used by Excel, R, and most statistical packages
        var p95 = CalculatePercentile(measurements, 0.95);
        var p99 = CalculatePercentile(measurements, 0.99);
        
        // Calculate standard deviation
        var variance = measurements.Select(m => Math.Pow(m - mean, 2)).Average();
        var stdDev = Math.Sqrt(variance);
        
        return new PerformanceStatistics
        {
            Count = measurements.Count,
            Min = min,
            Max = max,
            Mean = mean,
            Median = median,
            P95 = p95,
            P99 = p99,
            StdDev = stdDev
        };
    }
    
    /// <summary>
    /// Calculate a percentile using linear interpolation (Excel PERCENTILE.INC method)
    /// This provides more accurate results than nearest-rank for small samples
    /// </summary>
    private static double CalculatePercentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
            throw new ArgumentException("Cannot calculate percentile of empty list", nameof(sortedValues));
        
        if (sortedValues.Count == 1)
            return sortedValues[0];
        
        // Position in the sorted array (1-based)
        var position = percentile * (sortedValues.Count - 1);
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);
        
        // If position is exactly on an index, return that value
        if (lowerIndex == upperIndex)
            return sortedValues[lowerIndex];
        
        // Otherwise, interpolate between the two adjacent values
        var lowerValue = sortedValues[lowerIndex];
        var upperValue = sortedValues[upperIndex];
        var fraction = position - lowerIndex;
        
        return lowerValue + (upperValue - lowerValue) * fraction;
    }
    
    /// <summary>
    /// Format a human-readable performance report
    /// </summary>
    private static string FormatPerformanceReport(string testName, PerformanceStatistics stats, double expectedMean, double expectedP95)
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine();
        report.AppendLine($"═══════════════════════════════════════════════════════════════");
        report.AppendLine($"Performance Smoke Test: {testName}");
        report.AppendLine($"═══════════════════════════════════════════════════════════════");
        report.AppendLine($"Iterations:     {stats.Count}");
        report.AppendLine($"Mean:           {stats.Mean:F2} ms (expected: ~{expectedMean:F0} ms)");
        report.AppendLine($"Median:         {stats.Median:F2} ms");
        report.AppendLine($"Std Dev:        {stats.StdDev:F2} ms");
        report.AppendLine($"Min:            {stats.Min:F2} ms");
        report.AppendLine($"Max:            {stats.Max:F2} ms");
        report.AppendLine($"P95:            {stats.P95:F2} ms (expected: <{expectedP95:F0} ms)");
        report.AppendLine($"P99:            {stats.P99:F2} ms");
        report.AppendLine($"───────────────────────────────────────────────────────────────");
        
        // Add informational notes about expected vs actual
        if (stats.Mean > expectedMean * 2.0)
        {
            report.AppendLine($"⚠️  NOTE: Mean is {stats.Mean / expectedMean:F1}x higher than expected");
        }
        else if (stats.Mean < expectedMean * 0.5)
        {
            report.AppendLine($"✓ Performance better than expected ({expectedMean / stats.Mean:F1}x faster)");
        }
        else
        {
            report.AppendLine($"✓ Performance within expected range");
        }
        
        report.AppendLine($"═══════════════════════════════════════════════════════════════");
        report.AppendLine();
        
        return report.ToString();
    }
    
    /// <summary>
    /// Container for performance statistics
    /// </summary>
    private record PerformanceStatistics
    {
        public int Count { get; init; }
        public double Min { get; init; }
        public double Max { get; init; }
        public double Mean { get; init; }
        public double Median { get; init; }
        public double P95 { get; init; }
        public double P99 { get; init; }
        public double StdDev { get; init; }
    }
}
