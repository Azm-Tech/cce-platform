using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicEvents;

public sealed class ListPublicEventsQueryHandler : IRequestHandler<ListPublicEventsQuery, PagedResult<PublicEventDto>>
{
    private readonly ICceDbContext _db;

    public ListPublicEventsQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<PublicEventDto>> Handle(ListPublicEventsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<CCE.Domain.Content.Event> query = _db.Events;

        if (request.From is { } from && request.To is { } to)
        {
            query = query.Where(e => e.StartsOn >= from && e.StartsOn <= to);
        }
        else
        {
            var now = System.DateTimeOffset.UtcNow;
            query = query.Where(e => e.StartsOn >= now);
        }

        query = query.OrderBy(e => e.StartsOn);

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = page.Items.Select(MapToDto).ToList();
        return new PagedResult<PublicEventDto>(items, page.Page, page.PageSize, page.Total);
    }

    internal static PublicEventDto MapToDto(CCE.Domain.Content.Event e) => new(
        e.Id,
        e.TitleAr,
        e.TitleEn,
        e.DescriptionAr,
        e.DescriptionEn,
        e.StartsOn,
        e.EndsOn,
        e.LocationAr,
        e.LocationEn,
        e.OnlineMeetingUrl,
        e.FeaturedImageUrl,
        e.ICalUid);
}
