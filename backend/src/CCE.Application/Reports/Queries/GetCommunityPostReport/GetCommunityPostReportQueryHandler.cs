using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.Reports.Dtos;
using MediatR;

namespace CCE.Application.Reports.Queries.GetCommunityPostReport;

internal sealed class GetCommunityPostReportQueryHandler(
    ICceDbContext _db,
    MessageFactory _msg)
    : IRequestHandler<GetCommunityPostReportQuery, Response<PagedResult<CommunityPostReportDto>>>
{
    public async Task<Response<PagedResult<CommunityPostReportDto>>> Handle(
        GetCommunityPostReportQuery q, CancellationToken ct)
    {
        var query = _db.Posts.AsQueryable();

        if (q.From.HasValue)
            query = query.Where(p => p.CreatedOn >= q.From.Value);
        if (q.To.HasValue)
            query = query.Where(p => p.CreatedOn <= q.To.Value);

        query = query.OrderByDescending(p => p.CreatedOn);

        var paged = await query.ToPagedResultAsync(
            p => new CommunityPostReportDto(
                p.Id,
                p.Title,
                p.Content,
                (int)p.Type,
                p.CreatedOn),
            q.Page,
            q.PageSize,
            ct)
            .ConfigureAwait(false);

        return _msg.Ok(paged, "ITEMS_LISTED");
    }
}
