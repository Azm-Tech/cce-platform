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
/// and from SQL for global, tag-filtered, top-voted, or Redis-miss queries. SQL is always the
/// source of truth for the hydrated post data and the visibility guard.
/// </summary>
public sealed class ListCommunityFeedQueryHandler
    : IRequestHandler<ListCommunityFeedQuery, Response<PagedResult<CommunityFeedItemDto>>>
{
    private readonly ICceDbContext _db;
    private readonly IRedisFeedStore _feedStore;
    private readonly MessageFactory _msg;

    public ListCommunityFeedQueryHandler(ICceDbContext db, IRedisFeedStore feedStore, MessageFactory msg)
    {
        _db = db;
        _feedStore = feedStore;
        _msg = msg;
    }

    public async Task<Response<PagedResult<CommunityFeedItemDto>>> Handle(
        ListCommunityFeedQuery request, CancellationToken cancellationToken)
    {
        var page = System.Math.Max(1, request.Page);
        var pageSize = System.Math.Clamp(request.PageSize, 1, PaginationExtensions.MaxPageSize);
        var tagIds = request.TagIds ?? System.Array.Empty<System.Guid>();

        // ─── Redis fast-path: community-scoped Hot/Newest, no tag filter, no post-type filter ───
        var canUseRedis = tagIds.Count == 0
            && request.CommunityId.HasValue
            && request.PostType is null
            && (request.Sort == PostFeedSort.Hot || request.Sort == PostFeedSort.Newest);

        if (canUseRedis)
        {
            var communityId = request.CommunityId!.Value;
            var ids = request.Sort == PostFeedSort.Hot
                ? (await _feedStore.GetHotPostsAsync(communityId, page * pageSize, cancellationToken).ConfigureAwait(false))
                    .Skip((page - 1) * pageSize).Take(pageSize).ToList()
                : (await _feedStore.GetCommunityFeedAsync(communityId, page, pageSize, cancellationToken).ConfigureAwait(false))
                    .ToList();

            if (ids.Count > 0)
            {
                var total = await _db.Posts
                    .Where(p => p.CommunityId == communityId && p.Status == PostStatus.Published)
                    .CountAsyncEither(cancellationToken).ConfigureAwait(false);
                var hydrated = await HydrateAsync(ids, request.UserId, cancellationToken).ConfigureAwait(false);
                return _msg.Ok(
                    new PagedResult<CommunityFeedItemDto>(hydrated, page, pageSize, total),
                    "ITEMS_LISTED");
            }
            // Redis cold/unavailable — fall through to SQL.
        }

        // ─── SQL path: global, tag-filtered, top-voted, or Redis miss ───
        var communityFilter = request.CommunityId;
        var topicFilter = request.TopicId;

        var postTypeFilter = request.PostType;

        var query = _db.Posts
            .Where(p => p.Status == PostStatus.Published)
            .Where(p => _db.Communities.Any(c =>
                c.Id == p.CommunityId && c.IsActive && c.Visibility == CommunityVisibility.Public))
            .WhereIf(communityFilter.HasValue, p => p.CommunityId == communityFilter!.Value)
            .WhereIf(topicFilter.HasValue, p => p.TopicId == topicFilter!.Value)
            .WhereIf(tagIds.Count > 0, p => p.Tags.Any(t => tagIds.Contains(t.Id)))
            .WhereIf(postTypeFilter.HasValue, p => p.Type == postTypeFilter!.Value);

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

        var items = await HydrateAsync(pagedIds.Items, request.UserId, cancellationToken).ConfigureAwait(false);
        return _msg.Ok(
            new PagedResult<CommunityFeedItemDto>(items, page, pageSize, pagedIds.Total),
            "ITEMS_LISTED");
    }

    /// <summary>
    /// Loads the posts for <paramref name="orderedIds"/> (preserving that order), re-applying the
    /// published + public-and-active-community guard so stale/private/deleted IDs from Redis drop
    /// out, then batch-enriches author names, attachment IDs, and tag IDs.
    /// </summary>
    private async Task<IReadOnlyList<CommunityFeedItemDto>> HydrateAsync(
        IReadOnlyList<System.Guid> orderedIds, System.Guid? userId, CancellationToken ct)
    {
        if (orderedIds.Count == 0)
        {
            return System.Array.Empty<CommunityFeedItemDto>();
        }

        var posts = await _db.Posts
            .Where(p => orderedIds.Contains(p.Id) && p.Status == PostStatus.Published)
            .Where(p => _db.Communities.Any(c =>
                c.Id == p.CommunityId && c.IsActive && c.Visibility == CommunityVisibility.Public))
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);

        if (posts.Count == 0)
        {
            return System.Array.Empty<CommunityFeedItemDto>();
        }

        var postIds = posts.Select(p => p.Id).ToList();
        var authorIds = posts.Select(p => p.AuthorId).Distinct().ToList();

        var authorNames = (await _db.Users
            .Where(u => authorIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false))
            .ToDictionary(a => a.Id, a => a.Name);

        var attachmentsByPost = (await _db.PostAttachments
            .Where(a => postIds.Contains(a.PostId))
            .Select(a => new { a.PostId, a.AssetFileId })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false))
            .GroupBy(a => a.PostId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<System.Guid>)g.Select(a => a.AssetFileId).ToList());

        var tagsByPost = (await _db.Posts
            .Where(p => postIds.Contains(p.Id))
            .Select(p => new { p.Id, TagIds = p.Tags.Select(t => t.Id).ToList() })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false))
            .ToDictionary(x => x.Id, x => (IReadOnlyList<System.Guid>)x.TagIds);

        var topicIds = posts.Select(p => p.TopicId).Distinct().ToList();
        var topicNames = (await _db.Topics
            .Where(t => topicIds.Contains(t.Id))
            .Select(t => new { t.Id, t.NameAr, t.NameEn })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false))
            .ToDictionary(t => t.Id, t => (t.NameAr, t.NameEn));

        var expertAuthorIds = new System.Collections.Generic.HashSet<System.Guid>(
            await _db.ExpertProfiles
                .Where(e => authorIds.Contains(e.UserId))
                .Select(e => e.UserId)
                .ToListAsyncEither(ct)
                .ConfigureAwait(false));

        var watchlistedPostIds = new System.Collections.Generic.HashSet<System.Guid>();
        if (userId.HasValue)
        {
            watchlistedPostIds = new System.Collections.Generic.HashSet<System.Guid>(
                await _db.PostFollows
                    .Where(pf => postIds.Contains(pf.PostId) && pf.UserId == userId.Value)
                    .Select(pf => pf.PostId)
                    .ToListAsyncEither(ct)
                    .ConfigureAwait(false));
        }

        var voteByPost = new System.Collections.Generic.Dictionary<System.Guid, int>();
        if (userId.HasValue)
        {
            voteByPost = (await _db.PostVotes
                .Where(pv => postIds.Contains(pv.PostId) && pv.UserId == userId.Value)
                .Select(pv => new { pv.PostId, pv.Value })
                .ToListAsyncEither(ct)
                .ConfigureAwait(false))
                .ToDictionary(v => v.PostId, v => v.Value);
        }

        var byId = posts.ToDictionary(p => p.Id);
        var empty = (IReadOnlyList<System.Guid>)System.Array.Empty<System.Guid>();

        return orderedIds
            .Where(byId.ContainsKey)
            .Select(id => byId[id])
            .Select(p => new CommunityFeedItemDto(
                p.Id,
                p.CommunityId,
                p.TopicId,
                p.AuthorId,
                authorNames.GetValueOrDefault(p.AuthorId),
                p.Type,
                p.Title,
                p.Content,
                p.Locale,
                p.IsAnswerable,
                p.AnsweredReplyId,
                p.UpvoteCount,
                p.DownvoteCount,
                p.CommentsCount,
                attachmentsByPost.GetValueOrDefault(p.Id, empty),
                tagsByPost.GetValueOrDefault(p.Id, empty),
                p.CreatedOn,
                topicNames.GetValueOrDefault(p.TopicId).NameAr ?? string.Empty,
                topicNames.GetValueOrDefault(p.TopicId).NameEn ?? string.Empty,
                expertAuthorIds.Contains(p.AuthorId),
                watchlistedPostIds.Contains(p.Id),
                voteByPost.GetValueOrDefault(p.Id, 0)))
            .ToList();
    }
}
