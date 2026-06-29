using System.Collections.Generic;
using System.Linq;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Content;
using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.Application.Community;

namespace CCE.Application.Community.Public;

/// <summary>
/// Shared post hydration for community feed queries. Given an ordered list of post IDs
/// (from Redis or SQL), loads and enriches each <see cref="CommunityFeedItemDto"/>.
/// The visibility guard (published + public active community) is re-applied so stale
/// Redis IDs drop out without leaking deleted or unpublished posts.
///
/// Query plan (5 round-trips, 6 when the caller is authenticated):
///   1. One JOIN query: posts + community visibility guard + author + topic + expert status.
///   2. Attachments JOIN AssetFiles (separate to avoid cartesian with step 1).
///   3. Tags via SelectMany on the many-to-many join (separate for the same reason).
///   4. Post follows + votes batch (one block, skipped when anonymous).
///   5. Poll data (skipped when no Poll-type posts on the page).
///   Redis meta is fired after step 1 and awaited just before the final map. It runs on
///   its own connection so it makes progress during the EF await gaps — partial overlap,
///   not true parallelism (DbContext is single-threaded). The net gain is that the Redis
///   RTT is mostly hidden behind steps 2-5 rather than being a serial addition.
/// </summary>
public sealed class FeedHydratorService
{
    private readonly ICceDbContext _db;
    private readonly IRedisFeedStore _feedStore;
    private readonly ISystemClock _clock;
    private readonly IFileStorage _storage;

    public FeedHydratorService(ICceDbContext db, IRedisFeedStore feedStore, ISystemClock clock, IFileStorage storage)
    {
        _db = db;
        _feedStore = feedStore;
        _clock = clock;
        _storage = storage;
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

        // ── Step 2: Attachments — JOIN AssetFiles to get Url+MimeType for main-image resolution
        var attachmentRows = await (
            from a in _db.PostAttachments
            join af in _db.AssetFiles on a.AssetFileId equals af.Id
            where postIds.Contains(a.PostId)
            select new { a.PostId, a.AssetFileId, a.Kind, a.SortOrder, af.Url, af.MimeType }
        ).ToListAsyncEither(ct).ConfigureAwait(false);

        var attachmentsByPost = attachmentRows
            .GroupBy(a => a.PostId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<System.Guid>)g.OrderBy(a => a.SortOrder).Select(a => a.AssetFileId).ToList());

        // First image per post (lowest SortOrder, Kind=Media, image MIME type).
        // GetPublicUrl is pure string concatenation (CDN base + key) — no I/O, safe to call per-item.
        var mainImageByPost = attachmentRows
            .Where(a => a.Kind == AttachmentKind.Media
                     && PostAttachmentPolicy.ImageMimeTypes.Contains(a.MimeType))
            .GroupBy(a => a.PostId)
            .ToDictionary(g => g.Key,
                g => _storage.GetPublicUrl(g.OrderBy(a => a.SortOrder).First().Url).ToString());

        // ── Step 3: Tags — SelectMany through the M2M join; avoids a ToList() inside an EF projection
        var tagsByPost = (await _db.Posts
            .Where(p => postIds.Contains(p.Id))
            .SelectMany(p => p.Tags, (p, t) => new { PostId = p.Id, TagId = t.Id })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false))
            .GroupBy(x => x.PostId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<System.Guid>)g.Select(x => x.TagId).ToList());

        // ── Step 4: User-specific batch lookups (one block, skipped when anonymous) ───────
        var watchlistedPostIds = new System.Collections.Generic.HashSet<System.Guid>();
        var voteByPost = new System.Collections.Generic.Dictionary<System.Guid, int>();
        if (userId.HasValue)
        {
            watchlistedPostIds = new System.Collections.Generic.HashSet<System.Guid>(
                await _db.PostFollows
                    .Where(pf => postIds.Contains(pf.PostId) && pf.UserId == userId.Value)
                    .Select(pf => pf.PostId)
                    .ToListAsyncEither(ct)
                    .ConfigureAwait(false));

            voteByPost = (await _db.PostVotes
                .Where(pv => postIds.Contains(pv.PostId) && pv.UserId == userId.Value)
                .Select(pv => new { pv.PostId, pv.Value })
                .ToListAsyncEither(ct)
                .ConfigureAwait(false))
                .ToDictionary(v => v.PostId, v => v.Value);
        }

        // Collect Redis result (has been running concurrently since step 2).
        var hotMeta = await hotMetaTask.ConfigureAwait(false);

        // ── Step 5: Poll data (skipped when no Poll-type posts on this page) ────────────
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
                    pollsByPostId.GetValueOrDefault(e.Id),
                    MainImageUrl: mainImageByPost.GetValueOrDefault(e.Id));
            })
            .ToList();
    }
}
