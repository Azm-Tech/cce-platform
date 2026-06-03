using CCE.Domain.Common;

namespace CCE.Domain.Content.Events;

public sealed record NewsPublishedEvent(
    System.Guid NewsId,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
