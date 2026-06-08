using CCE.Domain.Community;

namespace CCE.Application.Community.Public.Dtos;

public sealed record MyMentionDto(
    System.Guid Id,
    MentionSourceType SourceType,
    System.Guid SourceId,
    System.Guid MentionedByUserId,
    System.DateTimeOffset CreatedOn);
