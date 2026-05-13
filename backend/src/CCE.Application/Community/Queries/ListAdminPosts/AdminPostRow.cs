namespace CCE.Application.Community.Queries.ListAdminPosts;

/// <summary>
/// Admin-side row for the community-posts moderation list. Joins the
/// Post aggregate with its parent Topic (for the locale-aware topic
/// name) and computes the reply count. Soft-deleted rows are included
/// when the caller passes `IncludeDeleted = true` so moderators can
/// review what's been removed without having to query by id.
/// </summary>
public sealed record AdminPostRow(
    System.Guid Id,
    System.Guid TopicId,
    string TopicNameEn,
    string TopicNameAr,
    System.Guid AuthorId,
    string Content,
    string Locale,
    bool IsAnswerable,
    bool IsAnswered,
    bool IsDeleted,
    System.DateTimeOffset CreatedOn,
    System.DateTimeOffset? DeletedOn,
    int ReplyCount);
