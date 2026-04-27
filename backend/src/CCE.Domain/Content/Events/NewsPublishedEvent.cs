using CCE.Domain.Common;

namespace CCE.Domain.Content.Events;

public sealed record NewsPublishedEvent(
    System.Guid NewsId,
    string Slug,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
