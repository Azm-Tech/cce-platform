using CCE.Domain.Common;

namespace CCE.Domain.Identity.Events;

/// <summary>
/// Raised when an <see cref="ExpertRegistrationRequest"/> is approved. Phase 07's
/// DomainEventDispatcher routes it to a handler that creates the <c>ExpertProfile</c>.
/// </summary>
public sealed record ExpertRegistrationApprovedEvent(
    System.Guid RequestId,
    System.Guid RequestedById,
    System.Guid ApprovedById,
    string RequestedBioAr,
    string RequestedBioEn,
    IList<string> RequestedTags,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
