using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.Reports.Dtos;
using MediatR;

namespace CCE.Application.Reports.Queries.GetNewsReport;

internal sealed class GetNewsReportQueryHandler(
    ICceDbContext _db,
    MessageFactory _msg)
    : IRequestHandler<GetNewsReportQuery, Response<PagedResult<NewsReportDto>>>
{
    public async Task<Response<PagedResult<NewsReportDto>>> Handle(
        GetNewsReportQuery q, CancellationToken ct)
    {
        var query = from n in _db.News
                    join t in _db.Topics on n.TopicId equals t.Id
                    select new { n, TopicNameEn = t.NameEn, TopicNameAr = t.NameAr };

        if (q.From.HasValue)
            query = query.Where(x => x.n.PublishedOn >= q.From.Value);
        if (q.To.HasValue)
            query = query.Where(x => x.n.PublishedOn <= q.To.Value);

        query = query.OrderByDescending(x => x.n.PublishedOn);

        var paged = await query.ToPagedResultAsync(
            x => new NewsReportDto(
                x.n.Id,
                x.n.TitleAr,
                x.n.TitleEn,
                x.n.FeaturedImageUrl,
                x.TopicNameEn,
                x.TopicNameAr,
                x.n.ContentAr,
                x.n.ContentEn,
                x.n.PublishedOn),
            q.Page,
            q.PageSize,
            ct)
            .ConfigureAwait(false);

        return _msg.Ok(paged, "ITEMS_LISTED");
    }
}
