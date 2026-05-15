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

    public ListResourcesQueryHandler(ICceDbContext db) => _db = db;

    public async Task<PagedResult<ResourceDto>> Handle(
        ListResourcesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Resources
            .WhereIf(!string.IsNullOrWhiteSpace(request.Search),
                r => r.TitleAr.Contains(request.Search!) ||
                     r.TitleEn.Contains(request.Search!) ||
                     r.DescriptionAr.Contains(request.Search!) ||
                     r.DescriptionEn.Contains(request.Search!))
            .WhereIf(request.CategoryId.HasValue, r => r.CategoryId == request.CategoryId!.Value)
            .WhereIf(request.CountryId.HasValue,  r => r.CountryId == request.CountryId!.Value)
            .WhereIf(request.IsPublished == true,  r => r.PublishedOn != null)
            .WhereIf(request.IsPublished == false, r => r.PublishedOn == null)
            .OrderByDescending(r => r.PublishedOn ?? DateTimeOffset.MinValue)
            .ThenByDescending(r => r.Id);

        var result = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);
        return result.Map(MapToDto);
    }

    internal static ResourceDto MapToDto(Resource r) => new(
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
