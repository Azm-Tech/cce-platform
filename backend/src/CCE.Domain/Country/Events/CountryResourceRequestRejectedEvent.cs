using CCE.Domain.Common;

namespace CCE.Domain.Country.Events;

public sealed record CountryResourceRequestRejectedEvent(
    System.Guid RequestId,
    System.Guid CountryId,
    System.Guid RequestedById,
    System.Guid RejectedById,
    string AdminNotesAr,
    string AdminNotesEn,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
