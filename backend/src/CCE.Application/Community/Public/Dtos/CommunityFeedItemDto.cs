using CCE.Domain.Community;

namespace CCE.Application.Community.Public.Dtos;

/// <summary>
/// A single post in the community home feed. Same shape as <see cref="PublicPostDto"/> plus the
/// post's tag IDs (so the client can render/echo the active tag filter). Exposes
/// <see cref="UpvoteCount"/> only — never <c>DownvoteCount</c> (US027).
/// </summary>
public sealed record CommunityFeedItemDto(
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
    int CommentsCount,
    System.Collections.Generic.IReadOnlyList<System.Guid> AttachmentIds,
    System.Collections.Generic.IReadOnlyList<System.Guid> TagIds,
    System.DateTimeOffset CreatedOn);
