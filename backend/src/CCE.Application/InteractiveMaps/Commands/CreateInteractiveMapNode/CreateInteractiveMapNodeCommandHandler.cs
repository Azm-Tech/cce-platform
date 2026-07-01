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
            interactiveMapId: request.InteractiveMapId,
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

        await _repo.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(MessageKeys.InteractiveMaps.NODE_CREATED);
    }
}
