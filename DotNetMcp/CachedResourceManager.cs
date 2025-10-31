using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DotNetMcp;

/// <summary>
/// Represents a cached resource entry with metadata.
/// </summary>
/// <typeparam name="T">The type of data being cached.</typeparam>
public class CachedEntry<T> where T : notnull
{
    public required T Data { get; set; }
    public DateTime CachedAt { get; set; }
    public TimeSpan CacheDuration { get; set; }

    public bool IsExpired(DateTime now) => now > CachedAt.Add(CacheDuration);

    public int CacheAgeSeconds(DateTime now) => (int)(now - CachedAt).TotalSeconds;
}

/// <summary>
/// Generic cache manager for readonly resources with configurable TTL and metrics.
/// Thread-safe implementation using SemaphoreSlim for async operations.
/// </summary>
/// <typeparam name="T">The type of data being cached.</typeparam>
public class CachedResourceManager<T> : IDisposable where T : notnull
{
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private CachedEntry<T>? _cache;
    private readonly TimeSpan _defaultTtl;
    private readonly CacheMetrics _metrics = new();
    private readonly ILogger? _logger;
    private readonly string _resourceName;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedResourceManager{T}"/> class.
    /// </summary>
    /// <param name="resourceName">Name of the resource for logging purposes.</param>
    /// <param name="defaultTtlSeconds">Default cache TTL in seconds (default: 300).</param>
    /// <param name="logger">Optional logger instance.</param>
    public CachedResourceManager(string resourceName, int defaultTtlSeconds = 300, ILogger? logger = null)
    {
        _resourceName = resourceName;
        _defaultTtl = TimeSpan.FromSeconds(defaultTtlSeconds);
        _logger = logger;
    }

    /// <summary>
    /// Gets cache metrics for this resource.
    /// </summary>
    public CacheMetrics Metrics => _metrics;

    /// <summary>
    /// Gets or loads cached data, executing the loader function if cache is expired or forceReload is true.
    /// </summary>
    /// <param name="loader">Function to load fresh data when cache is expired.</param>
    /// <param name="forceReload">If true, forces a cache miss and reloads data.</param>
    /// <param name="customTtl">Optional custom TTL for this specific cache entry.</param>
    /// <returns>Cached data with metadata.</returns>
    public async Task<CachedEntry<T>> GetOrLoadAsync(
        Func<Task<T>> loader,
        bool forceReload = false,
        TimeSpan? customTtl = null)
    {
        var now = DateTime.UtcNow;

        await _cacheLock.WaitAsync();
        try
        {
            // Check if we need to reload
            if (forceReload || _cache == null || _cache.IsExpired(now))
            {
                _metrics.RecordMiss();
                _logger?.LogDebug("{ResourceName} cache miss - loading fresh data (forceReload: {ForceReload})",
                    _resourceName, forceReload);

                var data = await loader();
                _cache = new CachedEntry<T>
                {
                    Data = data,
                    CachedAt = now,
                    CacheDuration = customTtl ?? _defaultTtl
                };

                _logger?.LogInformation("{ResourceName} cache updated - expires in {Duration}s",
                    _resourceName, _cache.CacheDuration.TotalSeconds);
            }
            else
            {
                _metrics.RecordHit();
                _logger?.LogDebug("{ResourceName} cache hit - age: {AgeSeconds}s, metrics: {Metrics}",
                    _resourceName, _cache.CacheAgeSeconds(now), _metrics);
            }

            return _cache;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Clears the cache, forcing next access to reload data.
    /// </summary>
    public async Task ClearAsync()
    {
        await _cacheLock.WaitAsync();
        try
        {
            _cache = null;
            _logger?.LogInformation("{ResourceName} cache cleared", _resourceName);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Resets cache metrics (hits and misses).
    /// </summary>
    public void ResetMetrics()
    {
        _metrics.Reset();
        _logger?.LogInformation("{ResourceName} cache metrics reset", _resourceName);
    }

    /// <summary>
    /// Gets a JSON response with cache metadata included.
    /// </summary>
    public string GetJsonResponse(CachedEntry<T> entry, object additionalData)
    {
        var now = DateTime.UtcNow;
        var response = new
        {
            data = additionalData,
            cache = new
            {
                timestamp = entry.CachedAt.ToString("O"),
                cacheAgeSeconds = entry.CacheAgeSeconds(now),
                cacheDurationSeconds = (int)entry.CacheDuration.TotalSeconds,
                metrics = new
                {
                    hits = _metrics.Hits,
                    misses = _metrics.Misses,
                    hitRatio = _metrics.HitRatio
                }
            }
        };

        return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Disposes the SemaphoreSlim resource.
    /// </summary>
    public void Dispose()
    {
        _cacheLock.Dispose();
    }
}
