using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.ListResources;

public sealed class ListResourcesQueryHandler
    : IRequestHandler<ListResourcesQuery, PagedResult<ResourceDto>>
{
    private readonly ICceDbContext _db;

    public ListResourcesQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ResourceDto>> Handle(
        ListResourcesQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<Resource> query = _db.Resources;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(r =>
                r.TitleAr.Contains(term) ||
                r.TitleEn.Contains(term) ||
                r.DescriptionAr.Contains(term) ||
                r.DescriptionEn.Contains(term));
        }
        if (request.CategoryId is { } categoryId)
        {
            query = query.Where(r => r.CategoryId == categoryId);
        }
        if (request.CountryId is { } countryId)
        {
            query = query.Where(r => r.CountryId == countryId);
        }
        if (request.IsPublished is { } isPublished)
        {
            query = isPublished
                ? query.Where(r => r.PublishedOn != null)
                : query.Where(r => r.PublishedOn == null);
        }
        query = query.OrderByDescending(r => r.PublishedOn ?? System.DateTimeOffset.MinValue)
                     .ThenByDescending(r => r.Id);

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = page.Items.Select(MapToDto).ToList();
        return new PagedResult<ResourceDto>(items, page.Page, page.PageSize, page.Total);
    }

    private static ResourceDto MapToDto(Resource r) => new(
        r.Id,
        r.TitleAr,
        r.TitleEn,
        r.DescriptionAr,
        r.DescriptionEn,
        r.ResourceType,
        r.CategoryId,
        r.CountryId,
        r.UploadedById,
        r.AssetFileId,
        r.PublishedOn,
        r.ViewCount,
        r.IsCenterManaged,
        r.IsPublished,
        System.Convert.ToBase64String(r.RowVersion));
}
