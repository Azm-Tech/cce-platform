using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListResourceCategories;
using MediatR;

namespace CCE.Application.Content.Commands.UpdateResourceCategory;

public sealed class UpdateResourceCategoryCommandHandler : IRequestHandler<UpdateResourceCategoryCommand, ResourceCategoryDto?>
{
    private readonly IResourceCategoryService _service;

    public UpdateResourceCategoryCommandHandler(IResourceCategoryService service)
    {
        _service = service;
    }

    public async Task<ResourceCategoryDto?> Handle(UpdateResourceCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (category is null)
        {
            return null;
        }

        category.UpdateNames(request.NameAr, request.NameEn);
        category.Reorder(request.OrderIndex);

        if (request.IsActive)
            category.Activate();
        else
            category.Deactivate();

        await _service.UpdateAsync(category, cancellationToken).ConfigureAwait(false);

        return ListResourceCategoriesQueryHandler.MapToDto(category);
    }
}
