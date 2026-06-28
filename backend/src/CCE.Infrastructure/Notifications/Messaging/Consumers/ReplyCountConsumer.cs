using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Application.Community;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

/// <summary>
/// Consumes <see cref="CommentCountChangedIntegrationEvent"/> and updates the
/// <c>replyCount</c> field in <c>post:{id}:meta</c> Redis hash.
///
/// <para>
/// Strategy: read the existing meta first. If it already exists (written by a prior
/// <see cref="VoteConsumer"/> pass), update only <c>replyCount</c> while preserving
/// <c>upvotes</c>, <c>downvotes</c>, and <c>score</c>. If no meta key is present yet,
/// skip — the key will be created by <see cref="VoteConsumer"/> on the first vote, and
/// the SQL <c>CommentsCount</c> column remains the fallback until then. This avoids
/// writing a partial hash that would corrupt vote counts read by query handlers.
/// </para>
///
/// <para>Idempotent: replaying the same event writes the same absolute count.</para>
/// </summary>
public sealed class ReplyCountConsumer : IConsumer<CommentCountChangedIntegrationEvent>
{
    private readonly IRedisFeedStore _feedStore;
    private readonly ILogger<ReplyCountConsumer> _logger;

    public ReplyCountConsumer(IRedisFeedStore feedStore, ILogger<ReplyCountConsumer> logger)
    {
        _feedStore = feedStore;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CommentCountChangedIntegrationEvent> context)
    {
        var evt = context.Message;
        _logger.LogDebug(
            "ReplyCountConsumer: PostId={PostId} CommentsCount={CommentsCount}",
            evt.PostId, evt.CommentsCount);

        var existing = await _feedStore.GetPostMetaAsync(evt.PostId, context.CancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            // No meta key yet — VoteConsumer creates it on the first vote, which will then
            // preserve the replyCount via the read-modify-write in VoteConsumer. Until then,
            // query handlers fall back to the SQL CommentsCount column.
            return;
        }

        await _feedStore.SetPostMetaAsync(
            evt.PostId,
            existing.Upvotes,
            existing.Downvotes,
            existing.Score,
            replyCount: evt.CommentsCount,
            context.CancellationToken)
            .ConfigureAwait(false);
    }
}
