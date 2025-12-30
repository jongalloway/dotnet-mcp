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
/// <remarks>
/// When used as a static instance (e.g., in DotNetResources or TemplateEngineHelper),
/// this class is intended to live for the application lifetime and disposal is handled
/// by the runtime during application shutdown. For non-static usage, ensure Dispose() is called.
/// </remarks>
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
    public CacheMetrics Metrics
    {
        get
        {
            ThrowIfDisposed();
            return _metrics;
        }
    }

    /// <summary>
    /// Gets or loads cached data, executing the loader function if cache is expired or forceReload is true.
    /// </summary>
    /// <param name="loader">Function to load fresh data when cache is expired.</param>
    /// <param name="forceReload">If true, forces a cache miss and reloads data.</param>
    /// <param name="customTtl">Optional custom TTL for this specific cache entry.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>Cached data with metadata.</returns>
    public async Task<CachedEntry<T>> GetOrLoadAsync(
        Func<Task<T>> loader,
        bool forceReload = false,
        TimeSpan? customTtl = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var now = DateTime.UtcNow;

        // Fast-path: Check cache without lock (common case: cache hit)
        if (!forceReload)
        {
            var cachedEntry = _cache;
            if (cachedEntry != null && !cachedEntry.IsExpired(now))
            {
                _metrics.RecordHit();
                _logger?.LogDebug("{ResourceName} cache hit - age: {AgeSeconds}s, metrics: {Metrics}",
                    _resourceName, cachedEntry.CacheAgeSeconds(now), _metrics);
                return cachedEntry;
            }
        }

        // Slow-path: Cache miss or expired, acquire lock
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check: Another thread may have loaded while we waited for the lock
            now = DateTime.UtcNow;
            if (!forceReload && _cache != null && !_cache.IsExpired(now))
            {
                _metrics.RecordHit();
                _logger?.LogDebug("{ResourceName} cache hit (after lock) - age: {AgeSeconds}s, metrics: {Metrics}",
                    _resourceName, _cache.CacheAgeSeconds(now), _metrics);
                return _cache;
            }

            // Need to reload
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

            return _cache;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <summary>
    /// Gets or loads cached data with cancellation support, executing the loader function if cache is expired or forceReload is true.
    /// </summary>
    /// <param name="loader">Function to load fresh data when cache is expired, which accepts a cancellation token.</param>
    /// <param name="forceReload">If true, forces a cache miss and reloads data.</param>
    /// <param name="customTtl">Optional custom TTL for this specific cache entry.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>Cached data with metadata.</returns>
    public async Task<CachedEntry<T>> GetOrLoadAsync(
        Func<CancellationToken, Task<T>> loader,
        bool forceReload = false,
        TimeSpan? customTtl = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var now = DateTime.UtcNow;

        // Fast-path: Check cache without lock (common case: cache hit)
        if (!forceReload)
        {
            var cachedEntry = _cache;
            if (cachedEntry != null && !cachedEntry.IsExpired(now))
            {
                _metrics.RecordHit();
                _logger?.LogDebug("{ResourceName} cache hit - age: {AgeSeconds}s, metrics: {Metrics}",
                    _resourceName, cachedEntry.CacheAgeSeconds(now), _metrics);
                return cachedEntry;
            }
        }

        // Slow-path: Cache miss or expired, acquire lock
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check: Another thread may have loaded while we waited for the lock
            now = DateTime.UtcNow;
            if (!forceReload && _cache != null && !_cache.IsExpired(now))
            {
                _metrics.RecordHit();
                _logger?.LogDebug("{ResourceName} cache hit (after lock) - age: {AgeSeconds}s, metrics: {Metrics}",
                    _resourceName, _cache.CacheAgeSeconds(now), _metrics);
                return _cache;
            }

            // Need to reload
            _metrics.RecordMiss();
            _logger?.LogDebug("{ResourceName} cache miss - loading fresh data (forceReload: {ForceReload})",
                _resourceName, forceReload);

            var data = await loader(cancellationToken);
            _cache = new CachedEntry<T>
            {
                Data = data,
                CachedAt = now,
                CacheDuration = customTtl ?? _defaultTtl
            };

            _logger?.LogInformation("{ResourceName} cache updated - expires in {Duration}s",
                _resourceName, _cache.CacheDuration.TotalSeconds);

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
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _cacheLock.WaitAsync(cancellationToken);
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
        ThrowIfDisposed();
        _metrics.Reset();
        _logger?.LogInformation("{ResourceName} cache metrics reset", _resourceName);
    }

    /// <summary>
    /// Gets a JSON response with cache metadata included.
    /// </summary>
    /// <param name="entry">The cached entry containing data and metadata.</param>
    /// <param name="additionalData">Additional data to include in the response.</param>
    /// <param name="now">The timestamp representing when the cache entry was accessed.</param>
    public string GetJsonResponse(CachedEntry<T> entry, object additionalData, DateTime now)
    {
        ThrowIfDisposed();
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

    private bool _disposed = false;

    /// <summary>
    /// Throws ObjectDisposedException if this instance has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }

    /// <summary>
    /// Disposes the resources used by the cache manager.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    /// <param name="disposing">True if called from Dispose; false if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose managed resources
            _cacheLock.Dispose();
        }

        // No unmanaged resources to release
        _disposed = true;
    }
}
