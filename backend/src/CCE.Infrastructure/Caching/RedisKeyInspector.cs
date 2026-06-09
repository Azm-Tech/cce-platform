using CCE.Application.Common.Caching;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CCE.Infrastructure.Caching;

/// <summary>
/// Redis implementation of <see cref="IRedisKeyInspector"/>. Uses <c>SCAN</c> (via StackExchange.Redis
/// <c>IServer.KeysAsync</c>) to list keys without blocking the server. Degrades gracefully when Redis
/// is unreachable.
/// </summary>
public sealed class RedisKeyInspector : IRedisKeyInspector
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisKeyInspector> _logger;

    public RedisKeyInspector(IConnectionMultiplexer redis, ILogger<RedisKeyInspector> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string pattern, int count, CancellationToken cancellationToken)
    {
        var keys = new List<string>();
        try
        {
            // Try IServer.KeysAsync first (works on local/standalone Redis).
            var server = _redis.GetServers().FirstOrDefault();
            if (server is not null)
            {
                await foreach (var key in server.KeysAsync(pattern: pattern, pageSize: count).WithCancellation(cancellationToken))
                {
                    keys.Add(key.ToString());
                    if (keys.Count >= count)
                        break;
                }
                return keys;
            }

            // Fallback for cloud/clustered instances: use IDatabase.ExecuteAsync with SCAN.
            var db = _redis.GetDatabase();
            var cursor = 0;
            do
            {
                var result = await db.ExecuteAsync("SCAN", cursor, "MATCH", pattern, "COUNT", count).ConfigureAwait(false);
                if (result is null || result.IsNull) break;
                var arr = (RedisResult[])result!;
                if (arr.Length < 2) break;
                cursor = (int)arr[0];
                var batch = (RedisResult[])arr[1]!;
                foreach (var item in batch)
                {
                    keys.Add(item.ToString());
                    if (keys.Count >= count)
                        break;
                }
            } while (cursor != 0 && keys.Count < count);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable while scanning keys with pattern {Pattern}; returning empty result.", pattern);
        }
        return keys;
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key).ConfigureAwait(false);
            return value.HasValue ? value.ToString() : null;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable while reading value for key {Key}; returning null.", key);
            return null;
        }
    }

    public async Task<string> GetKeyTypeAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var db = _redis.GetDatabase();
            var type = await db.KeyTypeAsync(key).ConfigureAwait(false);
            return type.ToString().ToLowerInvariant();
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable while reading type for key {Key}; returning 'none'.", key);
            return "none";
        }
    }
}
