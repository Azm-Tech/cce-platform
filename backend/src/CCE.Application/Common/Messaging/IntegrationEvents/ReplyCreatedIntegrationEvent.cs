namespace CCE.Application.Common.Messaging.IntegrationEvents;

/// <summary>
/// Raised when a reply (root or nested) is created on a post. Captured by the EF outbox
/// in the same transaction as the reply row + mention rows. Triggers notification dispatch
/// to post followers and parent-reply author in the Worker.
/// </summary>
public sealed record ReplyCreatedIntegrationEvent(
    System.Guid ReplyId,
    System.Guid PostId,
    System.Guid? ParentReplyId,
    System.Guid AuthorId,
    string ContentSnippet,
    System.DateTimeOffset CreatedOn);
