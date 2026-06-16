using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Application.Community;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

/// <summary>
/// Consumes <see cref="VoteCreatedIntegrationEvent"/> from the bus and updates the Redis hot counters
/// (<c>post:{postId}:meta</c>). SQL is the source of truth; this keeps the hot-counter cache warm.
///
/// <para>The realtime <c>VoteChanged</c> SignalR push is owned by the API command handler
/// (<c>VotePostCommandHandler</c>) for instant actor feedback — this consumer deliberately does NOT
/// push it, to avoid clients receiving the event twice (hybrid realtime: direct push for feedback,
/// consumer for the durable counter side-effect).</para>
/// </summary>
public sealed class VoteConsumer : IConsumer<VoteCreatedIntegrationEvent>
{
    private readonly IRedisFeedStore _feedStore;
    private readonly ILogger<VoteConsumer> _logger;

    public VoteConsumer(IRedisFeedStore feedStore, ILogger<VoteConsumer> logger)
    {
        _feedStore = feedStore;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<VoteCreatedIntegrationEvent> context)
    {
        var evt = context.Message;
        _logger.LogDebug(
            "VoteConsumer: PostId={PostId} Direction={Direction} Up={Up} Down={Down} Score={Score}",
            evt.PostId, evt.Direction, evt.UpvoteCount, evt.DownvoteCount, evt.Score);

        // Derive counter deltas from the transition (previousDirection → direction).
        // Using PreviousDirection is the only way to handle retractions correctly: when
        // Direction=0 and PreviousDirection=1 the user removed their upvote, so upvotes -1.
        // Checking evt.UpvoteCount > 0 was wrong — if the last upvote was removed the
        // post-action count is already 0 and the Redis counter would not be decremented.
        var upDelta   = (evt.PreviousDirection == 1 ? -1 : 0) + (evt.Direction == 1 ? 1 : 0);
        var downDelta = (evt.PreviousDirection == -1 ? -1 : 0) + (evt.Direction == -1 ? 1 : 0);
        await _feedStore.IncrementPostVotesAsync(evt.PostId, upDelta, downDelta, context.CancellationToken)
            .ConfigureAwait(false);

        // Update the hot leaderboard score so ranking reflects votes without waiting for the next
        // post to be published (which was the only previous trigger for RankingConsumer).
        await _feedStore.AddToHotLeaderboardAsync(evt.CommunityId, evt.PostId, evt.Score, context.CancellationToken)
            .ConfigureAwait(false);
    }
}
