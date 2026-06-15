using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.InteractiveMaps.Public.Dtos;
using CCE.Application.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.InteractiveMaps.Public.Queries.GetInteractiveMapBySlug;

internal sealed class GetInteractiveMapBySlugQueryHandler
    : IRequestHandler<GetInteractiveMapBySlugQuery, Response<PublicInteractiveMapDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetInteractiveMapBySlugQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PublicInteractiveMapDto>> Handle(
        GetInteractiveMapBySlugQuery request,
        CancellationToken cancellationToken)
    {
        var map = await _db.InteractiveMaps
            .Where(m => m.Slug == request.Slug && m.IsActive)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (map is null)
            return _msg.MapNotFound<PublicInteractiveMapDto>();

        var nodes = await _db.InteractiveMapNodes
            .Where(n => n.InteractiveMapId == map.Id && n.IsActive)
            .OrderBy(n => n.Category)
            .ThenBy(n => n.Level)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok(
            PublicInteractiveMapDto.FromEntity(
                map,
                nodes.Select(PublicInteractiveMapNodeDto.FromEntity).ToList()),
            "ITEMS_LISTED");
    }
}
