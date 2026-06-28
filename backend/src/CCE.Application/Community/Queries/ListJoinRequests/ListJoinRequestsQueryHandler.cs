using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Queries.ListJoinRequests;

public sealed class ListJoinRequestsQueryHandler
    : IRequestHandler<ListJoinRequestsQuery, Response<PagedResult<JoinRequestDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListJoinRequestsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PagedResult<JoinRequestDto>>> Handle(
        ListJoinRequestsQuery request, CancellationToken cancellationToken)
    {
        var paged = await _db.CommunityJoinRequests
            .Where(r => r.CommunityId == request.CommunityId && r.Status == JoinRequestStatus.Pending)
            .OrderBy(r => r.RequestedOn)
            .Select(r => new JoinRequestDto(
                r.Id, r.CommunityId, r.UserId, r.Status, r.RequestedOn, r.DecidedOn))
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok(paged, MessageKeys.General.ITEMS_LISTED);
    }
}
