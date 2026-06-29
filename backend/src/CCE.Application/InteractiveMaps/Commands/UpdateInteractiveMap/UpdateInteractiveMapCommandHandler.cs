using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.InteractiveMaps;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.InteractiveMaps.Commands.UpdateInteractiveMap;

internal sealed class UpdateInteractiveMapCommandHandler
    : IRequestHandler<UpdateInteractiveMapCommand, Response<VoidData>>
{
    private readonly IRepository<InteractiveMap, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public UpdateInteractiveMapCommandHandler(
        IRepository<InteractiveMap, System.Guid> repo,
        ICceDbContext db,
        MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(
        UpdateInteractiveMapCommand request,
        CancellationToken cancellationToken)
    {
        var mapId = await _db.InteractiveMaps
            .IgnoreQueryFilters()
            .Select(m => m.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        InteractiveMap entity;

            if (mapId == default)
            {
                entity = InteractiveMap.Create(
                    request.NameAr,
                    request.NameEn,
                    request.DescriptionAr,
                    request.DescriptionEn);

                await _repo.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                entity = (await _repo.GetByIdAsync(mapId, cancellationToken).ConfigureAwait(false))!;

                if (entity is null)
                    return _msg.NotFound<VoidData>(MessageKeys.InteractiveMaps.MAP_NOT_FOUND);

                entity.UpdateDetails(
                    request.NameAr,
                    request.NameEn,
                    request.DescriptionAr,
                    request.DescriptionEn);
            }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(MessageKeys.InteractiveMaps.MAP_UPDATED);
    }
}
