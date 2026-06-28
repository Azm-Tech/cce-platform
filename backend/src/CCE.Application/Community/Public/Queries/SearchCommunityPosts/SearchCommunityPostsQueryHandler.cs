using System.Collections.Generic;
using System.Linq;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Community.Public.Queries.ListCommunityFeed;
using CCE.Application.Messages;
using CCE.Application.Search;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Public.Queries.SearchCommunityPosts;

public sealed class SearchCommunityPostsQueryHandler
    : IRequestHandler<SearchCommunityPostsQuery, Response<PagedResult<CommunityFeedItemDto>>>
{
    private readonly ISearchClient _searchClient;
    private readonly ICceDbContext _db;
    private readonly FeedHydratorService _hydratorService;
    private readonly MessageFactory _msg;

    public SearchCommunityPostsQueryHandler(
        ISearchClient searchClient,
        ICceDbContext db,
        FeedHydratorService hydratorService,
        MessageFactory msg)
    {
        _searchClient     = searchClient;
        _db               = db;
        _hydratorService  = hydratorService;
        _msg              = msg;
    }

    public async Task<Response<PagedResult<CommunityFeedItemDto>>> Handle(
        SearchCommunityPostsQuery request, CancellationToken cancellationToken)
    {
        var page     = System.Math.Max(1, request.Page);
        var pageSize = System.Math.Clamp(request.PageSize, 1, PaginationExtensions.MaxPageSize);

        // Fetch enough candidates from Meilisearch to cover reasonable pagination depth.
        // Posts beyond position 500 in Meilisearch relevance ranking are not reachable.
        var limit = System.Math.Min(System.Math.Max(10 * pageSize, 200), 500);

        var rawResult = await _searchClient
            .SearchCommunityPostsAsync(request.SearchTerm, limit, cancellationToken)
            .ConfigureAwait(false);

        var postHits  = rawResult.PostHits;
        var replyHits = rawResult.ReplyHits;

        var allPostIds = postHits.Select(h => h.PostId)
            .Union(replyHits.Select(h => h.PostId))
            .ToList();

        if (allPostIds.Count == 0)
        {
            return _msg.Ok(
                new PagedResult<CommunityFeedItemDto>(System.Array.Empty<CommunityFeedItemDto>(), page, pageSize, 0),
                MessageKeys.General.ITEMS_LISTED);
        }

        // Visibility guard + ID containment + optional extra filters.
        var baseQuery = (
            from p in _db.Posts
            join c in _db.Communities on p.CommunityId equals c.Id
            where p.Status == PostStatus.Published
               && c.IsActive
               && c.Visibility == CommunityVisibility.Public
               && allPostIds.Contains(p.Id)
            select p)
            .WhereIf(request.CommunityId.HasValue, p => p.CommunityId == request.CommunityId!.Value)
            .WhereIf(request.TopicId.HasValue,     p => p.TopicId      == request.TopicId!.Value)
            .WhereIf(request.PostType.HasValue,    p => p.Type         == request.PostType!.Value)
            .WhereIf(request.AuthorId.HasValue,    p => p.AuthorId     == request.AuthorId!.Value);

        IReadOnlyList<System.Guid> pageIds;
        long total;

        if (request.Sort is null)
        {
            // ── Relevance path: reorder by Meilisearch rank in memory ──────────────────────
            var filteredIds = await baseQuery
                .Select(p => p.Id)
                .ToListAsyncEither(cancellationToken)
                .ConfigureAwait(false);

            var filteredSet = filteredIds.ToHashSet();
            var postHitById = postHits.ToDictionary(h => h.PostId);

            // Best (lowest-rank) reply hit per parent post, for reply-only matches.
            var bestReplyByPost = replyHits
                .GroupBy(h => h.PostId)
                .ToDictionary(g => g.Key, g => g.OrderBy(h => h.MeiliRank).First());

            var directPostIds = postHits
                .Where(h => filteredSet.Contains(h.PostId))
                .OrderBy(h => h.MeiliRank)
                .Select(h => h.PostId)
                .ToList();

            var replyOnlyPostIds = bestReplyByPost.Keys
                .Where(id => filteredSet.Contains(id) && !postHitById.ContainsKey(id))
                .OrderBy(id => bestReplyByPost[id].MeiliRank)
                .ToList();

            var orderedIds = directPostIds.Concat(replyOnlyPostIds).ToList();
            total   = orderedIds.Count;
            pageIds = orderedIds.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }
        else
        {
            // ── Sort path: SQL ORDER BY + OFFSET/LIMIT ─────────────────────────────────────
            var sortedQuery = request.Sort switch
            {
                PostFeedSort.Newest => baseQuery
                    .OrderByDescending(p => p.PublishedOn ?? p.CreatedOn)
                    .ThenByDescending(p => p.Id),
                PostFeedSort.TopVoted => baseQuery
                    .OrderByDescending(p => p.UpvoteCount)
                    .ThenByDescending(p => p.Score),
                PostFeedSort.MostCommented => baseQuery
                    .OrderByDescending(p => p.CommentsCount)
                    .ThenByDescending(p => p.Score),
                _ => baseQuery
                    .OrderByDescending(p => p.Score),
            };

            var pagedResult = await sortedQuery
                .Select(p => p.Id)
                .ToPagedResultAsync(page, pageSize, cancellationToken)
                .ConfigureAwait(false);

            pageIds = pagedResult.Items;
            total   = pagedResult.Total;
        }

        if (pageIds.Count == 0)
        {
            return _msg.Ok(
                new PagedResult<CommunityFeedItemDto>(System.Array.Empty<CommunityFeedItemDto>(), page, pageSize, total),
                MessageKeys.General.ITEMS_LISTED);
        }

        var hydratedItems = await _hydratorService
            .HydrateAsync(pageIds, request.UserId, null, cancellationToken)
            .ConfigureAwait(false);

        var postHitMap = postHits.ToDictionary(h => h.PostId);
        var replyHitMap = replyHits
            .GroupBy(h => h.PostId)
            .ToDictionary(g => g.Key, g => g.OrderBy(h => h.MeiliRank).First());

        var enriched = hydratedItems.Select(dto =>
        {
            postHitMap.TryGetValue(dto.Id, out var postHit);
            replyHitMap.TryGetValue(dto.Id, out var replyHit);
            return dto with
            {
                TitleHighlight = postHit?.HighlightedTitle,
                BodyHighlight  = postHit?.ExcerptContent,
                MatchedInReply = postHit is null && replyHit is not null,
                ReplyExcerpt   = replyHit?.Excerpt,
            };
        }).ToList();

        return _msg.Ok(
            new PagedResult<CommunityFeedItemDto>(enriched, page, pageSize, total),
            MessageKeys.General.ITEMS_LISTED);
    }
}
