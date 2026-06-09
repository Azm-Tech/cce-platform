namespace CCE.Application.Common.Caching;

/// <summary>
/// Invalidates the Redis-backed HTTP output cache by region or by key. The Application layer depends only
/// on this abstraction; the Redis implementation lives in Infrastructure. All methods degrade gracefully
/// (a Redis outage is logged and treated as a no-op) so cache management never faults a request.
/// </summary>
public interface IOutputCacheInvalidator
{
    /// <summary>Purge every cached entry in the given regions (and the region index sets).</summary>
    Task EvictRegionsAsync(IEnumerable<string> regions, CancellationToken cancellationToken);

    /// <summary>Delete a single cache entry by its full key. Returns 1 if a key was removed, else 0.</summary>
    Task<long> EvictKeyAsync(string key, CancellationToken cancellationToken);

    /// <summary>Per-region entry counts (the cache "tables" and how many entries each holds).</summary>
    Task<IReadOnlyList<CacheRegionStatus>> GetStatusAsync(CancellationToken cancellationToken);

    /// <summary>Purge every known region.</summary>
    Task FlushAllAsync(CancellationToken cancellationToken);
}

/// <summary>A cache region and the number of live entries indexed under it.</summary>
public sealed record CacheRegionStatus(string Region, long Entries);
