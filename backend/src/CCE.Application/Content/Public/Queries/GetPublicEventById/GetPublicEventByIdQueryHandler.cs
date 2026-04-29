using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Application.Content.Public.Queries.ListPublicEvents;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetPublicEventById;

public sealed class GetPublicEventByIdQueryHandler : IRequestHandler<GetPublicEventByIdQuery, PublicEventDto?>
{
    private readonly ICceDbContext _db;

    public GetPublicEventByIdQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PublicEventDto?> Handle(GetPublicEventByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Events
            .Where(e => e.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var ev = list.SingleOrDefault();
        return ev is null ? null : ListPublicEventsQueryHandler.MapToDto(ev);
    }
}
