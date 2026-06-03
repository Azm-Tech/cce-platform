using CCE.Domain.Common;

namespace CCE.Domain.Country.Events;

public sealed record CountryContentRequestRejectedEvent(
    System.Guid RequestId,
    System.Guid CountryId,
    System.Guid RequestedById,
    ContentKind Kind,
    System.Guid RejectedById,
    string AdminNotesAr,
    string AdminNotesEn,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
