using CCE.Domain.Community;

namespace CCE.Application.Community.Public.Dtos;

public sealed record PublicPostDto(
    System.Guid Id,
    System.Guid CommunityId,
    System.Guid TopicId,
    System.Guid AuthorId,
    PostType Type,
    string? Title,
    string? Content,
    string Locale,
    bool IsAnswerable,
    System.Guid? AnsweredReplyId,
    int UpvoteCount,
    System.DateTimeOffset CreatedOn);
