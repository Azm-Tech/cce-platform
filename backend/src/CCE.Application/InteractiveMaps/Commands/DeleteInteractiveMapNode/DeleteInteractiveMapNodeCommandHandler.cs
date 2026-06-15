using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.InteractiveMaps;
using MediatR;

namespace CCE.Application.InteractiveMaps.Commands.DeleteInteractiveMapNode;

internal sealed class DeleteInteractiveMapNodeCommandHandler
    : IRequestHandler<DeleteInteractiveMapNodeCommand, Response<VoidData>>
{
    private readonly IRepository<InteractiveMapNode, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public DeleteInteractiveMapNodeCommandHandler(
        IRepository<InteractiveMapNode, System.Guid> repo,
        ICceDbContext db,
        MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(
        DeleteInteractiveMapNodeCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entity is null || entity.InteractiveMapId != request.MapId)
            return _msg.NodeNotFound<VoidData>();

        entity.Deactivate();
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.NodeDeleted();
    }
}
