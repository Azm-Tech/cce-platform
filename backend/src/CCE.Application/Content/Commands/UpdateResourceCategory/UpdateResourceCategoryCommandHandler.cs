using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListResourceCategories;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.UpdateResourceCategory;

public sealed class UpdateResourceCategoryCommandHandler : IRequestHandler<UpdateResourceCategoryCommand, Response<ResourceCategoryDto>>
{
    private readonly IRepository<ResourceCategory, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public UpdateResourceCategoryCommandHandler(
        IRepository<ResourceCategory, System.Guid> repo,
        ICceDbContext db,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _messages = messages;
    }

    public async Task<Response<ResourceCategoryDto>> Handle(UpdateResourceCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (category is null)
            return _messages.NotFound<ResourceCategoryDto>(MessageKeys.Content.CATEGORY_NOT_FOUND);

        category.UpdateNames(request.NameAr, request.NameEn);
        category.Reorder(request.OrderIndex);

        if (request.IsActive)
            category.Activate();
        else
            category.Deactivate();

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _messages.Ok(ListResourceCategoriesQueryHandler.MapToDto(category), MessageKeys.General.SUCCESS_OPERATION);
    }
}
