using System.Collections.Generic;
using System.Linq;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;

using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Community.Public.Queries.GetPostActivity;

/// <summary>
/// Fetches the current counters, replies since the client's <c>Since</c> cursor, and a poll
/// snapshot for a single post. Used by clients on <c>onreconnected</c> after a SignalR drop
/// — avoids per-event refetches while the socket was down.
/// </summary>
public sealed class GetPostActivityQueryHandler
    : IRequestHandler<GetPostActivityQuery, Response<PostActivityDto>>
{
    private readonly ICceDbContext _db;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public GetPostActivityQueryHandler(ICceDbContext db, ISystemClock clock, MessageFactory msg)
    {
        _db = db;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<PostActivityDto>> Handle(
        GetPostActivityQuery request,
        CancellationToken cancellationToken)
    {
        // PK lookup of the post — counter source of truth (denormalized).
        var post = await _db.Posts.AsNoTracking()
            .Where(p => p.Id == request.PostId && p.Status == PostStatus.Published)
            .Select(p => new { p.Id, p.Type, p.UpvoteCount, p.DownvoteCount, p.Score, p.CommentsCount })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (post is null)
            return _msg.NotFound<PostActivityDto>(MessageKeys.Community.POST_NOT_FOUND);

        // Replies created since the cursor — fetch the full nodes so mobile can render them
        // without a follow-up GET. Performance note: posts have hot index on (PostId, AuthorId)
        // and PostReply.CreatedOn is also indexed for the time range scan.
        var newReplies = await (
            from r in _db.PostReplies.AsNoTracking()
            where r.PostId == request.PostId && r.CreatedOn > request.Since
            orderby r.CreatedOn ascending
            join u in _db.Users.AsNoTracking() on r.AuthorId equals u.Id
            select new PublicPostReplyDto(
                r.Id,
                r.PostId,
                r.AuthorId,
                r.Content,
                r.Locale,
                r.ParentReplyId,
                r.IsByExpert,
                r.Depth,
                r.ChildCount,
                r.UpvoteCount,
                r.CreatedOn,
                $"{u.FirstName} {u.LastName}".Trim(),
                u.AvatarUrl))
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        // Poll snapshot — only for poll posts. Mirrors the GetPublicPostById path.
        PollSummaryDto? poll = null;
        if (post.Type == PostType.Poll)
        {
            var polls = await PollHydrator.FetchAsync(
                _db, _clock, new[] { post.Id }, request.UserId, cancellationToken).ConfigureAwait(false);
            poll = polls.GetValueOrDefault(post.Id);
        }

        return _msg.Ok(new PostActivityDto(
            post.UpvoteCount,
            post.DownvoteCount,
            post.Score,
            post.CommentsCount,
            newReplies,
            poll), MessageKeys.General.SUCCESS_OPERATION);
    }
}