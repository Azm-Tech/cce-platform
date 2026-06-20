using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Application.Community;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

/// <summary>
/// Consumes <see cref="VoteCreatedIntegrationEvent"/> and updates Redis with authoritative counts
/// from the domain aggregate. Uses <see cref="IRedisFeedStore.SetPostMetaAsync"/> (absolute set)
/// rather than HINCRBY increments so the consumer is fully idempotent — replaying the message on
/// MassTransit retry sets the same values rather than double-counting.
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

        // Read the existing replyCount so it is not clobbered by the vote update.
        // Vote events do not carry reply counts; reply count is owned by reply consumers.
        var existing = await _feedStore.GetPostMetaAsync(evt.PostId, context.CancellationToken)
            .ConfigureAwait(false);

        // Absolute write — idempotent on retry. Uses authoritative counts from the domain event
        // (already committed to SQL), so Redis always converges to the correct value.
        await _feedStore.SetPostMetaAsync(
            evt.PostId,
            evt.UpvoteCount,
            evt.DownvoteCount,
            evt.Score,
            replyCount: existing?.ReplyCount ?? 0,
            context.CancellationToken)
            .ConfigureAwait(false);

        // Update the hot leaderboard score so ranking reflects votes in real time.
        await _feedStore.AddToHotLeaderboardAsync(
            evt.CommunityId, evt.PostId, evt.Score, context.CancellationToken)
            .ConfigureAwait(false);
    }
}
