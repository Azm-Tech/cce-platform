using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.GetResourceCategoryById;

public sealed class GetResourceCategoryByIdQueryHandler : IRequestHandler<GetResourceCategoryByIdQuery, Response<ResourceCategoryDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public GetResourceCategoryByIdQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<ResourceCategoryDto>> Handle(GetResourceCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.ResourceCategories
            .Where(c => c.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var category = list.SingleOrDefault();
        if (category is null)
            return _messages.NotFound<ResourceCategoryDto>(MessageKeys.Content.CATEGORY_NOT_FOUND);

        return _messages.Ok(MapToDto(category), MessageKeys.General.SUCCESS_OPERATION);
    }

    internal static ResourceCategoryDto MapToDto(ResourceCategory c) => new(
        c.Id,
        c.NameAr,
        c.NameEn,
        c.Slug,
        c.ParentId,
        c.OrderIndex,
        c.IsActive);
}
