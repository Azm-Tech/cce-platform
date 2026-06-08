using CCE.Domain.Community;

namespace CCE.Application.Community.Public.Dtos;

public sealed record JoinRequestDto(
    System.Guid Id,
    System.Guid CommunityId,
    System.Guid UserId,
    JoinRequestStatus Status,
    System.DateTimeOffset RequestedOn,
    System.DateTimeOffset? DecidedOn);
