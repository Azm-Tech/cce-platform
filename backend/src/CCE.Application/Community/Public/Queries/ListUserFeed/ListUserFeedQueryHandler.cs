using System.Collections.Generic;
using System.Linq;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using Microsoft.EntityFrameworkCore;
using CCE.Application.Community.Public.Queries.ListCommunityFeed;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListUserFeed;

/// <summary>
/// Handles <see cref="ListUserFeedQuery"/>. Two-path fanout-read strategy:
/// <list type="number">
///   <item>Read personal IDs+timestamps from <c>feed:user:{userId}</c> (Redis), merge
///     expert/celebrity posts from SQL, optionally filter the merged ID pool in SQL
///     for communityId/topicId/postType, page, then hydrate only the returned page.
///     Requires Newest sort and no tag filters.</item>
///   <item>Fall back to a pure SQL query when Redis is cold, tag filters are active,
///     or sort is not Newest.</item>
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

        // ─── Redis fast-path: personal feed (fanout-read) ───
        // FeedConsumer fans out ALL followed entities (user/community/topic) into
        // feed:user:{userId}, so this key is the canonical personal feed store.
        // Condition: Newest sort (Redis is time-ordered) + no tag filters (tags need SQL JOIN).
        // communityId / topicId / postType are handled by filtering the merged ID pool in SQL.
        var canUsePersonalRedis = tagIds.Count == 0 && request.Sort == PostFeedSort.Newest;

        if (canUsePersonalRedis)
        {
            var hasEntityFilter = request.CommunityId.HasValue
                || request.TopicId.HasValue
                || request.PostType.HasValue;

            // Over-fetch a 5× window when entity filters are active so that after SQL filtering
            // the requested page is reachable. Capped at 2 000 to bound the IN-clause size.
            var redisLimit = hasEntityFilter
                ? System.Math.Min(page * pageSize * 5, 2_000)
                : System.Math.Min(page * pageSize + pageSize, 2_000);

            var personalEntries = await _feedStore
                .GetUserFeedWithScoresAsync(userId, redisLimit, cancellationToken)
                .ConfigureAwait(false);

            // Fix B: pre-load expert user IDs via JOIN rather than a correlated Any() per post row.
            var expertUserIds = new System.Collections.Generic.List<System.Guid>();
            if (followedCommunityIds.Count > 0 || followedUserIds.Count > 0)
            {
                expertUserIds = await (
                    from ep in _db.ExpertProfiles
                    join p in _db.Posts on ep.UserId equals p.AuthorId
                    where p.Status == PostStatus.Published
                        && (followedCommunityIds.Contains(p.CommunityId)
                            || followedUserIds.Contains(p.AuthorId))
                    select ep.UserId)
                    .Distinct()
                    .ToListAsyncEither(cancellationToken)
                    .ConfigureAwait(false);
            }

            var expertEntries = new System.Collections.Generic.List<(System.Guid Id, System.DateTimeOffset Date)>();
            if (expertUserIds.Count > 0)
            {
                expertEntries = (await _db.Posts
                    .Where(p => p.Status == PostStatus.Published
                        && (followedCommunityIds.Contains(p.CommunityId)
                            || followedUserIds.Contains(p.AuthorId))
                        && expertUserIds.Contains(p.AuthorId))
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
                // Deduplicate: a post in both personal Redis and expert SQL keeps its Redis timestamp.
                var personalSet = personalEntries.ToDictionary(e => e.PostId, e => e.PublishedOn);
                var merged = personalEntries
                    .Select(e => (e.PostId, e.PublishedOn))
                    .Concat(expertEntries
                        .Where(e => !personalSet.ContainsKey(e.Id))
                        .Select(e => (PostId: e.Id, PublishedOn: e.Date)))
                    .OrderByDescending(e => e.PublishedOn)
                    .ToList();

                long total;
                System.Collections.Generic.List<(System.Guid PostId, System.DateTimeOffset PublishedOn)> filteredMerged;

                if (hasEntityFilter)
                {
                    // Filter the merged ID pool in SQL — one cheap round-trip, no cartesian product.
                    // Preserves Newest ordering by retaining the merged list's sort after intersection.
                    var mergedIds = merged.Select(e => e.PostId).ToList();
                    var filteredIds = await (
                        from p in _db.Posts
                        join c in _db.Communities on p.CommunityId equals c.Id
                        where mergedIds.Contains(p.Id)
                            && p.Status == PostStatus.Published
                            && c.IsActive && c.Visibility == CommunityVisibility.Public
                        select p)
                        .WhereIf(request.CommunityId.HasValue, p => p.CommunityId == request.CommunityId!.Value)
                        .WhereIf(request.TopicId.HasValue,     p => p.TopicId     == request.TopicId!.Value)
                        .WhereIf(request.PostType.HasValue,    p => p.Type        == request.PostType!.Value)
                        .Select(p => p.Id)
                        .ToListAsyncEither(cancellationToken)
                        .ConfigureAwait(false);

                    var filteredSet = filteredIds.ToHashSet();
                    filteredMerged = merged.Where(e => filteredSet.Contains(e.PostId)).ToList();
                    total = filteredMerged.Count;
                }
                else
                {
                    filteredMerged = merged;
                    // Fix C: expert posts are not fanned into personal Redis — sets are disjoint
                    // by design, so true total is redisTotal + full expert post count.
                    var redisTotal = await _feedStore
                        .GetUserFeedCountAsync(userId, cancellationToken)
                        .ConfigureAwait(false);
                    var expertTotal = expertUserIds.Count == 0 ? 0L
                        : await _db.Posts
                            .Where(p => p.Status == PostStatus.Published
                                && (followedCommunityIds.Contains(p.CommunityId)
                                    || followedUserIds.Contains(p.AuthorId))
                                && expertUserIds.Contains(p.AuthorId))
                            .LongCountAsync(cancellationToken)
                            .ConfigureAwait(false);
                    total = redisTotal + expertTotal;
                }

                var pagedIds = filteredMerged
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => e.PostId)
                    .ToList();

                if (pagedIds.Count > 0)
                {
                    var hydrated = await _hydratorService
                        .HydrateAsync(pagedIds, userId, null, cancellationToken)
                        .ConfigureAwait(false);
                    return _msg.Ok(
                        new PagedResult<CommunityFeedItemDto>(hydrated, page, pageSize, total),
                        MessageKeys.General.ITEMS_LISTED);
                }
            }
        }

        // ─── SQL fallback: Redis cold, tag filters active, or non-Newest sort ───
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
                MessageKeys.General.ITEMS_LISTED);
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
                MessageKeys.General.ITEMS_LISTED);
        }

        // Fix E: use JOIN instead of correlated Communities.Any() per post row.
        var query = (
            from p in _db.Posts
            join c in _db.Communities on p.CommunityId equals c.Id
            where p.Status == PostStatus.Published
                && c.IsActive
                && c.Visibility == CommunityVisibility.Public
                && (followedCommunityIds.Contains(p.CommunityId)
                    || followedTopicIds.Contains(p.TopicId)
                    || followedUserIds.Contains(p.AuthorId))
            select p)
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
            MessageKeys.General.ITEMS_LISTED);
    }
}
