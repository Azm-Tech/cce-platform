using CCE.Application.Common.Caching;
using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Application.Community;
using CCE.Application.Common.Interfaces;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

/// <summary>
/// Consumes <see cref="PostCreatedIntegrationEvent"/> from the bus and fans out the post ID into
/// Redis feed keys. Implements the Spring 9 hybrid fan-out strategy:
///
/// <list type="bullet">
///   <item><description><b>Celebrity/Expert authors</b> (IsExpert=true OR FollowerCount &gt; threshold):
///     skip fan-out (feed is merged dynamically at read time).</description></item>
///   <item><description><b>Normal authors</b>: push post ID into every follower's
///     <c>feed:user:{followerId}</c> Redis sorted-set via a single pipelined batch.</description></item>
/// </list>
///
/// <para>Also updates the community public feed <c>feed:community:{communityId}</c> and the
/// hot leaderboard via <see cref="IRedisFeedStore"/>.</para>
/// </summary>
public sealed class FeedConsumer : IConsumer<PostCreatedIntegrationEvent>
{
    private readonly ICceDbContext _db;
    private readonly IRedisFeedStore _feedStore;
    private readonly IOutputCacheInvalidator _cacheInvalidator;
    private readonly ILogger<FeedConsumer> _logger;
    private readonly CceInfrastructureOptions _opts;

    public FeedConsumer(
        ICceDbContext db,
        IRedisFeedStore feedStore,
        IOutputCacheInvalidator cacheInvalidator,
        IOptions<CceInfrastructureOptions> opts,
        ILogger<FeedConsumer> logger)
    {
        _db = db;
        _feedStore = feedStore;
        _cacheInvalidator = cacheInvalidator;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PostCreatedIntegrationEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation(
            "FeedConsumer: PostCreated PostId={PostId} Community={CommunityId} Author={AuthorId}",
            evt.PostId, evt.CommunityId, evt.AuthorId);

        // EF Core DbContext is not thread-safe — queries on the same instance must be sequential.
        var isExpert = await _db.ExpertProfiles
            .AnyAsync(e => e.UserId == evt.AuthorId, context.CancellationToken)
            .ConfigureAwait(false);
        var author = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == evt.AuthorId, context.CancellationToken)
            .ConfigureAwait(false);

        var isCelebrity = isExpert || (author?.FollowerCount > _opts.CelebrityFollowerThreshold);

        // Always update the community public feed and hot leaderboard (independent of celebrity).
        await _feedStore.AddToCommunityFeedAsync(evt.CommunityId, evt.PostId, evt.PublishedOn, context.CancellationToken)
            .ConfigureAwait(false);
        await _feedStore.AddToHotLeaderboardAsync(evt.CommunityId, evt.PostId, 0, context.CancellationToken)
            .ConfigureAwait(false);

        if (isCelebrity)
        {
            _logger.LogInformation(
                "FeedConsumer: Author {AuthorId} is celebrity/expert — skipping personal feed fan-out.",
                evt.AuthorId);
            await _cacheInvalidator
                .EvictRegionsAsync([CacheRegions.Posts, CacheRegions.Feed], context.CancellationToken)
                .ConfigureAwait(false);
            return;
        }

        // Gather followers sequentially — EF Core DbContext is not thread-safe.
        var followerIds = new HashSet<Guid>();

        var userFollowers = await _db.UserFollows
            .AsNoTracking()
            .Where(f => f.FollowedId == evt.AuthorId)
            .Select(f => f.FollowerId)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);
        followerIds.UnionWith(userFollowers);

        var communityFollowers = await _db.CommunityFollows
            .AsNoTracking()
            .Where(f => f.CommunityId == evt.CommunityId)
            .Select(f => f.UserId)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);
        followerIds.UnionWith(communityFollowers);

        var topicFollowers = await _db.TopicFollows
            .AsNoTracking()
            .Where(f => f.TopicId == evt.TopicId)
            .Select(f => f.UserId)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);
        followerIds.UnionWith(topicFollowers);

        // Fan-out into all follower personal feeds in one pipelined Redis batch.
        await _feedStore.AddToUserFeedBatchAsync(followerIds, evt.PostId, evt.PublishedOn, context.CancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "FeedConsumer: Fan-out complete for PostId={PostId} — {Count} followers.",
            evt.PostId, followerIds.Count);

        await _cacheInvalidator
            .EvictRegionsAsync([CacheRegions.Posts, CacheRegions.Feed], context.CancellationToken)
            .ConfigureAwait(false);
    }
}
