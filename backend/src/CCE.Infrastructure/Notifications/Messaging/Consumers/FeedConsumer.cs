using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Application.Community;
using CCE.Application.Common.Interfaces;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

/// <summary>
/// Consumes <see cref="PostCreatedIntegrationEvent"/> from the bus and fan-outs the post ID into
/// Redis feed keys. Implements the Spring 9 hybrid fan-out strategy:
///
/// <list type="bullet">
///   <item><description><b>Celebrity/Expert authors</b> (IsExpert=true OR FollowerCount &gt; threshold):
///     skip fan-out (feed is merged dynamically at read time).</description></item>
///   <item><description><b>Normal authors</b>: push post ID into every follower's
///     <c>feed:user:{followerId}</c> Redis sorted-set.</description></item>
/// </list>
///
/// <para>Also updates the community public feed <c>feed:community:{communityId}</c> and the
/// hot leaderboard via <see cref="IRedisFeedStore"/>.</para>
/// </summary>
public sealed class FeedConsumer : IConsumer<PostCreatedIntegrationEvent>
{
    private readonly ICceDbContext _db;
    private readonly IRedisFeedStore _feedStore;
    private readonly ILogger<FeedConsumer> _logger;

    public FeedConsumer(ICceDbContext db, IRedisFeedStore feedStore, ILogger<FeedConsumer> logger)
    {
        _db = db;
        _feedStore = feedStore;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PostCreatedIntegrationEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation(
            "FeedConsumer: PostCreated PostId={PostId} Community={CommunityId} Author={AuthorId}",
            evt.PostId, evt.CommunityId, evt.AuthorId);

        // Resolve celebrity status (expert OR high follower count).
        var isExpert = evt.IsExpert || await _db.ExpertProfiles
            .AnyAsync(e => e.UserId == evt.AuthorId, context.CancellationToken).ConfigureAwait(false);
        var author = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == evt.AuthorId, context.CancellationToken)
            .ConfigureAwait(false);
        var isCelebrity = isExpert || (author?.FollowerCount > 10_000);

        // Always update the community public feed (independent of celebrity).
        await _feedStore.AddToCommunityFeedAsync(evt.CommunityId, evt.PostId, evt.PublishedOn, context.CancellationToken)
            .ConfigureAwait(false);

        // Update hot leaderboard.
        await _feedStore.AddToHotLeaderboardAsync(evt.CommunityId, evt.PostId, 0, context.CancellationToken)
            .ConfigureAwait(false);

        if (isCelebrity)
        {
            _logger.LogInformation(
                "FeedConsumer: Author {AuthorId} is celebrity/expert — skipping personal feed fan-out.",
                evt.AuthorId);
            return;
        }

        // Gather followers: users who follow the author, the community, or the topic.
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

        // Fan-out into each follower's personal feed.
        foreach (var userId in followerIds)
        {
            await _feedStore.AddToUserFeedAsync(userId, evt.PostId, evt.PublishedOn, context.CancellationToken)
                .ConfigureAwait(false);
        }

        _logger.LogInformation(
            "FeedConsumer: Fan-out complete for PostId={PostId} — {Count} followers.",
            evt.PostId, followerIds.Count);
    }
}
