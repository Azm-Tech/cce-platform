using CCE.Domain.Common;

namespace CCE.Domain.Country.Events;

public sealed record CountryContentRequestApprovedEvent(
    System.Guid RequestId,
    System.Guid CountryId,
    System.Guid RequestedById,
    ContentType Type,
    System.Guid ApprovedById,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
