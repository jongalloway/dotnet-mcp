using DotNetMcp;
using FluentAssertions;
using Xunit;

namespace DotNetMcp.Tests;

public class CachedResourceManagerTests
{
    [Fact]
    public async Task GetOrLoadAsync_LoadsDataOnFirstCall()
    {
        // Arrange
        var manager = new CachedResourceManager<string>("TestResource");
        var loadCount = 0;

        // Act
        var entry = await manager.GetOrLoadAsync(async () =>
        {
            loadCount++;
            await Task.Delay(10);
            return "test data";
        });

        // Assert
        entry.Data.Should().Be("test data");
        loadCount.Should().Be(1);
        manager.Metrics.Misses.Should().Be(1);
        manager.Metrics.Hits.Should().Be(0);
    }

    [Fact]
    public async Task GetOrLoadAsync_UsesCache_OnSubsequentCalls()
    {
        // Arrange
        var manager = new CachedResourceManager<string>("TestResource");
        var loadCount = 0;

        // Act
        var entry1 = await manager.GetOrLoadAsync(async () =>
        {
            loadCount++;
            await Task.Delay(10);
            return "test data";
        });

        var entry2 = await manager.GetOrLoadAsync(async () =>
        {
            loadCount++;
            await Task.Delay(10);
            return "test data";
        });

        // Assert
        entry1.Data.Should().Be("test data");
        entry2.Data.Should().Be("test data");
        loadCount.Should().Be(1); // Should only load once
        manager.Metrics.Misses.Should().Be(1);
        manager.Metrics.Hits.Should().Be(1);
    }

    [Fact]
    public async Task GetOrLoadAsync_ReloadsData_WhenForceReloadIsTrue()
    {
        // Arrange
        var manager = new CachedResourceManager<string>("TestResource");
        var loadCount = 0;

        // Act
        var entry1 = await manager.GetOrLoadAsync(async () =>
        {
            loadCount++;
            return $"data {loadCount}";
        });

        var entry2 = await manager.GetOrLoadAsync(async () =>
        {
            loadCount++;
            return $"data {loadCount}";
        }, forceReload: true);

        // Assert
        entry1.Data.Should().Be("data 1");
        entry2.Data.Should().Be("data 2");
        loadCount.Should().Be(2); // Should load twice
        manager.Metrics.Misses.Should().Be(2);
        manager.Metrics.Hits.Should().Be(0);
    }

    [Fact]
    public async Task GetOrLoadAsync_ReloadsData_AfterCacheExpires()
    {
        // Arrange - Cache with 1 second TTL
        var manager = new CachedResourceManager<string>("TestResource", defaultTtlSeconds: 1);
        var loadCount = 0;

        // Act - Load once
        var entry1 = await manager.GetOrLoadAsync(async () =>
        {
            loadCount++;
            return $"data {loadCount}";
        });

        // Wait for cache to expire
        await Task.Delay(1100);

        // Load again
        var entry2 = await manager.GetOrLoadAsync(async () =>
        {
            loadCount++;
            return $"data {loadCount}";
        });

        // Assert
        entry1.Data.Should().Be("data 1");
        entry2.Data.Should().Be("data 2");
        loadCount.Should().Be(2); // Should load twice due to expiration
        manager.Metrics.Misses.Should().Be(2);
        manager.Metrics.Hits.Should().Be(0);
    }

    [Fact]
    public async Task ClearAsync_ClearsCache()
    {
        // Arrange
        var manager = new CachedResourceManager<string>("TestResource");
        var loadCount = 0;

        await manager.GetOrLoadAsync(async () =>
        {
            loadCount++;
            return "test data";
        });

        // Act
        await manager.ClearAsync();

        var entry = await manager.GetOrLoadAsync(async () =>
        {
            loadCount++;
            return "new data";
        });

        // Assert
        entry.Data.Should().Be("new data");
        loadCount.Should().Be(2); // Should reload after clearing
        manager.Metrics.Misses.Should().Be(2);
    }

    [Fact]
    public void ResetMetrics_ClearsMetrics()
    {
        // Arrange
        var manager = new CachedResourceManager<string>("TestResource");
        manager.Metrics.RecordHit();
        manager.Metrics.RecordHit();
        manager.Metrics.RecordMiss();

        // Act
        manager.ResetMetrics();

        // Assert
        manager.Metrics.Hits.Should().Be(0);
        manager.Metrics.Misses.Should().Be(0);
    }

    [Fact]
    public async Task GetJsonResponse_IncludesCacheMetadata()
    {
        // Arrange
        var manager = new CachedResourceManager<string>("TestResource");
        var entry = await manager.GetOrLoadAsync(async () => "test data");

        // Act
        var json = manager.GetJsonResponse(entry, new { value = "test" });

        // Assert
        json.Should().Contain("\"data\"");
        json.Should().Contain("\"cache\"");
        json.Should().Contain("\"timestamp\"");
        json.Should().Contain("\"cacheAgeSeconds\"");
        json.Should().Contain("\"cacheDurationSeconds\"");
        json.Should().Contain("\"metrics\"");
        json.Should().Contain("\"hits\"");
        json.Should().Contain("\"misses\"");
        json.Should().Contain("\"hitRatio\"");
    }

    [Fact]
    public async Task CachedEntry_IsExpired_WorksCorrectly()
    {
        // Arrange
        var entry = new CachedEntry<string>
        {
            Data = "test",
            CachedAt = DateTime.UtcNow.AddSeconds(-10),
            CacheDuration = TimeSpan.FromSeconds(5)
        };

        // Act & Assert
        entry.IsExpired(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public async Task CachedEntry_IsNotExpired_WorksCorrectly()
    {
        // Arrange
        var entry = new CachedEntry<string>
        {
            Data = "test",
            CachedAt = DateTime.UtcNow.AddSeconds(-2),
            CacheDuration = TimeSpan.FromSeconds(5)
        };

        // Act & Assert
        entry.IsExpired(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public async Task CachedEntry_CacheAgeSeconds_CalculatesCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var entry = new CachedEntry<string>
        {
            Data = "test",
            CachedAt = now.AddSeconds(-10),
            CacheDuration = TimeSpan.FromSeconds(60)
        };

        // Act
        var age = entry.CacheAgeSeconds(now);

        // Assert
        age.Should().Be(10);
    }
}
