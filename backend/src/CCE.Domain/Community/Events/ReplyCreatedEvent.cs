using CCE.Domain.Common;

namespace CCE.Domain.Community.Events;

/// <summary>
/// Raised on the <see cref="Post"/> aggregate when a reply (root or nested) is created on it.
/// Translated to a <c>ReplyCreatedIntegrationEvent</c> by a bridge handler and relayed to the Worker
/// for notification fan-out to post followers and the parent-reply author.
/// </summary>
public sealed record ReplyCreatedEvent(
    System.Guid ReplyId,
    System.Guid PostId,
    System.Guid? ParentReplyId,
    System.Guid AuthorId,
    string ContentSnippet,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
