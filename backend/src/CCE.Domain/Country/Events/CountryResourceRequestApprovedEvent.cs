using CCE.Domain.Common;

namespace CCE.Domain.Country.Events;

public sealed record CountryContentRequestApprovedEvent(
    System.Guid RequestId,
    System.Guid CountryId,
    System.Guid RequestedById,
    ContentKind Kind,
    System.Guid ApprovedById,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
