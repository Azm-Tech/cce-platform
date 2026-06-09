using CCE.Domain.Community;

namespace CCE.Application.Community.Public.Dtos;

public sealed record PublicPostDto(
    System.Guid Id,
    System.Guid CommunityId,
    System.Guid TopicId,
    System.Guid AuthorId,
    string? AuthorName,
    PostType Type,
    string? Title,
    string? Content,
    string Locale,
    bool IsAnswerable,
    System.Guid? AnsweredReplyId,
    int UpvoteCount,
    int DownvoteCount,
    int CommentsCount,
    System.Collections.Generic.IReadOnlyList<System.Guid> AttachmentIds,
    System.DateTimeOffset CreatedOn);
