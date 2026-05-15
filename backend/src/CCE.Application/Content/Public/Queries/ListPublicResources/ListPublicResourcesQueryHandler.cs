using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicResources;

public sealed class ListPublicResourcesQueryHandler : IRequestHandler<ListPublicResourcesQuery, PagedResult<PublicResourceDto>>
{
    private readonly ICceDbContext _db;

    public ListPublicResourcesQueryHandler(ICceDbContext db) => _db = db;

    public async Task<PagedResult<PublicResourceDto>> Handle(ListPublicResourcesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Resources
            .Where(r => r.PublishedOn != null)
            .WhereIf(request.CategoryId.HasValue,   r => r.CategoryId == request.CategoryId!.Value)
            .WhereIf(request.CountryId.HasValue,    r => r.CountryId == request.CountryId!.Value)
            .WhereIf(request.ResourceType.HasValue, r => r.ResourceType == request.ResourceType!.Value)
            .OrderByDescending(r => r.PublishedOn);

        var result = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);
        return result.Map(MapToDto);
    }

    internal static PublicResourceDto MapToDto(Resource r) => new(
        r.Id,
        r.TitleAr,
        r.TitleEn,
        r.DescriptionAr,
        r.DescriptionEn,
        r.ResourceType,
        r.CategoryId,
        r.CountryId,
        r.AssetFileId,
        r.PublishedOn!.Value,
        r.ViewCount);
}
