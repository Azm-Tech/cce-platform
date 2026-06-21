using System.Collections.Generic;
using System.Linq;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Community.Public.Queries.ListCommunityFeed;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListUserFeed;

/// <summary>
/// Handles <see cref="ListUserFeedQuery"/>. Hybrid fan-out read strategy:
/// <list type="number">
///   <item>When <c>communityId</c> is specified and the user follows that community, serve from
///     community-level Redis keys (<c>feed:community:{communityId}</c> / <c>hot:{communityId}</c>).
///     A <c>topicId</c> filter is applied by over-fetching a wider window from Redis and letting
///     <see cref="FeedHydratorService"/> filter in SQL — no Redis schema change required.</item>
///   <item>Otherwise, read personal IDs+timestamps from <c>feed:user:{userId}</c> (Redis), merge
///     expert/celebrity posts from SQL, page the merged ID list first, then hydrate only the
///     returned page — avoiding the cost of hydrating the entire window.</item>
///   <item>Fall back to a pure SQL query when Redis is cold, filters exceed Redis coverage, or the
///     community is not followed.</item>
/// </list>
/// </summary>
public sealed class ListUserFeedQueryHandler
    : IRequestHandler<ListUserFeedQuery, Response<PagedResult<CommunityFeedItemDto>>>
{
    private readonly ICceDbContext _db;
    private readonly IRedisFeedStore _feedStore;
    private readonly MessageFactory _msg;
    private readonly FeedHydratorService _hydratorService;

    public ListUserFeedQueryHandler(
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
        ListUserFeedQuery request, CancellationToken cancellationToken)
    {
        var page = System.Math.Max(1, request.Page);
        var pageSize = System.Math.Clamp(request.PageSize, 1, PaginationExtensions.MaxPageSize);
        var userId = request.UserId;
        var tagIds = request.TagIds ?? System.Array.Empty<System.Guid>();

        // Sequential — EF Core DbContext is not thread-safe.
        var followedCommunityIds = await _db.CommunityFollows
            .Where(f => f.UserId == userId)
            .Select(f => f.CommunityId)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var followedUserIds = await _db.UserFollows
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FollowedId)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        // ─── Redis fast-path: community-scoped — reuse community Redis keys when the user
        //     follows the community. TopicId is handled by over-fetching and filtering inside
        //     FeedHydratorService so no Redis key layout change is needed.
        var canUseCommunityRedis = tagIds.Count == 0
            && request.CommunityId.HasValue
            && followedCommunityIds.Contains(request.CommunityId.Value)
            && request.PostType is null
            && (request.Sort == PostFeedSort.Hot || request.Sort == PostFeedSort.Newest);

        if (canUseCommunityRedis)
        {
            var communityId = request.CommunityId!.Value;
            var hasTopic = request.TopicId.HasValue;

            // Over-fetch when topicId is active: a 5× window handles topics that cover ≥20% of
            // the community's posts. Capped at 500 to bound the SQL IN-clause size. Topics
            // narrower than that fall through to SQL for accurate pagination.
            var fetchPage = hasTopic ? 1 : page;
            var fetchSize = hasTopic ? System.Math.Min(page * pageSize * 5, 500) : pageSize;

            var ids = request.Sort == PostFeedSort.Hot
                ? (await _feedStore.GetHotPostsAsync(communityId, fetchPage, fetchSize, cancellationToken).ConfigureAwait(false)).ToList()
                : (await _feedStore.GetCommunityFeedAsync(communityId, fetchPage, fetchSize, cancellationToken).ConfigureAwait(false)).ToList();

            if (ids.Count > 0)
            {
                var total = request.Sort == PostFeedSort.Hot
                    ? await _feedStore.GetHotLeaderboardCountAsync(communityId, cancellationToken).ConfigureAwait(false)
                    : await _feedStore.GetCommunityFeedCountAsync(communityId, cancellationToken).ConfigureAwait(false);

                var hydrated = await _hydratorService
                    .HydrateAsync(ids, userId, request.TopicId, cancellationToken)
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
            // Redis cold or window exhausted — fall through to SQL.
        }

        // ─── Redis fast-path: personal feed (no filters, Newest sort) ───
        var canUsePersonalRedis = tagIds.Count == 0
            && request.CommunityId is null
            && request.TopicId is null
            && request.PostType is null
            && request.Sort == PostFeedSort.Newest;

        if (canUsePersonalRedis)
        {
            // Fetch IDs+timestamps (Redis scores = publishedOn Unix time) so the ID list can be
            // paged before hydrating, avoiding loading more post rows than the page requires.
            var redisLimit = System.Math.Min(page * pageSize + pageSize, 1_000);
            var personalEntries = await _feedStore
                .GetUserFeedWithScoresAsync(userId, redisLimit, cancellationToken)
                .ConfigureAwait(false);

            var expertEntries = new System.Collections.Generic.List<(System.Guid Id, System.DateTimeOffset Date)>();
            if (followedCommunityIds.Count > 0 || followedUserIds.Count > 0)
            {
                expertEntries = (await _db.Posts
                    .Where(p => p.Status == PostStatus.Published
                        && (followedCommunityIds.Contains(p.CommunityId)
                            || followedUserIds.Contains(p.AuthorId))
                        && _db.ExpertProfiles.Any(e => e.UserId == p.AuthorId))
                    .OrderByDescending(p => p.PublishedOn ?? p.CreatedOn)
                    .Take(pageSize * 3)
                    .Select(p => new { p.Id, Date = p.PublishedOn ?? p.CreatedOn })
                    .ToListAsyncEither(cancellationToken)
                    .ConfigureAwait(false))
                    .Select(x => (x.Id, x.Date))
                    .ToList();
            }

            if (personalEntries.Count > 0 || expertEntries.Count > 0)
            {
                // Deduplicate: a post that appears in both the personal Redis feed and the expert
                // SQL result keeps its Redis timestamp (already the canonical publish time).
                var personalSet = personalEntries.ToDictionary(e => e.PostId, e => e.PublishedOn);
                var merged = personalEntries
                    .Select(e => (e.PostId, e.PublishedOn))
                    .Concat(expertEntries
                        .Where(e => !personalSet.ContainsKey(e.Id))
                        .Select(e => (PostId: e.Id, PublishedOn: e.Date)))
                    .OrderByDescending(e => e.PublishedOn)
                    .ToList();

                var pagedIds = merged
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => e.PostId)
                    .ToList();

                if (pagedIds.Count > 0)
                {
                    var hydrated = await _hydratorService
                        .HydrateAsync(pagedIds, userId, null, cancellationToken)
                        .ConfigureAwait(false);
                    var redisTotal = await _feedStore
                        .GetUserFeedCountAsync(userId, cancellationToken)
                        .ConfigureAwait(false);
                    var total = (int)System.Math.Max(redisTotal, merged.Count);
                    return _msg.Ok(
                        new PagedResult<CommunityFeedItemDto>(hydrated, page, pageSize, total),
                        "ITEMS_LISTED");
                }
            }
        }

        // ─── SQL fallback: Redis cold, filters require SQL, or community not followed ───
        return await FallbackSqlAsync(
            userId, followedCommunityIds, followedUserIds,
            tagIds, request.CommunityId, request.TopicId, request.PostType, request.Sort,
            page, pageSize, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<Response<PagedResult<CommunityFeedItemDto>>> FallbackSqlAsync(
        System.Guid userId,
        System.Collections.Generic.List<System.Guid> followedCommunityIds,
        System.Collections.Generic.List<System.Guid> followedUserIds,
        System.Collections.Generic.IReadOnlyList<System.Guid> tagIds,
        System.Guid? communityFilter,
        System.Guid? topicFilter,
        CCE.Domain.Community.PostType? postTypeFilter,
        PostFeedSort sort,
        int page, int pageSize, CancellationToken ct)
    {
        var followedTopicIds = await _db.TopicFollows
            .Where(f => f.UserId == userId)
            .Select(f => f.TopicId)
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);

        if (followedCommunityIds.Count == 0 && followedTopicIds.Count == 0 && followedUserIds.Count == 0)
        {
            return _msg.Ok(
                new PagedResult<CommunityFeedItemDto>(
                    System.Array.Empty<CommunityFeedItemDto>(), page, pageSize, 0),
                "ITEMS_LISTED");
        }

        // The WHERE clause is (followedCommunity OR followedTopic OR followedUser) AND community = X.
        // If X is not followed and the user has no other follow-graph path into it, no post can match.
        if (communityFilter.HasValue
            && !followedCommunityIds.Contains(communityFilter.Value)
            && followedUserIds.Count == 0
            && followedTopicIds.Count == 0)
        {
            return _msg.Ok(
                new PagedResult<CommunityFeedItemDto>(
                    System.Array.Empty<CommunityFeedItemDto>(), page, pageSize, 0),
                "ITEMS_LISTED");
        }

        var query = _db.Posts
            .Where(p => p.Status == PostStatus.Published)
            .Where(p => _db.Communities.Any(c =>
                c.Id == p.CommunityId && c.IsActive && c.Visibility == CommunityVisibility.Public))
            .Where(p => followedCommunityIds.Contains(p.CommunityId)
                || followedTopicIds.Contains(p.TopicId)
                || followedUserIds.Contains(p.AuthorId))
            .WhereIf(tagIds is { Count: > 0 }, p => p.Tags.Any(t => tagIds.Contains(t.Id)))
            .WhereIf(communityFilter.HasValue, p => p.CommunityId == communityFilter!.Value)
            .WhereIf(topicFilter.HasValue, p => p.TopicId == topicFilter!.Value)
            .WhereIf(postTypeFilter.HasValue, p => p.Type == postTypeFilter!.Value);

        query = sort switch
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
            .ToPagedResultAsync(page, pageSize, ct)
            .ConfigureAwait(false);

        var items = await _hydratorService
            .HydrateAsync(pagedIds.Items, userId, null, ct)
            .ConfigureAwait(false);
        return _msg.Ok(
            new PagedResult<CommunityFeedItemDto>(items, page, pageSize, pagedIds.Total),
            "ITEMS_LISTED");
    }
}
