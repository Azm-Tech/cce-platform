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

        // Update Redis hot counters (best-effort; SQL is source of truth).
        var upDelta = evt.Direction == 1 ? 1 : evt.Direction == -1 ? 0 : evt.UpvoteCount > 0 ? -1 : 0;
        var downDelta = evt.Direction == -1 ? 1 : evt.Direction == 1 ? 0 : evt.DownvoteCount > 0 ? -1 : 0;
        await _feedStore.IncrementPostVotesAsync(evt.PostId, upDelta, downDelta, context.CancellationToken)
            .ConfigureAwait(false);
    }
}
