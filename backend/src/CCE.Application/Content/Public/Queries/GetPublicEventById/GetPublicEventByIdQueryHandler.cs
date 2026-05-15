using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetPublicEventById;

public sealed class GetPublicEventByIdQueryHandler : IRequestHandler<GetPublicEventByIdQuery, PublicEventDto?>
{
    private readonly ICceDbContext _db;

    public GetPublicEventByIdQueryHandler(ICceDbContext db) => _db = db;

    public async Task<PublicEventDto?> Handle(GetPublicEventByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Events
            .Where(e => e.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var ev = list.SingleOrDefault();
        return ev is null ? null : MapToDto(ev);
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
