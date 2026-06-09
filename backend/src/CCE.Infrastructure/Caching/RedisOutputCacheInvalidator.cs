using CCE.Application.Common.Caching;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CCE.Infrastructure.Caching;

/// <summary>
/// Redis implementation of <see cref="IOutputCacheInvalidator"/>. Uses the per-region tag SET written by
/// <c>RedisOutputCacheMiddleware</c> (<c>out:tag:&lt;region&gt;</c>) to clear a region without scanning the
/// keyspace. Mirrors the middleware's graceful-degradation contract: a <see cref="RedisException"/> is
/// logged and swallowed so an admin call or a write never 500s because Redis is down.
/// </summary>
public sealed class RedisOutputCacheInvalidator : IOutputCacheInvalidator
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisOutputCacheInvalidator> _logger;

    public RedisOutputCacheInvalidator(
        IConnectionMultiplexer redis,
        ILogger<RedisOutputCacheInvalidator> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task EvictRegionsAsync(IEnumerable<string> regions, CancellationToken cancellationToken)
    {
        try
        {
            var db = _redis.GetDatabase();
            foreach (var region in regions.Distinct(System.StringComparer.OrdinalIgnoreCase))
            {
                var tagKey = (RedisKey)CacheRegions.TagSetKey(region);
                var members = await db.SetMembersAsync(tagKey).ConfigureAwait(false);
                if (members.Length > 0)
                {
                    var keys = System.Array.ConvertAll(members, m => (RedisKey)m.ToString());
                    await db.KeyDeleteAsync(keys).ConfigureAwait(false);
                }
                await db.KeyDeleteAsync(tagKey).ConfigureAwait(false);
            }
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable while evicting cache regions; skipping.");
        }
    }

    public async Task<long> EvictKeyAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var db = _redis.GetDatabase();
            return await db.KeyDeleteAsync(key).ConfigureAwait(false) ? 1 : 0;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable while deleting cache key {Key}; skipping.", key);
            return 0;
        }
    }

    public async Task<IReadOnlyList<CacheRegionStatus>> GetStatusAsync(CancellationToken cancellationToken)
    {
        var statuses = new List<CacheRegionStatus>(CacheRegions.All.Count);
        try
        {
            var db = _redis.GetDatabase();
            foreach (var region in CacheRegions.All)
            {
                var count = await db.SetLengthAsync(CacheRegions.TagSetKey(region)).ConfigureAwait(false);
                statuses.Add(new CacheRegionStatus(region, count));
            }
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable while reading cache status; returning partial result.");
        }
        return statuses;
    }

    public Task FlushAllAsync(CancellationToken cancellationToken)
        => EvictRegionsAsync(CacheRegions.All, cancellationToken);
}
