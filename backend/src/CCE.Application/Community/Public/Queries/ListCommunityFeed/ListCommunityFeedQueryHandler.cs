using System.Collections.Generic;
using System.Linq;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListCommunityFeed;

/// <summary>
/// Reads the community home feed. Ordering is taken from the Redis fan-out read-model
/// (<see cref="IRedisFeedStore"/>) for community-scoped Hot/Newest queries with no tag filter,
/// and from SQL for global, tag-filtered, top-voted, or Redis-miss queries. When a topicId is
/// specified, a wider window is fetched from Redis and filtered inside
/// <see cref="FeedHydratorService"/> — pages that exceed the window fall through to SQL.
/// SQL is always the source of truth for hydrated post data and the visibility guard.
/// </summary>
public sealed class ListCommunityFeedQueryHandler
    : IRequestHandler<ListCommunityFeedQuery, Response<PagedResult<CommunityFeedItemDto>>>
{
    private readonly ICceDbContext _db;
    private readonly IRedisFeedStore _feedStore;
    private readonly MessageFactory _msg;
    private readonly FeedHydratorService _hydratorService;

    public ListCommunityFeedQueryHandler(
        ICceDbContext db,
        IRedisFeedStore feedStore,
        MessageFactory msg,
        FeedHydratorService hydratorService)
    {
        _db = db;
        _feedStore = feedStore;
        _msg = msg;
        _hydratorService = hydratorService;
    }

    public async Task<Response<PagedResult<CommunityFeedItemDto>>> Handle(
        ListCommunityFeedQuery request, CancellationToken cancellationToken)
    {
        var page = System.Math.Max(1, request.Page);
        var pageSize = System.Math.Clamp(request.PageSize, 1, PaginationExtensions.MaxPageSize);
        var tagIds = request.TagIds ?? System.Array.Empty<System.Guid>();

        // ─── Redis fast-path: community-scoped Hot/Newest, no tag filter, no post-type filter.
        //     TopicId is handled by over-fetching a wider window and applying the filter inside
        //     FeedHydratorService. Pages beyond the window fall through to SQL.
        var canUseRedis = tagIds.Count == 0
            && request.CommunityId.HasValue
            && request.PostType is null
            && !request.AuthorId.HasValue
            && (request.Sort == PostFeedSort.Hot || request.Sort == PostFeedSort.Newest);

        if (canUseRedis)
        {
            var communityId = request.CommunityId!.Value;
            var hasTopic = request.TopicId.HasValue;

            // Over-fetch when topicId is active: a 5× window handles topics covering ≥20% of
            // the community. Capped at 500 to bound the SQL IN-clause size. Narrower topics
            // fall through to SQL for accurate pagination.
            var fetchPage = hasTopic ? 1 : page;
            var fetchSize = hasTopic ? System.Math.Min(page * pageSize * 5, 500) : pageSize;

            var ids = request.Sort == PostFeedSort.Hot
                ? (await _feedStore.GetHotPostsAsync(communityId, fetchPage, fetchSize, cancellationToken).ConfigureAwait(false)).ToList()
                : (await _feedStore.GetCommunityFeedAsync(communityId, fetchPage, fetchSize, cancellationToken).ConfigureAwait(false)).ToList();

            if (ids.Count > 0)
            {
                // Use the Redis sorted-set cardinality as the total so the pagination count is
                // consistent with the items source. Using SQL total here caused phantom pages:
                // deleted/unpublished posts stay in Redis until TTL, so HydrateAsync silently
                // drops stale IDs and each page appears shorter than pageSize while total stays high.
                var total = request.Sort == PostFeedSort.Hot
                    ? await _feedStore.GetHotLeaderboardCountAsync(communityId, cancellationToken).ConfigureAwait(false)
                    : await _feedStore.GetCommunityFeedCountAsync(communityId, cancellationToken).ConfigureAwait(false);

                var hydrated = await _hydratorService
                    .HydrateAsync(ids, request.UserId, request.TopicId, cancellationToken)
                    .ConfigureAwait(false);

                if (!hasTopic)
                {
                    return _msg.Ok(
                        new PagedResult<CommunityFeedItemDto>(hydrated, page, pageSize, (int)total),
                        "ITEMS_LISTED");
                }

                // topicId active: page the in-memory filtered result.
                var skip = (page - 1) * pageSize;
                if (skip < hydrated.Count)
                {
                    return _msg.Ok(
                        new PagedResult<CommunityFeedItemDto>(
                            hydrated.Skip(skip).Take(pageSize).ToList(), page, pageSize, (int)total),
                        "ITEMS_LISTED");
                }
                // Window exhausted for this page — fall through to SQL.
            }
            // Redis cold/unavailable or window exhausted — fall through to SQL.
        }

        // ─── SQL path: global, tag-filtered, top-voted, or Redis miss ───
        var communityFilter = request.CommunityId;
        var topicFilter = request.TopicId;
        var postTypeFilter = request.PostType;

        // JOIN replaces the correlated Communities.Any subquery that was evaluated once
        // per post row — a single hash-join is cheaper on any page size.
        var query = (
            from p in _db.Posts
            join c in _db.Communities on p.CommunityId equals c.Id
            where p.Status == PostStatus.Published
                && c.IsActive
                && c.Visibility == CommunityVisibility.Public
            select p)
            .WhereIf(communityFilter.HasValue, p => p.CommunityId == communityFilter!.Value)
            .WhereIf(topicFilter.HasValue, p => p.TopicId == topicFilter!.Value)
            .WhereIf(tagIds.Count > 0, p => p.Tags.Any(t => tagIds.Contains(t.Id)))
            .WhereIf(postTypeFilter.HasValue, p => p.Type == postTypeFilter!.Value)
            .WhereIf(request.AuthorId.HasValue, p => p.AuthorId == request.AuthorId!.Value);

        query = request.Sort switch
        {
            PostFeedSort.Newest => query
                .OrderByDescending(p => p.PublishedOn ?? p.CreatedOn)
                .ThenByDescending(p => p.Id),
            PostFeedSort.TopVoted => query
                .OrderByDescending(p => p.UpvoteCount)
                .ThenByDescending(p => p.Score),
            PostFeedSort.MostCommented => query
                .OrderByDescending(p => p.CommentsCount)
                .ThenByDescending(p => p.Score),
            _ => query.OrderByDescending(p => p.Score),
        };

        var pagedIds = await query
            .Select(p => p.Id)
            .ToPagedResultAsync(page, pageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = await _hydratorService
            .HydrateAsync(pagedIds.Items, request.UserId, null, cancellationToken)
            .ConfigureAwait(false);
        return _msg.Ok(
            new PagedResult<CommunityFeedItemDto>(items, page, pageSize, pagedIds.Total),
            "ITEMS_LISTED");
    }
}
