using CCE.Domain.Common;

namespace CCE.Domain.Identity.Events;

/// <summary>
/// Raised when an <see cref="ExpertRegistrationRequest"/> is rejected. Phase 07's
/// DomainEventDispatcher routes it to a notification handler that emails the requester.
/// </summary>
public sealed record ExpertRegistrationRejectedEvent(
    System.Guid RequestId,
    System.Guid RequestedById,
    System.Guid RejectedById,
    string RejectionReasonAr,
    string RejectionReasonEn,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
