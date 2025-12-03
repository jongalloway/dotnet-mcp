using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests;

public class CacheMetricsTests
{
    [Fact]
    public void CacheMetrics_StartsWithZeroValues()
    {
        // Arrange & Act
        var metrics = new CacheMetrics();

        // Assert
        Assert.Equal(0, metrics.Hits);
        Assert.Equal(0, metrics.Misses);
        Assert.Equal(0.0, metrics.HitRatio);
    }

    [Fact]
    public void RecordHit_IncrementsHitCounter()
    {
        // Arrange
        var metrics = new CacheMetrics();

        // Act
        metrics.RecordHit();
        metrics.RecordHit();
        metrics.RecordHit();

        // Assert
        Assert.Equal(3, metrics.Hits);
        Assert.Equal(0, metrics.Misses);
    }

    [Fact]
    public void RecordMiss_IncrementsMissCounter()
    {
        // Arrange
        var metrics = new CacheMetrics();

        // Act
        metrics.RecordMiss();
        metrics.RecordMiss();

        // Assert
        Assert.Equal(0, metrics.Hits);
        Assert.Equal(2, metrics.Misses);
    }

    [Fact]
    public void HitRatio_CalculatesCorrectly()
    {
        // Arrange
        var metrics = new CacheMetrics();

        // Act
        metrics.RecordHit();
        metrics.RecordHit();
        metrics.RecordHit();
        metrics.RecordMiss();

        // Assert - 3 hits / 4 total = 0.75
        Assert.Equal(0.75, metrics.HitRatio, precision: 2);
    }

    [Fact]
    public void HitRatio_ReturnsZero_WhenNoRecords()
    {
        // Arrange
        var metrics = new CacheMetrics();

        // Act & Assert
        Assert.Equal(0.0, metrics.HitRatio);
    }

    [Fact]
    public void Reset_ClearsAllMetrics()
    {
        // Arrange
        var metrics = new CacheMetrics();
        metrics.RecordHit();
        metrics.RecordHit();
        metrics.RecordMiss();

        // Act
        metrics.Reset();

        // Assert
        Assert.Equal(0, metrics.Hits);
        Assert.Equal(0, metrics.Misses);
        Assert.Equal(0.0, metrics.HitRatio);
    }

    [Fact]
    public void ToString_ReturnsFormattedMetrics()
    {
        // Arrange
        var metrics = new CacheMetrics();
        metrics.RecordHit();
        metrics.RecordHit();
        metrics.RecordHit();
        metrics.RecordMiss();

        // Act
        var result = metrics.ToString();

        // Assert
        Assert.Contains("Hits: 3", result);
        Assert.Contains("Misses: 1", result);
        Assert.Contains("Hit Ratio: 75", result);
    }

    [Fact]
    public async Task CacheMetrics_IsThreadSafe()
    {
        // Arrange
        var metrics = new CacheMetrics();
        var tasks = new List<Task>();

        // Act - Concurrently record hits and misses
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() => metrics.RecordHit()));
            tasks.Add(Task.Run(() => metrics.RecordMiss()));
        }

        await Task.WhenAll(tasks);

        // Assert - Should have exactly 100 hits and 100 misses
        Assert.Equal(100, metrics.Hits);
        Assert.Equal(100, metrics.Misses);
        Assert.Equal(0.5, metrics.HitRatio, precision: 2);
    }
}
