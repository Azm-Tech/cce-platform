using CCE.Domain.Common;

namespace CCE.Domain.Content.Events;

public sealed record NewsletterConfirmedEvent(
    System.Guid SubscriptionId,
    string Email,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
