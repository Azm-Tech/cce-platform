using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Application.Community;
using CCE.Application.Common.Interfaces;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

/// <summary>
/// Consumes <see cref="PostCreatedIntegrationEvent"/> and rebuilds the <c>hot:{communityId}</c>
/// Redis sorted-set leaderboard from the SQL <c>Score</c> column. Trims to the top 1000 posts
/// per community. Concurrency limit = 1 to prevent ranking corruption under burst load.
/// </summary>
public sealed class RankingConsumer : IConsumer<PostCreatedIntegrationEvent>
{
    private readonly ICceDbContext _db;
    private readonly IRedisFeedStore _feedStore;
    private readonly ILogger<RankingConsumer> _logger;

    public RankingConsumer(ICceDbContext db, IRedisFeedStore feedStore, ILogger<RankingConsumer> logger)
    {
        _db = db;
        _feedStore = feedStore;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PostCreatedIntegrationEvent> context)
    {
        var evt = context.Message;
        _logger.LogDebug("RankingConsumer: Rebuilding hot leaderboard for CommunityId={CommunityId}", evt.CommunityId);

        // Rebuild the leaderboard from SQL (source of truth) — top 1000 published posts by Score.
        var posts = await _db.Posts
            .AsNoTracking()
            .Where(p => p.CommunityId == evt.CommunityId && p.Status == Domain.Community.PostStatus.Published)
            .OrderByDescending(p => p.Score)
            .Take(1000)
            .Select(p => new { p.Id, p.Score })
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);

        foreach (var post in posts)
        {
            await _feedStore.AddToHotLeaderboardAsync(evt.CommunityId, post.Id, post.Score, context.CancellationToken)
                .ConfigureAwait(false);
        }

        _logger.LogInformation(
            "RankingConsumer: Leaderboard rebuilt for CommunityId={CommunityId} with {Count} posts.",
            evt.CommunityId, posts.Count);
    }
}
