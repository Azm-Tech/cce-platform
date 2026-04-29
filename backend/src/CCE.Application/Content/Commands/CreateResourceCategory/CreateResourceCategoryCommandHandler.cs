using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListResourceCategories;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.CreateResourceCategory;

public sealed class CreateResourceCategoryCommandHandler : IRequestHandler<CreateResourceCategoryCommand, ResourceCategoryDto>
{
    private readonly IResourceCategoryService _service;

    public CreateResourceCategoryCommandHandler(IResourceCategoryService service)
    {
        _service = service;
    }

    public async Task<ResourceCategoryDto> Handle(CreateResourceCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = ResourceCategory.Create(
            request.NameAr,
            request.NameEn,
            request.Slug,
            request.ParentId,
            request.OrderIndex);

        await _service.SaveAsync(category, cancellationToken).ConfigureAwait(false);

        return ListResourceCategoriesQueryHandler.MapToDto(category);
    }
}
