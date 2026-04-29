namespace CCE.Application.Community.Public.Dtos;

public sealed record MyFollowsDto(
    IReadOnlyList<System.Guid> TopicIds,
    IReadOnlyList<System.Guid> UserIds,
    IReadOnlyList<System.Guid> PostIds);
