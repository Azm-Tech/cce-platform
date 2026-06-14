using CCE.Domain.Community;

namespace CCE.Application.Community.Public.Dtos;

/// <summary>
/// Enriched single-post view returned by GET /api/community/posts/{id}.
/// Author data is nested in <see cref="PostAuthorDto"/>. User-specific flags
/// (IsWatchlisted, VoteStatus) are populated when a UserId is provided.
/// </summary>
public sealed record PostDetailDto(
    System.Guid Id,
    System.Guid CommunityId,
    System.Guid TopicId,
    PostAuthorDto Author,
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
    System.DateTimeOffset CreatedOn,
    string TopicNameAr,
    string TopicNameEn,
    bool IsWatchlisted,
    int VoteStatus);
