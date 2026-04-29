using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListResourceCategories;
using MediatR;

namespace CCE.Application.Content.Queries.GetResourceCategoryById;

public sealed class GetResourceCategoryByIdQueryHandler : IRequestHandler<GetResourceCategoryByIdQuery, ResourceCategoryDto?>
{
    private readonly ICceDbContext _db;

    public GetResourceCategoryByIdQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<ResourceCategoryDto?> Handle(GetResourceCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.ResourceCategories
            .Where(c => c.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var category = list.SingleOrDefault();
        return category is null ? null : ListResourceCategoriesQueryHandler.MapToDto(category);
    }
}
