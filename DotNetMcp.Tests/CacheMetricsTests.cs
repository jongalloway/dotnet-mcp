using DotNetMcp;
using FluentAssertions;
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
        metrics.Hits.Should().Be(0);
        metrics.Misses.Should().Be(0);
        metrics.HitRatio.Should().Be(0.0);
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
        metrics.Hits.Should().Be(3);
        metrics.Misses.Should().Be(0);
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
        metrics.Hits.Should().Be(0);
        metrics.Misses.Should().Be(2);
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

        // Assert
        metrics.HitRatio.Should().BeApproximately(0.75, 0.01); // 3 hits / 4 total = 0.75
    }

    [Fact]
    public void HitRatio_ReturnsZero_WhenNoRecords()
    {
        // Arrange
        var metrics = new CacheMetrics();

        // Act & Assert
        metrics.HitRatio.Should().Be(0.0);
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
        metrics.Hits.Should().Be(0);
        metrics.Misses.Should().Be(0);
        metrics.HitRatio.Should().Be(0.0);
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
        result.Should().Contain("Hits: 3");
        result.Should().Contain("Misses: 1");
        result.Should().Contain("Hit Ratio: 75");
    }

    [Fact]
    public void CacheMetrics_IsThreadSafe()
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

        Task.WaitAll(tasks.ToArray());

        // Assert - Should have exactly 100 hits and 100 misses
        metrics.Hits.Should().Be(100);
        metrics.Misses.Should().Be(100);
        metrics.HitRatio.Should().BeApproximately(0.5, 0.01);
    }
}
