using CCE.Domain.Common;

namespace CCE.Domain.Content.Events;

public sealed record EventScheduledEvent(
    System.Guid EventId,
    System.DateTimeOffset StartsOn,
    System.DateTimeOffset EndsOn,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
