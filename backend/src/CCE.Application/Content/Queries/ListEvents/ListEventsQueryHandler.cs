using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.ListEvents;

public sealed class ListEventsQueryHandler : IRequestHandler<ListEventsQuery, PagedResult<EventDto>>
{
    private readonly ICceDbContext _db;

    public ListEventsQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<EventDto>> Handle(ListEventsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<CCE.Domain.Content.Event> query = _db.Events;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(e =>
                e.TitleAr.Contains(term) ||
                e.TitleEn.Contains(term));
        }

        if (request.FromDate is { } fromDate)
        {
            query = query.Where(e => e.StartsOn >= fromDate);
        }

        if (request.ToDate is { } toDate)
        {
            query = query.Where(e => e.EndsOn <= toDate);
        }

        query = query.OrderByDescending(e => e.StartsOn);

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = page.Items.Select(MapToDto).ToList();
        return new PagedResult<EventDto>(items, page.Page, page.PageSize, page.Total);
    }

    internal static EventDto MapToDto(CCE.Domain.Content.Event e) => new(
        e.Id, e.TitleAr, e.TitleEn, e.DescriptionAr, e.DescriptionEn,
        e.StartsOn, e.EndsOn, e.LocationAr, e.LocationEn,
        e.OnlineMeetingUrl, e.FeaturedImageUrl, e.ICalUid,
        System.Convert.ToBase64String(e.RowVersion));
}
