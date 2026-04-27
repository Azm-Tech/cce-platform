using CCE.Domain.Common;

namespace CCE.Domain.Content.Events;

/// <summary>
/// Raised when a <c>Resource</c> is first published (transitions from draft to public).
/// Phase 07 dispatches this to the search-index updater + recommendation cache invalidator.
/// </summary>
public sealed record ResourcePublishedEvent(
    System.Guid ResourceId,
    System.Guid? CountryId,
    System.Guid CategoryId,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
