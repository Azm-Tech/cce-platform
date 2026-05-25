using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.ListEvents;

public sealed class ListEventsQueryHandler : IRequestHandler<ListEventsQuery, PagedResult<EventDto>>
{
    private readonly ICceDbContext _db;

    public ListEventsQueryHandler(ICceDbContext db) => _db = db;

    public async Task<PagedResult<EventDto>> Handle(ListEventsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Events
            .WhereIf(!string.IsNullOrWhiteSpace(request.Search),
                e => e.TitleAr.Contains(request.Search!) ||
                     e.TitleEn.Contains(request.Search!))
            .WhereIf(request.FromDate.HasValue, e => e.StartsOn >= request.FromDate!.Value)
            .WhereIf(request.ToDate.HasValue,   e => e.EndsOn <= request.ToDate!.Value)
            .OrderByDescending(e => e.StartsOn);

        var result = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);
        return result.Map(MapToDto);
    }

    internal static EventDto MapToDto(Event e) => new(
        e.Id, e.TitleAr, e.TitleEn, e.DescriptionAr, e.DescriptionEn,
        e.StartsOn, e.EndsOn, e.LocationAr, e.LocationEn,
        e.OnlineMeetingUrl, e.FeaturedImageUrl, e.ICalUid,
        System.Convert.ToBase64String(e.RowVersion));
}
