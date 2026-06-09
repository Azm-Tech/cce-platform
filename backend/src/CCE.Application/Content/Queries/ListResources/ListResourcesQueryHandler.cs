using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Content;
using CCE.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Content.Queries.ListResources;

public sealed class ListResourcesQueryHandler : IRequestHandler<ListResourcesQuery, Response<PagedResult<ResourceDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public ListResourcesQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<PagedResult<ResourceDto>>> Handle(
        ListResourcesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Resources
            .AsNoTracking()
            .Include(r => r.Countries)
            .WhereIf(!string.IsNullOrWhiteSpace(request.Search),
                r => r.TitleAr.Contains(request.Search!) ||
                     r.TitleEn.Contains(request.Search!) ||
                     r.DescriptionAr.Contains(request.Search!) ||
                     r.DescriptionEn.Contains(request.Search!))
            .WhereIf(request.CategoryId.HasValue, r => r.CategoryId == request.CategoryId!.Value)
            .WhereIf(request.CountryId.HasValue,  r => r.Countries.Any(c => c.CountryId == request.CountryId!.Value))
            .WhereIf(request.IsPublished == true,  r => r.PublishedOn != null)
            .WhereIf(request.IsPublished == false, r => r.PublishedOn == null)
            .OrderByDescending(r => r.PublishedOn)
            .ThenByDescending(r => r.Id);

        var paged = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);

        // Batch enrich categories / assets / country names for the page (avoids N+1)
        var categoryIds = paged.Items.Select(r => r.CategoryId).Distinct().ToList();
        var assetIds = paged.Items.Select(r => r.AssetFileId).Distinct().ToList();
        var allCountryIds = paged.Items.SelectMany(r => r.Countries.Select(c => c.CountryId)).Distinct().ToList();

        var categories = await _db.ResourceCategories
            .Where(c => categoryIds.Contains(c.Id))
            .Select(c => new { c.Id, c.NameAr, c.NameEn })
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var categoryMap = categories.ToDictionary(c => c.Id, c => new { c.NameAr, c.NameEn });

        var assets = await _db.AssetFiles
            .Where(a => assetIds.Contains(a.Id))
            .Select(a => new { a.Id, a.OriginalFileName })
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var assetMap = assets.ToDictionary(a => a.Id, a => a.OriginalFileName);

        var countries = await _db.Countries
            .Where(c => allCountryIds.Contains(c.Id))
            .Select(c => new { c.Id, c.NameAr })
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var countryNameMap = countries.ToDictionary(c => c.Id, c => c.NameAr);

        var userIds = paged.Items.Select(r => r.UploadedById).Distinct().ToList();
        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.UserName })
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var userNameMap = users.ToDictionary(
            u => u.Id,
            u =>
            {
                var fullName = $"{u.FirstName} {u.LastName}".Trim();
                return string.IsNullOrEmpty(fullName) ? u.UserName : fullName;
            });

        var dtos = paged.Items.Select(r =>
        {
            var cat = categoryMap.GetValueOrDefault(r.CategoryId);
            var countryIds = r.Countries.Select(c => c.CountryId).ToList();
            var countryNames = countryIds.Select(id => countryNameMap.GetValueOrDefault(id) ?? string.Empty).ToList();
            return new ResourceDto(
                r.Id,
                r.TitleAr,
                r.TitleEn,
                r.DescriptionAr,
                r.DescriptionEn,
                r.ResourceType,
                ResourceTypeAr.Get(r.ResourceType),
                r.CategoryId,
                cat?.NameAr ?? string.Empty,
                cat?.NameEn ?? string.Empty,
                r.AssetFileId,
                assetMap.GetValueOrDefault(r.AssetFileId) ?? string.Empty,
                countryIds,
                countryNames,
                r.UploadedById,
                userNameMap.GetValueOrDefault(r.UploadedById) ?? string.Empty,
                r.PublishedOn,
                r.ViewCount,
                r.IsCenterManaged,
                r.IsPublished);
        }).ToList();

        var result = new PagedResult<ResourceDto>(dtos, paged.Page, paged.PageSize, paged.Total);
        return _messages.Ok(result, "ITEMS_LISTED");
    }
}
