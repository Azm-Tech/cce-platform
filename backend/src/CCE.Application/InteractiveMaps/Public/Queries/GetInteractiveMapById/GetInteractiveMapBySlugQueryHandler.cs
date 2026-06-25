using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.InteractiveMaps.Public.Dtos;
using CCE.Application.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.InteractiveMaps.Public.Queries.GetInteractiveMapById;

internal sealed class GetPublicInteractiveMapByIdQueryHandler
    : IRequestHandler<GetPublicInteractiveMapByIdQuery, Response<PublicInteractiveMapDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetPublicInteractiveMapByIdQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PublicInteractiveMapDto>> Handle(
        GetPublicInteractiveMapByIdQuery request,
        CancellationToken cancellationToken)
    {
        var map = await _db.InteractiveMaps
            .Where(m => m.Id == request.Id && m.IsActive)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (map is null)
            return _msg.NotFound<PublicInteractiveMapDto>(MessageKeys.InteractiveMaps.MAP_NOT_FOUND);

        var nodes = await _db.InteractiveMapNodes
            .Include(n => n.Tags)
            .Where(n => n.InteractiveMapId == map.Id && n.IsActive)
            .OrderBy(n => n.Category)
            .ThenBy(n => n.Level)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok(
            PublicInteractiveMapDto.FromEntity(
                map,
                nodes.Select(PublicInteractiveMapNodeDto.FromEntity).ToList()),
            MessageKeys.General.ITEMS_LISTED);
    }
}
