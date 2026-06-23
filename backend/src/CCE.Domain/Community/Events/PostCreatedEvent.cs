using CCE.Domain.Common;

namespace CCE.Domain.Community.Events;

public sealed record PostCreatedEvent(
    System.Guid PostId,
    System.Guid CommunityId,
    System.Guid TopicId,
    System.Guid AuthorId,
    string Locale,
    string Title,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
