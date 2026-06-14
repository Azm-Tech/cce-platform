using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListPublicPostReplies;

public sealed class ListPublicPostRepliesQueryHandler
    : IRequestHandler<ListPublicPostRepliesQuery, Response<PagedResult<PublicPostReplyDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListPublicPostRepliesQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PagedResult<PublicPostReplyDto>>> Handle(
        ListPublicPostRepliesQuery request,
        CancellationToken cancellationToken)
    {
        // Top-level comments first, ranked by score; nested replies fetched via the thread query.
        var paged = await _db.PostReplies
            .Where(r => r.PostId == request.PostId && r.ParentReplyId == null)
            .OrderByDescending(r => r.Score)
            .Select(r => MapToDto(r))
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok(paged, "ITEMS_LISTED");
    }

    internal static PublicPostReplyDto MapToDto(PostReply r) => new(
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
        r.CreatedOn);
}
