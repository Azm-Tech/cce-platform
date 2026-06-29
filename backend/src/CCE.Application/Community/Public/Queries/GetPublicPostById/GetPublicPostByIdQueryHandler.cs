using System.Collections.Generic;
using System.Linq;
using CCE.Application.Common;
using CCE.Application.Community;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content;
using CCE.Application.Messages;

using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Community.Public.Queries.GetPublicPostById;

public sealed class GetPublicPostByIdQueryHandler
    : IRequestHandler<GetPublicPostByIdQuery, Response<PostDetailDto>>
{
    private readonly ICceDbContext _db;
    private readonly IRedisFeedStore _feedStore;
    private readonly IFileStorage _storage;
    private readonly MessageFactory _msg;
    private readonly ISystemClock _clock;

    public GetPublicPostByIdQueryHandler(ICceDbContext db, IRedisFeedStore feedStore, IFileStorage storage, MessageFactory msg, ISystemClock clock)
    {
        _db = db;
        _feedStore = feedStore;
        _storage = storage;
        _msg = msg;
        _clock = clock;
    }

    public async Task<Response<PostDetailDto>> Handle(
        GetPublicPostByIdQuery request,
        CancellationToken cancellationToken)
    {
        var userId = request.UserId;

        // Single JOIN query — post + author + topic + expert status (LEFT JOIN).
        // The original had three correlated subqueries embedded in the SELECT projection
        // (ExpertProfiles.Any, PostFollows.Any, PostVotes.FirstOrDefault); this replaces them
        // with one clean SQL statement and two tiny indexed lookups below.
        var raw = await (
            from p in _db.Posts.AsNoTracking()
            join u in _db.Users.AsNoTracking() on p.AuthorId equals u.Id
            join t in _db.Topics.AsNoTracking() on p.TopicId equals t.Id
            join ep in _db.ExpertProfiles.AsNoTracking() on u.Id equals ep.UserId into epGroup
            from ep in epGroup.DefaultIfEmpty()
            where p.Id == request.Id && p.Status == PostStatus.Published
            select new
            {
                p.Id, p.CommunityId, p.TopicId,
                AuthorId      = u.Id,
                AuthorFirst   = u.FirstName,
                AuthorLast    = u.LastName,
                u.AvatarUrl, u.PostsCount, u.FollowerCount,
                p.Type, p.Title, p.Content, p.Locale,
                p.IsAnswerable, p.AnsweredReplyId,
                p.UpvoteCount, p.DownvoteCount, p.CommentsCount,
                p.CreatedOn,
                TopicNameAr = t.NameAr,
                TopicNameEn = t.NameEn,
                IsExpert = ep != null,
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (raw is null)
            return _msg.NotFound<PostDetailDto>(MessageKeys.Community.POST_NOT_FOUND);

        // Attachments — JOIN MediaFiles for full media metadata; separate query avoids cartesian with the JOIN above.
        var attachmentRows = await (
            from a in _db.PostAttachments.AsNoTracking()
            join mf in _db.MediaFiles.AsNoTracking() on a.MediaFileId equals mf.Id
            where a.PostId == raw.Id
            orderby a.SortOrder
            select new { a.MediaFileId, a.Kind, mf.MimeType, mf.Url,
                         mf.SizeBytes, mf.OriginalFileName, a.SortOrder, a.MetadataJson })
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var media = attachmentRows.Select(r => new PostMediaItemDto(
            r.MediaFileId, r.Kind, r.MimeType,
            _storage.GetPublicUrl(r.Url).ToString(),
            r.SizeBytes, r.OriginalFileName, r.SortOrder, r.MetadataJson)).ToList();

        // Redis meta — fired before the user-specific EF queries so it runs concurrently.
        var metaTask = _feedStore.GetPostMetaAsync(raw.Id, cancellationToken);

        // User-specific point lookups — three tiny indexed queries, sequential (same DbContext).
        var isFollowing = userId.HasValue
            && await _db.PostFollows.AsNoTracking()
                .AnyAsync(pf => pf.PostId == raw.Id && pf.UserId == userId.Value, cancellationToken)
                .ConfigureAwait(false);

        var isAuthorFollowed = userId.HasValue
            && await _db.UserFollows.AsNoTracking()
                .AnyAsync(uf => uf.FollowerId == userId.Value && uf.FollowedId == raw.AuthorId, cancellationToken)
                .ConfigureAwait(false);

        var vote = userId.HasValue
            ? await _db.PostVotes.AsNoTracking()
                .Where(pv => pv.PostId == raw.Id && pv.UserId == userId.Value)
                .Select(pv => (int?)pv.Value)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false) ?? 0
            : 0;

        var meta = await metaTask.ConfigureAwait(false);

        // Poll data — only fetched for Poll-type posts.
        var pollsByPost = raw.Type == PostType.Poll
            ? await PollHydrator.FetchAsync(_db, _clock, new[] { raw.Id }, userId, cancellationToken)
                .ConfigureAwait(false)
            : new System.Collections.Generic.Dictionary<System.Guid, PollSummaryDto>();
        var pollSummary = pollsByPost.GetValueOrDefault(raw.Id);

        var authorName = $"{raw.AuthorFirst} {raw.AuthorLast}".Trim();
        var dto = new PostDetailDto(
            raw.Id, raw.CommunityId, raw.TopicId,
            new PostAuthorDto(raw.AuthorId, authorName, raw.AvatarUrl, raw.IsExpert,
                raw.PostsCount, raw.FollowerCount, isAuthorFollowed),
            raw.Type, raw.Title, raw.Content, raw.Locale,
            raw.IsAnswerable, raw.AnsweredReplyId,
            meta?.Upvotes   ?? raw.UpvoteCount,
            meta?.Downvotes ?? raw.DownvoteCount,
            meta?.ReplyCount ?? raw.CommentsCount,
            media,
            raw.CreatedOn,
            raw.TopicNameAr ?? string.Empty,
            raw.TopicNameEn ?? string.Empty,
            isFollowing,
            vote,
            pollSummary);

        return _msg.Ok(dto, MessageKeys.General.SUCCESS_OPERATION);
    }
}
