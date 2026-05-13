using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Domain.Community;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Community.Queries.ListAdminPosts;

/// <summary>
/// Builds the admin community-posts moderation list. The Post aggregate
/// is filtered server-side (status / topic / search / locale), the
/// matching page is joined to its parent Topic for the locale-aware
/// topic name, and the reply count is computed via a grouped sub-query
/// against PostReplies (excluding soft-deleted replies).
/// </summary>
public sealed class ListAdminPostsQueryHandler
    : IRequestHandler<ListAdminPostsQuery, PagedResult<AdminPostRow>>
{
    private readonly ICceDbContext _db;

    public ListAdminPostsQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AdminPostRow>> Handle(
        ListAdminPostsQuery request,
        CancellationToken cancellationToken)
    {
        // Start from the global query filter override so we can see deleted rows.
        IQueryable<Post> posts = _db.Posts.IgnoreQueryFilters();

        // ─── Filters ───────────────────────────────────────
        switch (request.Status?.ToLowerInvariant())
        {
            case "active":
                posts = posts.Where(p => !p.IsDeleted);
                break;
            case "deleted":
                posts = posts.Where(p => p.IsDeleted);
                break;
            case "question":
                posts = posts.Where(p => !p.IsDeleted && p.IsAnswerable && p.AnsweredReplyId == null);
                break;
            case "answered":
                posts = posts.Where(p => !p.IsDeleted && p.IsAnswerable && p.AnsweredReplyId != null);
                break;
            case "all":
            case null:
            case "":
                // No status filter — show everything (active + deleted).
                break;
            default:
                // Unknown status keyword → treat as no filter (defensive).
                break;
        }

        if (request.TopicId is { } topicId)
        {
            posts = posts.Where(p => p.TopicId == topicId);
        }
        if (!string.IsNullOrWhiteSpace(request.Locale))
        {
            var locale = request.Locale.ToLowerInvariant();
            if (locale == "ar" || locale == "en")
            {
                posts = posts.Where(p => p.Locale == locale);
            }
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            // EF translates EF.Functions.Like to provider-specific LIKE.
            posts = posts.Where(p => EF.Functions.Like(p.Content, $"%{term}%"));
        }

        posts = posts.OrderByDescending(p => p.CreatedOn);

        var pagePostsResult = await posts
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        if (pagePostsResult.Items.Count == 0)
        {
            return new PagedResult<AdminPostRow>(
                System.Array.Empty<AdminPostRow>(),
                pagePostsResult.Page, pagePostsResult.PageSize, pagePostsResult.Total);
        }

        // ─── Lookups for the page slice only ────────────────
        var pagePostIds = pagePostsResult.Items.Select(p => p.Id).ToList();
        var pageTopicIds = pagePostsResult.Items.Select(p => p.TopicId).Distinct().ToList();

        // Topic names (always include even soft-deleted topics so admins
        // can spot dangling references when needed).
        var topicRows = await _db.Topics.IgnoreQueryFilters()
            .Where(t => pageTopicIds.Contains(t.Id))
            .Select(t => new { t.Id, t.NameEn, t.NameAr })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var topicById = topicRows.ToDictionary(t => t.Id);

        // Reply counts — excluding soft-deleted replies (default scope OK).
        var replyCountRows = await _db.PostReplies
            .Where(r => pagePostIds.Contains(r.PostId))
            .GroupBy(r => r.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var replyCountByPost = replyCountRows.ToDictionary(r => r.PostId, r => r.Count);

        var items = pagePostsResult.Items.Select(p =>
        {
            topicById.TryGetValue(p.TopicId, out var topic);
            replyCountByPost.TryGetValue(p.Id, out var replyCount);
            return new AdminPostRow(
                Id: p.Id,
                TopicId: p.TopicId,
                TopicNameEn: topic?.NameEn ?? string.Empty,
                TopicNameAr: topic?.NameAr ?? string.Empty,
                AuthorId: p.AuthorId,
                Content: p.Content,
                Locale: p.Locale,
                IsAnswerable: p.IsAnswerable,
                IsAnswered: p.AnsweredReplyId != null,
                IsDeleted: p.IsDeleted,
                CreatedOn: p.CreatedOn,
                DeletedOn: p.DeletedOn,
                ReplyCount: replyCount);
        }).ToList();

        return new PagedResult<AdminPostRow>(
            items, pagePostsResult.Page, pagePostsResult.PageSize, pagePostsResult.Total);
    }
}
