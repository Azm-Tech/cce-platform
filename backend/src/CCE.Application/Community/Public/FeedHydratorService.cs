using System.Collections.Generic;
using System.Linq;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Domain.Common;
using CCE.Domain.Community;

namespace CCE.Application.Community.Public;

/// <summary>
/// Shared post hydration for community feed queries. Given an ordered list of post IDs
/// (from Redis or SQL), loads and enriches each <see cref="CommunityFeedItemDto"/>.
/// The visibility guard (published + public active community) is re-applied so stale
/// Redis IDs drop out without leaking deleted or unpublished posts.
///
/// Query plan (5 round-trips instead of the original 8):
///   1. One JOIN query: posts + community visibility guard + author + topic + expert status.
///   2. Attachments (separate to avoid cartesian with the JOIN above).
///   3. Tags (separate, many-to-many).
///   4. Post follows batch (skipped when anonymous).
///   5. Post votes batch  (skipped when anonymous).
///   Redis batch is fired after step 1 and awaited after step 5 — runs concurrently
///   with steps 2-5 because it uses a different connection.
/// </summary>
public sealed class FeedHydratorService
{
    private readonly ICceDbContext _db;
    private readonly IRedisFeedStore _feedStore;
    private readonly ISystemClock _clock;

    public FeedHydratorService(ICceDbContext db, IRedisFeedStore feedStore, ISystemClock clock)
    {
        _db = db;
        _feedStore = feedStore;
        _clock = clock;
    }

    public async Task<IReadOnlyList<CommunityFeedItemDto>> HydrateAsync(
        IReadOnlyList<System.Guid> orderedIds,
        System.Guid? userId,
        System.Guid? topicFilter,
        CancellationToken ct)
    {
        if (orderedIds.Count == 0)
            return System.Array.Empty<CommunityFeedItemDto>();

        // ── Step 1: one JOIN query replacing four separate round-trips ──────────────────
        // Combines: posts, community visibility guard (JOIN not correlated ANY), author
        // names, topic names, and expert status (LEFT JOIN on expert_profiles).
        var enriched = await (
            from p in _db.Posts
            join c in _db.Communities on p.CommunityId equals c.Id
            join u in _db.Users on p.AuthorId equals u.Id
            join t in _db.Topics on p.TopicId equals t.Id
            join ep in _db.ExpertProfiles on u.Id equals ep.UserId into epGroup
            from ep in epGroup.DefaultIfEmpty()
            where orderedIds.Contains(p.Id)
                && p.Status == PostStatus.Published
                && c.IsActive
                && c.Visibility == CommunityVisibility.Public
                && (!topicFilter.HasValue || p.TopicId == topicFilter.Value)
            select new
            {
                p.Id, p.CommunityId, p.TopicId, p.AuthorId,
                AuthorFirst    = u.FirstName,
                AuthorLast     = u.LastName,
                AuthorUserName = u.UserName,
                p.Type, p.Title, p.Content, p.Locale,
                p.IsAnswerable, p.AnsweredReplyId,
                p.UpvoteCount, p.DownvoteCount, p.CommentsCount,
                p.PublishedOn, p.CreatedOn,
                TopicNameAr = t.NameAr,
                TopicNameEn = t.NameEn,
                IsExpert = ep != null,
            })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);

        if (enriched.Count == 0)
            return System.Array.Empty<CommunityFeedItemDto>();

        var postIds = enriched.Select(e => e.Id).ToList();

        // ── Step 2 (concurrent): Redis batch — different connection, runs alongside EF ──
        var hotMetaTask = _feedStore.GetPostsMetaBatchAsync(postIds, ct);

        // ── Step 3: Attachments ──────────────────────────────────────────────────────────
        var attachmentsByPost = (await _db.PostAttachments
            .Where(a => postIds.Contains(a.PostId))
            .Select(a => new { a.PostId, a.AssetFileId })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false))
            .GroupBy(a => a.PostId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<System.Guid>)g.Select(a => a.AssetFileId).ToList());

        // ── Step 4: Tags ─────────────────────────────────────────────────────────────────
        var tagsByPost = (await _db.Posts
            .Where(p => postIds.Contains(p.Id))
            .Select(p => new { p.Id, TagIds = p.Tags.Select(tag => tag.Id).ToList() })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false))
            .ToDictionary(x => x.Id, x => (IReadOnlyList<System.Guid>)x.TagIds);

        // ── Step 5: User-specific batch lookups (skipped when anonymous) ─────────────────
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

        // Collect Redis result (has been running concurrently since step 2).
        var hotMeta = await hotMetaTask.ConfigureAwait(false);

        // ── Step 6: Poll data (skipped when no Poll-type posts on this page) ────────────
        var pollPostIds  = enriched.Where(e => e.Type == PostType.Poll).Select(e => e.Id).ToList();
        var pollsByPostId = await PollHydrator.FetchAsync(_db, _clock, pollPostIds, userId, ct)
            .ConfigureAwait(false);

        // ── Map in original Redis-sorted order, dropping stale IDs ───────────────────────
        var byId  = enriched.ToDictionary(e => e.Id);
        var empty = (IReadOnlyList<System.Guid>)System.Array.Empty<System.Guid>();

        return orderedIds
            .Where(byId.ContainsKey)
            .Select(id =>
            {
                var e = byId[id];
                hotMeta.TryGetValue(e.Id, out var meta);

                var fullName   = $"{e.AuthorFirst} {e.AuthorLast}".Trim();
                var authorName = string.IsNullOrEmpty(fullName)
                    ? e.AuthorUserName ?? string.Empty
                    : fullName;

                return new CommunityFeedItemDto(
                    e.Id, e.CommunityId, e.TopicId, e.AuthorId,
                    authorName,
                    e.Type, e.Title, e.Content, e.Locale,
                    e.IsAnswerable, e.AnsweredReplyId,
                    meta?.Upvotes    ?? e.UpvoteCount,
                    meta?.Downvotes  ?? e.DownvoteCount,
                    meta?.ReplyCount ?? e.CommentsCount,
                    attachmentsByPost.GetValueOrDefault(e.Id, empty),
                    tagsByPost.GetValueOrDefault(e.Id, empty),
                    e.PublishedOn ?? e.CreatedOn,
                    e.TopicNameAr ?? string.Empty,
                    e.TopicNameEn ?? string.Empty,
                    e.IsExpert,
                    watchlistedPostIds.Contains(e.Id),
                    voteByPost.GetValueOrDefault(e.Id, 0),
                    pollsByPostId.GetValueOrDefault(e.Id));
            })
            .ToList();
    }
}
