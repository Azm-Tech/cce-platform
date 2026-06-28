using System.Globalization;
using CCE.Application.Community;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CCE.Infrastructure.Community;

/// <summary>
/// <see cref="IRedisFeedStore"/> implementation backed by StackExchange.Redis. All keys are
/// prefixed per the Spring 9 architecture: <c>feed:</c>, <c>post:</c>, <c>hot:</c>, <c>notif:</c>.
///
/// <para>
/// Every operation catches <see cref="RedisException"/> and degrades gracefully (returns empty
/// or null) so Redis outages do not crash the write path. The SQL database remains authoritative.
/// </para>
/// </summary>
public sealed class RedisFeedStore : IRedisFeedStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisFeedStore> _logger;

    private static readonly TimeSpan FeedTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan PostMetaTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan HotTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan NotifTtl = TimeSpan.FromHours(1);

    public RedisFeedStore(IConnectionMultiplexer redis, ILogger<RedisFeedStore> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    private IDatabase Db => _redis.GetDatabase();

    // ─── Feed ───

    public async Task AddToUserFeedAsync(Guid userId, Guid postId, DateTimeOffset publishedOn, CancellationToken ct = default)
    {
        try
        {
            var key = $"feed:user:{userId}";
            var score = publishedOn.ToUnixTimeSeconds();
            await Db.SortedSetAddAsync(key, postId.ToString(), score).ConfigureAwait(false);
            await Db.KeyExpireAsync(key, FeedTtl).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for AddToUserFeedAsync(user={UserId}, post={PostId}).", userId, postId);
        }
    }

    public async Task AddToCommunityFeedAsync(Guid communityId, Guid postId, DateTimeOffset publishedOn, CancellationToken ct = default)
    {
        try
        {
            var key = $"feed:community:{communityId}";
            var score = publishedOn.ToUnixTimeSeconds();
            await Db.SortedSetAddAsync(key, postId.ToString(), score).ConfigureAwait(false);
            await Db.KeyExpireAsync(key, FeedTtl).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for AddToCommunityFeedAsync(community={CommunityId}, post={PostId}).", communityId, postId);
        }
    }

    public async Task<IReadOnlyList<Guid>> GetUserFeedAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        try
        {
            var key = $"feed:user:{userId}";
            var start = (page - 1) * pageSize;
            var entries = await Db.SortedSetRangeByRankAsync(key, start, start + pageSize - 1, Order.Descending).ConfigureAwait(false);
            return entries.Select(e => Guid.Parse(e.ToString())).ToList();
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for GetUserFeedAsync(user={UserId}).", userId);
            return Array.Empty<Guid>();
        }
    }

    public async Task<IReadOnlyList<Guid>> GetCommunityFeedAsync(Guid communityId, int page, int pageSize, CancellationToken ct = default)
    {
        try
        {
            var key = $"feed:community:{communityId}";
            var start = (page - 1) * pageSize;
            var entries = await Db.SortedSetRangeByRankAsync(key, start, start + pageSize - 1, Order.Descending).ConfigureAwait(false);
            return entries.Select(e => Guid.Parse(e.ToString())).ToList();
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for GetCommunityFeedAsync(community={CommunityId}).", communityId);
            return Array.Empty<Guid>();
        }
    }

    public async Task<long> GetCommunityFeedCountAsync(Guid communityId, CancellationToken ct = default)
    {
        try
        {
            return await Db.SortedSetLengthAsync($"feed:community:{communityId}").ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for GetCommunityFeedCountAsync(community={CommunityId}).", communityId);
            return 0;
        }
    }

    public async Task<long> GetHotLeaderboardCountAsync(Guid communityId, CancellationToken ct = default)
    {
        try
        {
            return await Db.SortedSetLengthAsync($"hot:{communityId}").ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for GetHotLeaderboardCountAsync(community={CommunityId}).", communityId);
            return 0;
        }
    }

    public async Task RemoveFromUserFeedAsync(Guid userId, Guid postId, CancellationToken ct = default)
    {
        try
        {
            await Db.SortedSetRemoveAsync($"feed:user:{userId}", postId.ToString()).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for RemoveFromUserFeedAsync(user={UserId}, post={PostId}).", userId, postId);
        }
    }

    public async Task RemovePostFromAllFeedsAsync(Guid communityId, Guid postId, CancellationToken ct = default)
    {
        try
        {
            var db = Db;
            var member = postId.ToString();
            await db.SortedSetRemoveAsync($"feed:community:{communityId}", member).ConfigureAwait(false);
            await db.SortedSetRemoveAsync($"hot:{communityId}", member).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for RemovePostFromAllFeedsAsync(community={CommunityId}, post={PostId}).", communityId, postId);
        }
    }

    public async Task AddToUserFeedBatchAsync(
        IReadOnlyCollection<Guid> userIds, Guid postId, DateTimeOffset publishedOn, CancellationToken ct = default)
    {
        if (userIds.Count == 0) return;
        try
        {
            var db = Db;
            var score = publishedOn.ToUnixTimeSeconds();
            var member = postId.ToString();
            var batch = db.CreateBatch();
            var tasks = new List<Task>(userIds.Count * 2);
            foreach (var userId in userIds)
            {
                var key = $"feed:user:{userId}";
                tasks.Add(batch.SortedSetAddAsync(key, member, score));
                tasks.Add(batch.KeyExpireAsync(key, FeedTtl));
            }
            batch.Execute();
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for AddToUserFeedBatchAsync(post={PostId}, users={Count}).", postId, userIds.Count);
        }
    }

    // ─── Post hot counters ───

    public async Task IncrementPostVotesAsync(Guid postId, int upDelta, int downDelta, CancellationToken ct = default)
    {
        try
        {
            var key = $"post:{postId}:meta";
            if (upDelta != 0)
                await Db.HashIncrementAsync(key, "upvotes", upDelta).ConfigureAwait(false);
            if (downDelta != 0)
                await Db.HashIncrementAsync(key, "downvotes", downDelta).ConfigureAwait(false);
            await Db.KeyExpireAsync(key, PostMetaTtl).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for IncrementPostVotesAsync(post={PostId}).", postId);
        }
    }

    public async Task<(int Upvotes, int Downvotes)> GetPostVotesAsync(Guid postId, CancellationToken ct = default)
    {
        try
        {
            var key = $"post:{postId}:meta";
            var values = await Db.HashGetAsync(key, new RedisValue[] { "upvotes", "downvotes" }).ConfigureAwait(false);
            var up = values[0].IsNull ? 0 : (int)values[0];
            var down = values[1].IsNull ? 0 : (int)values[1];
            return (up, down);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for GetPostVotesAsync(post={PostId}).", postId);
            return (0, 0);
        }
    }

    public async Task SetPostMetaAsync(Guid postId, int upvotes, int downvotes, double score, int replyCount, CancellationToken ct = default)
    {
        try
        {
            var key = $"post:{postId}:meta";
            var hash = new HashEntry[]
            {
                new("upvotes", upvotes),
                new("downvotes", downvotes),
                new("score", score.ToString(CultureInfo.InvariantCulture)),
                new("replyCount", replyCount)
            };
            await Db.HashSetAsync(key, hash).ConfigureAwait(false);
            await Db.KeyExpireAsync(key, PostMetaTtl).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for SetPostMetaAsync(post={PostId}).", postId);
        }
    }

    public async Task<PostMeta?> GetPostMetaAsync(Guid postId, CancellationToken ct = default)
    {
        try
        {
            var key = $"post:{postId}:meta";
            var entries = await Db.HashGetAllAsync(key).ConfigureAwait(false);
            if (entries.Length == 0) return null;

            var dict = entries.ToDictionary(
                e => e.Name.ToString(),
                e => e.Value.ToString());

            return new PostMeta(
                dict.TryGetValue("upvotes", out var u) && int.TryParse(u, out var up) ? up : 0,
                dict.TryGetValue("downvotes", out var d) && int.TryParse(d, out var down) ? down : 0,
                dict.TryGetValue("score", out var s) && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var sc) ? sc : 0,
                dict.TryGetValue("replyCount", out var r) && int.TryParse(r, out var rc) ? rc : 0);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for GetPostMetaAsync(post={PostId}).", postId);
            return null;
        }
    }

    public async Task<long> GetUserFeedCountAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            return await Db.SortedSetLengthAsync($"feed:user:{userId}").ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for GetUserFeedCountAsync(user={UserId}).", userId);
            return 0;
        }
    }

    public async Task<IReadOnlyList<(Guid PostId, DateTimeOffset PublishedOn)>> GetUserFeedWithScoresAsync(
        Guid userId, int limit, CancellationToken ct = default)
    {
        try
        {
            var entries = await Db
                .SortedSetRangeByRankWithScoresAsync($"feed:user:{userId}", 0, limit - 1, Order.Descending)
                .ConfigureAwait(false);
            return entries
                .Select(e => (Guid.Parse(e.Element.ToString()), DateTimeOffset.FromUnixTimeSeconds((long)e.Score)))
                .ToList();
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for GetUserFeedWithScoresAsync(user={UserId}).", userId);
            return Array.Empty<(Guid, DateTimeOffset)>();
        }
    }

    public async Task<IReadOnlyDictionary<Guid, PostMeta>> GetPostsMetaBatchAsync(
        IReadOnlyCollection<Guid> postIds, CancellationToken ct = default)
    {
        if (postIds.Count == 0) return new Dictionary<Guid, PostMeta>();
        try
        {
            var db = Db;
            var batch = db.CreateBatch();
            var tasks = postIds.ToDictionary(
                id => id,
                id => batch.HashGetAllAsync($"post:{id}:meta"));
            batch.Execute();

            var result = new Dictionary<Guid, PostMeta>(postIds.Count);
            foreach (var (id, task) in tasks)
            {
                var entries = await task.ConfigureAwait(false);
                if (entries.Length == 0) continue;
                var dict = entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
                result[id] = new PostMeta(
                    dict.TryGetValue("upvotes", out var u) && int.TryParse(u, out var up) ? up : 0,
                    dict.TryGetValue("downvotes", out var d) && int.TryParse(d, out var dn) ? dn : 0,
                    dict.TryGetValue("score", out var s) && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var sc) ? sc : 0,
                    dict.TryGetValue("replyCount", out var r) && int.TryParse(r, out var rc) ? rc : 0);
            }
            return result;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for GetPostsMetaBatchAsync({Count} posts).", postIds.Count);
            return new Dictionary<Guid, PostMeta>();
        }
    }

    // ─── Hot leaderboards ───

    public async Task AddToHotLeaderboardAsync(Guid communityId, Guid postId, double score, CancellationToken ct = default)
    {
        try
        {
            var key = $"hot:{communityId}";
            await Db.SortedSetAddAsync(key, postId.ToString(), score).ConfigureAwait(false);
            // Only trim when the set exceeds 1 000. Using -1001 with ZREMRANGEBYRANK is unsafe for
            // smaller sets: Redis clamps the negative rank to 0 and removes the entry just added.
            var len = await Db.SortedSetLengthAsync(key).ConfigureAwait(false);
            if (len > 1000)
                await Db.SortedSetRemoveRangeByRankAsync(key, 0, len - 1001).ConfigureAwait(false);
            await Db.KeyExpireAsync(key, HotTtl).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for AddToHotLeaderboardAsync(community={CommunityId}, post={PostId}).", communityId, postId);
        }
    }

    public async Task RemoveFromHotLeaderboardAsync(Guid communityId, Guid postId, CancellationToken ct = default)
    {
        try
        {
            await Db.SortedSetRemoveAsync($"hot:{communityId}", postId.ToString()).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for RemoveFromHotLeaderboardAsync(community={CommunityId}, post={PostId}).", communityId, postId);
        }
    }

    public async Task<IReadOnlyList<Guid>> GetHotPostsAsync(Guid communityId, int page, int pageSize, CancellationToken ct = default)
    {
        try
        {
            var start = (page - 1) * pageSize;
            var stop  = start + pageSize - 1;
            var entries = await Db
                .SortedSetRangeByRankAsync($"hot:{communityId}", start, stop, Order.Descending)
                .ConfigureAwait(false);
            return entries.Select(e => Guid.Parse(e.ToString())).ToList();
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for GetHotPostsAsync(community={CommunityId}).", communityId);
            return Array.Empty<Guid>();
        }
    }

    // ─── Notifications ───

    public async Task IncrementNotificationCountAsync(Guid userId, int delta = 1, CancellationToken ct = default)
    {
        try
        {
            var key = $"notif:{userId}:count";
            var newVal = await Db.StringIncrementAsync(key, delta).ConfigureAwait(false);
            if (newVal <= 0)
                await Db.KeyDeleteAsync(key).ConfigureAwait(false);
            else
                await Db.KeyExpireAsync(key, NotifTtl).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for IncrementNotificationCountAsync(user={UserId}).", userId);
        }
    }

    public async Task<int> GetNotificationCountAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var val = await Db.StringGetAsync($"notif:{userId}:count").ConfigureAwait(false);
            return val.IsNull ? 0 : (int)val;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for GetNotificationCountAsync(user={UserId}).", userId);
            return 0;
        }
    }

    public async Task ResetNotificationCountAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            await Db.KeyDeleteAsync($"notif:{userId}:count").ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for ResetNotificationCountAsync(user={UserId}).", userId);
        }
    }
}
