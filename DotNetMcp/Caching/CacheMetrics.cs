namespace DotNetMcp;

/// <summary>
/// Thread-safe cache metrics tracker for monitoring cache performance.
/// </summary>
public class CacheMetrics
{
    private long _hits;
    private long _misses;

    /// <summary>
    /// Gets the number of cache hits.
    /// </summary>
    public long Hits => Interlocked.Read(ref _hits);

    /// <summary>
    /// Gets the number of cache misses.
    /// </summary>
    public long Misses => Interlocked.Read(ref _misses);

    /// <summary>
    /// Gets the cache hit ratio (0.0 to 1.0).
    /// Reads both values atomically for a consistent snapshot.
    /// </summary>
    public double HitRatio
    {
        get
        {
            // Read both values together for a consistent snapshot
            long hits = Interlocked.Read(ref _hits);
            long misses = Interlocked.Read(ref _misses);
            long total = hits + misses;
            return total == 0 ? 0.0 : (double)hits / total;
        }
    }

    /// <summary>
    /// Records a cache hit.
    /// </summary>
    public void RecordHit() => Interlocked.Increment(ref _hits);

    /// <summary>
    /// Records a cache miss.
    /// </summary>
    public void RecordMiss() => Interlocked.Increment(ref _misses);

    /// <summary>
    /// Resets all metrics to zero.
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _hits, 0);
        Interlocked.Exchange(ref _misses, 0);
    }

    /// <summary>
    /// Returns a string representation of the metrics.
    /// </summary>
    public override string ToString()
    {
        return $"Hits: {Hits}, Misses: {Misses}, Hit Ratio: {HitRatio:P2}";
    }
}
