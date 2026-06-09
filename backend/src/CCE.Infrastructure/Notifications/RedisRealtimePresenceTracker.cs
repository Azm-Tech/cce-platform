using CCE.Application.Common.Realtime;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CCE.Infrastructure.Notifications;

/// <summary>
/// Redis-backed <see cref="IRealtimePresenceTracker"/>. Per post a HASH <c>presence:post:{id}</c> maps
/// connectionId → userId; the viewer count is the number of <em>distinct</em> users (a user with two tabs
/// counts once). Per connection a SET <c>presence:conn:{id}</c> records the posts it joined so a disconnect
/// can clean them all up. Best-effort: a <see cref="RedisException"/> degrades to "no presence".
/// </summary>
public sealed class RedisRealtimePresenceTracker : IRealtimePresenceTracker
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(12);

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisRealtimePresenceTracker> _logger;

    public RedisRealtimePresenceTracker(
        IConnectionMultiplexer redis,
        ILogger<RedisRealtimePresenceTracker> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    private static RedisKey PostKey(Guid postId) => $"presence:post:{postId}";
    private static RedisKey ConnKey(string connectionId) => $"presence:conn:{connectionId}";

    public async Task<int> JoinAsync(Guid postId, string userId, string connectionId, CancellationToken cancellationToken)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.HashSetAsync(PostKey(postId), connectionId, userId).ConfigureAwait(false);
            await db.SetAddAsync(ConnKey(connectionId), postId.ToString()).ConfigureAwait(false);
            await db.KeyExpireAsync(PostKey(postId), Ttl).ConfigureAwait(false);
            await db.KeyExpireAsync(ConnKey(connectionId), Ttl).ConfigureAwait(false);
            return await DistinctViewersAsync(db, postId).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for presence join (post {PostId}); skipping.", postId);
            return 0;
        }
    }

    public async Task<int> LeaveAsync(Guid postId, string userId, string connectionId, CancellationToken cancellationToken)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.HashDeleteAsync(PostKey(postId), connectionId).ConfigureAwait(false);
            await db.SetRemoveAsync(ConnKey(connectionId), postId.ToString()).ConfigureAwait(false);
            return await DistinctViewersAsync(db, postId).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for presence leave (post {PostId}); skipping.", postId);
            return 0;
        }
    }

    public async Task<IReadOnlyList<PresenceChange>> LeaveAllAsync(string connectionId, CancellationToken cancellationToken)
    {
        try
        {
            var db = _redis.GetDatabase();
            var posts = await db.SetMembersAsync(ConnKey(connectionId)).ConfigureAwait(false);
            var changes = new List<PresenceChange>(posts.Length);
            foreach (var member in posts)
            {
                if (!Guid.TryParse(member.ToString(), out var postId)) continue;
                await db.HashDeleteAsync(PostKey(postId), connectionId).ConfigureAwait(false);
                changes.Add(new PresenceChange(postId, await DistinctViewersAsync(db, postId).ConfigureAwait(false)));
            }
            await db.KeyDeleteAsync(ConnKey(connectionId)).ConfigureAwait(false);
            return changes;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for presence leave-all (connection {ConnectionId}); skipping.", connectionId);
            return [];
        }
    }

    private static async Task<int> DistinctViewersAsync(IDatabase db, Guid postId)
    {
        var values = await db.HashValuesAsync(PostKey(postId)).ConfigureAwait(false);
        return values.Select(v => v.ToString()).Distinct(StringComparer.Ordinal).Count();
    }
}
