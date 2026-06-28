using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.InteractiveMaps;
using MediatR;

namespace CCE.Application.InteractiveMaps.Commands.CreateInteractiveMap;

internal sealed class CreateInteractiveMapCommandHandler
    : IRequestHandler<CreateInteractiveMapCommand, Response<VoidData>>
{
    private readonly IRepository<InteractiveMap, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public CreateInteractiveMapCommandHandler(
        IRepository<InteractiveMap, System.Guid> repo,
        ICceDbContext db,
        MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(
        CreateInteractiveMapCommand request,
        CancellationToken cancellationToken)
    {
        var entity = InteractiveMap.Create(
            request.NameAr,
            request.NameEn,
            request.DescriptionAr,
            request.DescriptionEn);

        await _repo.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(MessageKeys.InteractiveMaps.MAP_CREATED);
    }
}
