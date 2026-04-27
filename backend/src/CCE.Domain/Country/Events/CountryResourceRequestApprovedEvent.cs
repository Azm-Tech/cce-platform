using CCE.Domain.Common;

namespace CCE.Domain.Country.Events;

public sealed record CountryResourceRequestApprovedEvent(
    System.Guid RequestId,
    System.Guid CountryId,
    System.Guid RequestedById,
    System.Guid ApprovedById,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
