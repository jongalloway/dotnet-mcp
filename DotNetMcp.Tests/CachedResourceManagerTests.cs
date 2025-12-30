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

    [Fact]
    public async Task GetOrLoadAsync_CancelsWaitingForLock()
    {
        // Arrange
        using var manager = new CachedResourceManager<string>("TestResource");
        using var cts = new CancellationTokenSource();
        var firstCallStarted = new TaskCompletionSource<bool>();
        var firstCallCanComplete = new TaskCompletionSource<bool>();

        // Start a long-running operation that holds the lock
        var firstTask = Task.Run(async () =>
        {
            await manager.GetOrLoadAsync(async () =>
            {
                firstCallStarted.SetResult(true);
                await firstCallCanComplete.Task;
                return "first data";
            }, cancellationToken: TestContext.Current.CancellationToken);
        });

        // Wait for the first call to start and acquire the lock
        await firstCallStarted.Task;

        // Act - Start second call that will wait for the lock
        var secondTask = manager.GetOrLoadAsync(async () => "second data", cancellationToken: cts.Token);

        // Give the second call a moment to start and begin waiting for the lock
        await Task.Delay(50, TestContext.Current.CancellationToken);

        // Cancel while the second call is waiting for the lock
        cts.Cancel();

        // Assert - Second call should be cancelled while waiting for the lock
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await secondTask);

        // Cleanup - allow first task to complete
        firstCallCanComplete.SetResult(true);
        await firstTask;
    }

    [Fact]
    public async Task GetOrLoadAsync_CancelsDuringLoad()
    {
        // Arrange
        using var manager = new CachedResourceManager<string>("TestResource");
        using var cts = new CancellationTokenSource();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await manager.GetOrLoadAsync(async (ct) =>
            {
                // Cancel during the load operation
                cts.Cancel();
                await Task.Delay(100, ct);
                return "data";
            }, cancellationToken: cts.Token);
        });
    }

    [Fact]
    public async Task GetOrLoadAsync_WithCancellableLoader_PassesCancellationToken()
    {
        // Arrange
        using var manager = new CachedResourceManager<string>("TestResource");
        using var cts = new CancellationTokenSource();
        CancellationToken? passedToken = null;

        // Act
        var entry = await manager.GetOrLoadAsync(async (ct) =>
        {
            passedToken = ct;
            await Task.Delay(10, TestContext.Current.CancellationToken);
            return "test data";
        }, cancellationToken: cts.Token);

        // Assert
        Assert.NotNull(passedToken);
        Assert.True(passedToken.Value.CanBeCanceled);
        Assert.Equal(cts.Token, passedToken.Value);
        Assert.Equal("test data", entry.Data);
    }

    [Fact]
    public async Task GetOrLoadAsync_WithCancellableLoader_UsesCachedDataWhenAvailable()
    {
        // Arrange
        using var manager = new CachedResourceManager<string>("TestResource");
        var loadCount = 0;

        // First call to populate cache
        await manager.GetOrLoadAsync(async (ct) =>
        {
            loadCount++;
            await Task.Delay(10, TestContext.Current.CancellationToken);
            return "cached data";
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Act - Second call should use cached data without calling loader
        var entry = await manager.GetOrLoadAsync(async (ct) =>
        {
            loadCount++;
            await Task.Delay(10, TestContext.Current.CancellationToken);
            return "new data";
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("cached data", entry.Data);
        Assert.Equal(1, loadCount); // Should only load once
        Assert.Equal(1, manager.Metrics.Misses);
        Assert.Equal(1, manager.Metrics.Hits);
    }

    [Fact]
    public async Task ClearAsync_SupportsCancellation()
    {
        // Arrange
        using var manager = new CachedResourceManager<string>("TestResource");
        using var loaderCts = new CancellationTokenSource();
        var loaderStarted = new TaskCompletionSource<bool>();

        // Start a long-running GetOrLoadAsync call to hold the internal lock
        var loaderTask = manager.GetOrLoadAsync(async ct =>
        {
            loaderStarted.TrySetResult(true);
            try
            {
                // Hold the lock for a long time until cancelled
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
            }
            catch (OperationCanceledException)
            {
                // Swallow cancellation to allow task to complete gracefully
            }

            return "test data";
        }, cancellationToken: loaderCts.Token);

        // Ensure the loader has started and is holding the lock
        await loaderStarted.Task;

        using var clearCts = new CancellationTokenSource();

        // Act - start ClearAsync so it waits for the lock
        var clearTask = Task.Run(async () => await manager.ClearAsync(clearCts.Token));

        // Give ClearAsync a moment to reach the lock acquisition point
        await Task.Delay(50, TestContext.Current.CancellationToken);

        // Cancel while ClearAsync is waiting
        clearCts.Cancel();

        // Assert - ClearAsync should observe cancellation and throw
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await clearTask);

        // Cleanup: release the loader so the test can complete
        loaderCts.Cancel();
        try
        {
            await loaderTask;
        }
        catch (OperationCanceledException)
        {
            // Expected during cleanup
        }
    }

    [Fact]
    public async Task GetOrLoadAsync_DefaultCancellationToken_WorksCorrectly()
    {
        // Arrange
        using var manager = new CachedResourceManager<string>("TestResource");

        // Act - Call without specifying cancellation token (uses default)
        var entry = await manager.GetOrLoadAsync(async () =>
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
            return "test data";
        });

        // Assert
        Assert.Equal("test data", entry.Data);
        Assert.Equal(1, manager.Metrics.Misses);
    }
}
