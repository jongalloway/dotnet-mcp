using DotNetMcp;
using Xunit;

namespace DotNetMcp.Tests;

public class CachedResourceManagerTests
{
    [Fact]
    public async Task GetOrLoadAsync_LoadsDataOnFirstCall()
    {
        // Arrange
        using var manager = new CachedResourceManager<string>("TestResource");
        var loadCount = 0;

        // Act
        var entry = await manager.GetOrLoadAsync(async () =>
        {
            loadCount++;
            await Task.Delay(10, TestContext.Current.CancellationToken);
            return "test data";
        });

        // Assert
        Assert.Equal("test data", entry.Data);
        Assert.Equal(1, loadCount);
        Assert.Equal(1, manager.Metrics.Misses);
        Assert.Equal(0, manager.Metrics.Hits);
    }

    [Fact]
    public async Task GetOrLoadAsync_UsesCache_OnSubsequentCalls()
    {
        // Arrange
        using var manager = new CachedResourceManager<string>("TestResource");
        var loadCount = 0;

        // Act
        var entry1 = await manager.GetOrLoadAsync(async () =>
        {
            loadCount++;
            await Task.Delay(10, TestContext.Current.CancellationToken);
            return "test data";
        });

        var entry2 = await manager.GetOrLoadAsync(async () =>
        {
            loadCount++;
            await Task.Delay(10, TestContext.Current.CancellationToken);
            return "test data";
        });

        // Assert
        Assert.Equal("test data", entry1.Data);
        Assert.Equal("test data", entry2.Data);
        Assert.Equal(1, loadCount); // Should only load once
        Assert.Equal(1, manager.Metrics.Misses);
        Assert.Equal(1, manager.Metrics.Hits);
    }

    [Fact]
    public async Task GetOrLoadAsync_ReloadsData_WhenForceReloadIsTrue()
    {
        // Arrange
        using var manager = new CachedResourceManager<string>("TestResource");
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
        Assert.Equal("data 1", entry1.Data);
        Assert.Equal("data 2", entry2.Data);
        Assert.Equal(2, loadCount); // Should load twice
        Assert.Equal(2, manager.Metrics.Misses);
        Assert.Equal(0, manager.Metrics.Hits);
    }

    [Fact]
    public async Task GetOrLoadAsync_ReloadsData_AfterCacheExpires()
    {
        // Arrange - Cache with 1 second TTL
        using var manager = new CachedResourceManager<string>("TestResource", defaultTtlSeconds: 1);
        var loadCount = 0;

        // Act - Load once
        var entry1 = await manager.GetOrLoadAsync(async () =>
        {
            loadCount++;
            return $"data {loadCount}";
        });

        // Wait for cache to expire
        await Task.Delay(1100, TestContext.Current.CancellationToken);

        // Load again
        var entry2 = await manager.GetOrLoadAsync(async () =>
        {
            loadCount++;
            return $"data {loadCount}";
        });

        // Assert
        Assert.Equal("data 1", entry1.Data);
        Assert.Equal("data 2", entry2.Data);
        Assert.Equal(2, loadCount); // Should load twice due to expiration
        Assert.Equal(2, manager.Metrics.Misses);
        Assert.Equal(0, manager.Metrics.Hits);
    }

    [Fact]
    public async Task ClearAsync_ClearsCache()
    {
        // Arrange
        using var manager = new CachedResourceManager<string>("TestResource");
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
        Assert.Equal("new data", entry.Data);
        Assert.Equal(2, loadCount); // Should reload after clearing
        Assert.Equal(2, manager.Metrics.Misses);
    }

    [Fact]
    public void ResetMetrics_ClearsMetrics()
    {
        // Arrange
        using var manager = new CachedResourceManager<string>("TestResource");
        manager.Metrics.RecordHit();
        manager.Metrics.RecordHit();
        manager.Metrics.RecordMiss();

        // Act
        manager.ResetMetrics();

        // Assert
        Assert.Equal(0, manager.Metrics.Hits);
        Assert.Equal(0, manager.Metrics.Misses);
    }

    [Fact]
    public async Task GetJsonResponse_IncludesCacheMetadata()
    {
        // Arrange
        using var manager = new CachedResourceManager<string>("TestResource");
        var entry = await manager.GetOrLoadAsync(async () => "test data");

        // Act
        var json = manager.GetJsonResponse(entry, new { value = "test" }, DateTime.UtcNow);

        // Assert
        Assert.Contains("\"data\"", json);
        Assert.Contains("\"cache\"", json);
        Assert.Contains("\"timestamp\"", json);
        Assert.Contains("\"cacheAgeSeconds\"", json);
        Assert.Contains("\"cacheDurationSeconds\"", json);
        Assert.Contains("\"metrics\"", json);
        Assert.Contains("\"hits\"", json);
        Assert.Contains("\"misses\"", json);
        Assert.Contains("\"hitRatio\"", json);
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
        Assert.True(entry.IsExpired(DateTime.UtcNow));
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
        Assert.False(entry.IsExpired(DateTime.UtcNow));
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
        Assert.Equal(10, age);
    }
}
