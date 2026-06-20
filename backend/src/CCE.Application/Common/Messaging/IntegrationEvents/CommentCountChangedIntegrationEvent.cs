namespace CCE.Application.Common.Messaging.IntegrationEvents;

/// <summary>
/// Raised when a post's <c>CommentsCount</c> changes (reply created or deleted).
/// Consumed by <c>ReplyCountConsumer</c> in the Worker to keep <c>post:{id}:meta.replyCount</c>
/// in Redis in sync with the SQL aggregate.
/// </summary>
public sealed record CommentCountChangedIntegrationEvent(
    System.Guid PostId,
    int CommentsCount);
