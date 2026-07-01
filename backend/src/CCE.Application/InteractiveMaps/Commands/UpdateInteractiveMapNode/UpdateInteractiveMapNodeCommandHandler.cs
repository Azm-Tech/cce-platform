using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.InteractiveMaps;
using MediatR;

namespace CCE.Application.InteractiveMaps.Commands.UpdateInteractiveMapNode;

internal sealed class UpdateInteractiveMapNodeCommandHandler
    : IRequestHandler<UpdateInteractiveMapNodeCommand, Response<VoidData>>
{
    private readonly IRepository<InteractiveMapNode, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public UpdateInteractiveMapNodeCommandHandler(
        IRepository<InteractiveMapNode, System.Guid> repo,
        ICceDbContext db,
        MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(
        UpdateInteractiveMapNodeCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entity is null || entity.InteractiveMapId != request.MapId)
            return _msg.NotFound<VoidData>(MessageKeys.InteractiveMaps.NODE_NOT_FOUND);

        entity.UpdateDetails(
            nameAr: request.NameAr,
            nameEn: request.NameEn,
            iconKey: request.IconKey,
            category: request.Category,
            categoryNameAr: request.CategoryNameAr,
            categoryNameEn: request.CategoryNameEn,
            titleAr: request.TitleAr,
            titleEn: request.TitleEn,
            descriptionAr: request.DescriptionAr,
            descriptionEn: request.DescriptionEn,
            parentId: request.ParentId,
            topicId: request.TopicId);

        if (request.IsActive)
            entity.Activate();
        else
            entity.Deactivate();

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(MessageKeys.InteractiveMaps.NODE_UPDATED);
    }
}
