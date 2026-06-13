using CCE.Domain.Common;

namespace CCE.Domain.Content.Events;

public sealed record NewsPublishedEvent(
    System.Guid NewsId,
    System.Guid TopicId,
    System.Guid AuthorId,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
