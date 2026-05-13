using CCE.Application.Common.Pagination;
using MediatR;

namespace CCE.Application.Community.Queries.ListAdminPosts;

/// <summary>
/// Admin moderation query — list community posts with optional filters.
///
/// Filters:
///   - <see cref="TopicId"/> — only posts in the given topic
///   - <see cref="Search"/>  — case-insensitive contains on Content
///   - <see cref="Status"/>  — "all" | "active" | "deleted" | "question" | "answered"
///   - <see cref="Locale"/>  — "ar" | "en"
/// </summary>
public sealed record ListAdminPostsQuery(
    int Page = 1,
    int PageSize = 20,
    System.Guid? TopicId = null,
    string? Search = null,
    string? Status = null,
    string? Locale = null) : IRequest<PagedResult<AdminPostRow>>;
