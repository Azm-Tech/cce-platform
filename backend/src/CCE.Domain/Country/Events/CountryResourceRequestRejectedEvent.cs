using CCE.Domain.Common;

namespace CCE.Domain.Country.Events;

public sealed record CountryContentRequestRejectedEvent(
    System.Guid RequestId,
    System.Guid CountryId,
    System.Guid RequestedById,
    ContentType Type,
    System.Guid RejectedById,
    string AdminNotesAr,
    string AdminNotesEn,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
