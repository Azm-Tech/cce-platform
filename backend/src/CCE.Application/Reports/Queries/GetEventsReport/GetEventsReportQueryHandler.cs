using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.Reports.Dtos;
using MediatR;

namespace CCE.Application.Reports.Queries.GetEventsReport;

internal sealed class GetEventsReportQueryHandler(
    ICceDbContext _db,
    MessageFactory _msg)
    : IRequestHandler<GetEventsReportQuery, Response<PagedResult<EventsReportDto>>>
{
    public async Task<Response<PagedResult<EventsReportDto>>> Handle(
        GetEventsReportQuery q, CancellationToken ct)
    {
        var query = from e in _db.Events
                    join t in _db.Topics on e.TopicId equals t.Id
                    select new { e, TopicName = t.NameEn };

        if (q.From.HasValue)
            query = query.Where(x => x.e.StartsOn >= q.From.Value);
        if (q.To.HasValue)
            query = query.Where(x => x.e.StartsOn <= q.To.Value);

        query = query.OrderByDescending(x => x.e.StartsOn);

        var paged = await query.ToPagedResultAsync(
            x => new EventsReportDto(
                x.e.Id,
                x.e.TitleEn,
                x.e.DescriptionEn,
                x.e.LocationEn,
                x.TopicName,
                x.e.StartsOn,
                x.e.EndsOn,
                x.e.FeaturedImageUrl,
                x.e.OnlineMeetingUrl,
                x.e.CreatedOn),
            q.Page,
            q.PageSize,
            ct)
            .ConfigureAwait(false);

        return _msg.Ok(paged, MessageKeys.General.ITEMS_LISTED);
    }
}
