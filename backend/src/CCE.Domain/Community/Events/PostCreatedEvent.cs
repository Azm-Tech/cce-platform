using CCE.Domain.Common;

namespace CCE.Domain.Community.Events;

public sealed record PostCreatedEvent(
    System.Guid PostId,
    System.Guid TopicId,
    System.Guid AuthorId,
    string Locale,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
