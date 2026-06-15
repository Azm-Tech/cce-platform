using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.InteractiveMaps.Public.Dtos;
using CCE.Application.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.InteractiveMaps.Public.Queries.ListInteractiveMaps;

internal sealed class ListInteractiveMapsQueryHandler
    : IRequestHandler<ListInteractiveMapsQuery, Response<IReadOnlyList<PublicInteractiveMapDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListInteractiveMapsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<IReadOnlyList<PublicInteractiveMapDto>>> Handle(
        ListInteractiveMapsQuery request,
        CancellationToken cancellationToken)
    {
        var maps = await _db.InteractiveMaps
            .Where(m => m.IsActive)
            .OrderBy(m => m.NameEn)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var mapIds = maps.Select(m => m.Id).ToList();
        var nodes = await _db.InteractiveMapNodes
            .Include(n => n.Tags)
            .Where(n => mapIds.Contains(n.InteractiveMapId) && n.IsActive)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var nodesByMapId = nodes.GroupBy(n => n.InteractiveMapId)
            .ToDictionary(g => g.Key, g => g.OrderBy(n => n.Category).ThenBy(n => n.Level).Select(PublicInteractiveMapNodeDto.FromEntity).ToList() as IReadOnlyList<PublicInteractiveMapNodeDto>);

        var dtos = maps.Select(m =>
            PublicInteractiveMapDto.FromEntity(m, nodesByMapId.GetValueOrDefault(m.Id) ?? [])
        ).ToList();

        return _msg.Ok(dtos as IReadOnlyList<PublicInteractiveMapDto>, "ITEMS_LISTED");
    }
}
