using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.InteractiveMaps.Dtos;
using CCE.Application.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.InteractiveMaps.Queries.GetCurrentInteractiveMap;

internal sealed class GetCurrentInteractiveMapQueryHandler
    : IRequestHandler<GetCurrentInteractiveMapQuery, Response<InteractiveMapDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetCurrentInteractiveMapQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<InteractiveMapDto>> Handle(
        GetCurrentInteractiveMapQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await _db.InteractiveMaps
            .Where(m => m.IsActive)
            .Select(m => new InteractiveMapDto(
                m.Id,
                m.NameAr,
                m.NameEn,
                m.DescriptionAr,
                m.DescriptionEn,
                m.IsActive))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (dto is null)
            return _msg.NotFound<InteractiveMapDto>(MessageKeys.InteractiveMaps.MAP_NOT_FOUND);

        return _msg.Ok(dto, MessageKeys.General.ITEMS_LISTED);
    }
}
