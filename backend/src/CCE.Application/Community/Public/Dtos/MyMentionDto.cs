using CCE.Domain.Community;

namespace CCE.Application.Community.Public.Dtos;

public sealed record MyMentionDto(
    System.Guid Id,
    MentionSourceType SourceType,
    System.Guid SourceId,
    System.Guid PostId,
    System.Guid CommunityId,
    System.Guid MentionedByUserId,
    string MentionedByName,
    string? MentionedByAvatarUrl,
    string Snippet,
    System.DateTimeOffset CreatedOn);
