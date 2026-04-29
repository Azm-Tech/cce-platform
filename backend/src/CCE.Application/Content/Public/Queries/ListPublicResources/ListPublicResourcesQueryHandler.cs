using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicResources;

public sealed class ListPublicResourcesQueryHandler : IRequestHandler<ListPublicResourcesQuery, PagedResult<PublicResourceDto>>
{
    private readonly ICceDbContext _db;

    public ListPublicResourcesQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<PublicResourceDto>> Handle(ListPublicResourcesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<Resource> query = _db.Resources.Where(r => r.PublishedOn != null);

        if (request.CategoryId is { } categoryId)
        {
            query = query.Where(r => r.CategoryId == categoryId);
        }

        if (request.CountryId is { } countryId)
        {
            query = query.Where(r => r.CountryId == countryId);
        }

        if (request.ResourceType is { } resourceType)
        {
            query = query.Where(r => r.ResourceType == resourceType);
        }

        query = query.OrderByDescending(r => r.PublishedOn);

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = page.Items.Select(MapToDto).ToList();
        return new PagedResult<PublicResourceDto>(items, page.Page, page.PageSize, page.Total);
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
