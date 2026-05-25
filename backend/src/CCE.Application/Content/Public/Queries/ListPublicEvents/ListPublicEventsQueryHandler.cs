using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicEvents;

public sealed class ListPublicEventsQueryHandler : IRequestHandler<ListPublicEventsQuery, PagedResult<PublicEventDto>>
{
    private readonly ICceDbContext _db;

    public ListPublicEventsQueryHandler(ICceDbContext db) => _db = db;

    public async Task<PagedResult<PublicEventDto>> Handle(ListPublicEventsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Events.AsQueryable();

        if (request.From.HasValue && request.To.HasValue)
        {
            query = query.Where(e => e.StartsOn >= request.From.Value && e.StartsOn <= request.To.Value);
        }
        else
        {
            var now = DateTimeOffset.UtcNow;
            query = query.Where(e => e.StartsOn >= now);
        }

        query = query.OrderBy(e => e.StartsOn);

        var result = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);
        return result.Map(MapToDto);
    }

    internal static PublicEventDto MapToDto(Event e) => new(
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
