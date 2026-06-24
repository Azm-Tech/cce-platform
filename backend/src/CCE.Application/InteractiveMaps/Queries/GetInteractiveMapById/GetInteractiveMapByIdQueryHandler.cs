using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.InteractiveMaps.Dtos;
using CCE.Application.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.InteractiveMaps.Queries.GetInteractiveMapById;

internal sealed class GetInteractiveMapByIdQueryHandler
    : IRequestHandler<GetInteractiveMapByIdQuery, Response<InteractiveMapDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetInteractiveMapByIdQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<InteractiveMapDto>> Handle(
        GetInteractiveMapByIdQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await _db.InteractiveMaps
            .Where(m => m.Id == request.Id)
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
            return _msg.MapNotFound<InteractiveMapDto>();

        return _msg.Ok(dto, MessageKeys.General.ITEMS_LISTED);
    }
}
