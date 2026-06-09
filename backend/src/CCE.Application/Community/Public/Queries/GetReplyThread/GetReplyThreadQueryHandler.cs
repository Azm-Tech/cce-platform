using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Community.Public.Queries.ListPublicPostReplies;
using CCE.Application.Errors;
using CCE.Application.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Community.Public.Queries.GetReplyThread;

public sealed class GetReplyThreadQueryHandler
    : IRequestHandler<GetReplyThreadQuery, Response<PagedResult<PublicPostReplyDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetReplyThreadQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PagedResult<PublicPostReplyDto>>> Handle(
        GetReplyThreadQuery request, CancellationToken cancellationToken)
    {
        var prefix = await _db.PostReplies
            .Where(r => r.Id == request.ReplyId)
            .Select(r => r.ThreadPath)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (prefix is null)
            return _msg.NotFound<PagedResult<PublicPostReplyDto>>(ApplicationErrors.Community.REPLY_NOT_FOUND);

        var paged = await _db.PostReplies
            .Where(r => r.ThreadPath.StartsWith(prefix) && r.Id != request.ReplyId)
            .OrderBy(r => r.ThreadPath)
            .Select(r => ListPublicPostRepliesQueryHandler.MapToDto(r))
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok(paged, "ITEMS_LISTED");
    }
}
