using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.GetEventById;

public sealed class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventDto?>
{
    private readonly ICceDbContext _db;

    public GetEventByIdQueryHandler(ICceDbContext db) => _db = db;

    public async Task<EventDto?> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Events.Where(e => e.Id == request.Id).ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var ev = list.SingleOrDefault();
        return ev is null ? null : MapToDto(ev);
    }

    internal static EventDto MapToDto(Event e) => new(
        e.Id, e.TitleAr, e.TitleEn, e.DescriptionAr, e.DescriptionEn,
        e.StartsOn, e.EndsOn, e.LocationAr, e.LocationEn,
        e.OnlineMeetingUrl, e.FeaturedImageUrl, e.ICalUid,
        System.Convert.ToBase64String(e.RowVersion));
}
