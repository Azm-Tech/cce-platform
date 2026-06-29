using CCE.Domain.Community;

namespace CCE.Application.Community.Public.Dtos;

/// <summary>
/// A single post in the community home feed. Same shape as <see cref="PublicPostDto"/> plus the
/// post's tag IDs (so the client can render/echo the active tag filter) and user-specific flags
/// (IsExpert, IsWatchlisted, VoteStatus) that are populated when a UserId is provided.
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
    int DownvoteCount,
    int CommentsCount,
    System.Collections.Generic.IReadOnlyList<System.Guid> AttachmentIds,
    System.Collections.Generic.IReadOnlyList<System.Guid> TagIds,
    System.DateTimeOffset CreatedOn,
    string TopicNameAr,
    string TopicNameEn,
    bool IsExpert,
    bool IsWatchlisted,
    int VoteStatus,
    PollSummaryDto? Poll,   // null for Info/Question posts
    // ── Search-only fields — null/false on all normal feed responses ──────────────────
    string? TitleHighlight = null,  // <em>-wrapped matched title fragment
    string? BodyHighlight  = null,  // <em>-wrapped matched content excerpt
    bool MatchedInReply    = false, // true when match was found in a reply, not the post body
    string? ReplyExcerpt   = null,  // highlighted reply fragment; null when MatchedInReply = false
    string? MainImageUrl   = null); // public URL of the first image attachment; null when post has no images
