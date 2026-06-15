using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.InteractiveMaps;
using MediatR;

namespace CCE.Application.InteractiveMaps.Commands.CreateInteractiveMapNode;

internal sealed class CreateInteractiveMapNodeCommandHandler
    : IRequestHandler<CreateInteractiveMapNodeCommand, Response<VoidData>>
{
    private readonly IRepository<InteractiveMapNode, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public CreateInteractiveMapNodeCommandHandler(
        IRepository<InteractiveMapNode, System.Guid> repo,
        ICceDbContext db,
        MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(
        CreateInteractiveMapNodeCommand request,
        CancellationToken cancellationToken)
    {
        var entity = InteractiveMapNode.Create(
            request.InteractiveMapId,
            request.NameAr,
            request.NameEn,
            request.IconKey,
            request.Category,
            request.CategoryNameAr,
            request.CategoryNameEn,
            request.Level,
            request.ParentId,
            request.TopicId);

        await _repo.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.NodeCreated();
    }
}
