using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Content.Public.Queries.ListPublicResources;

public sealed class ListPublicResourcesQueryHandler : IRequestHandler<ListPublicResourcesQuery, Response<PagedResult<PublicResourceDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;
    private readonly IUserContentInterestResolver _resolver;

    public ListPublicResourcesQueryHandler(ICceDbContext db, MessageFactory messages, IUserContentInterestResolver resolver)
    {
        _db = db;
        _messages = messages;
        _resolver = resolver;
    }

    public async Task<Response<PagedResult<PublicResourceDto>>> Handle(ListPublicResourcesQuery request, CancellationToken cancellationToken)
    {
        var knowledgeLevelId = request.KnowledgeLevelId;
        var jobSectorId = request.JobSectorId;

        (knowledgeLevelId, jobSectorId) = await _resolver.ResolveAsync(knowledgeLevelId, jobSectorId, cancellationToken).ConfigureAwait(false);

        var query = _db.Resources
            .AsNoTracking()
            .Include(r => r.Countries)
            .Where(r => r.PublishedOn != null)
            .WhereIf(!string.IsNullOrWhiteSpace(request.Search),
                r => r.TitleAr.Contains(request.Search!) ||
                     r.TitleEn.Contains(request.Search!) ||
                     r.DescriptionAr.Contains(request.Search!) ||
                     r.DescriptionEn.Contains(request.Search!))
            .WhereIf(request.CategoryId.HasValue,   r => r.CategoryId == request.CategoryId!.Value)
            .WhereIf(request.CountryId.HasValue,    r => r.Countries.Any(c => c.CountryId == request.CountryId!.Value))
            .WhereIf(request.ResourceType.HasValue, r => r.ResourceType == request.ResourceType!.Value);

        if (knowledgeLevelId.HasValue || jobSectorId.HasValue)
        {
            query = query.Where(r =>
                (!knowledgeLevelId.HasValue || r.KnowledgeLevelId == null || r.KnowledgeLevelId == knowledgeLevelId.Value) &&
                (!jobSectorId.HasValue || r.JobSectorId == null || r.JobSectorId == jobSectorId.Value));

            query = query.OrderByDescending(r =>
                (knowledgeLevelId.HasValue && r.KnowledgeLevelId == knowledgeLevelId.Value ? 2 : 0) +
                (jobSectorId.HasValue && r.JobSectorId == jobSectorId.Value ? 1 : 0))
                .ThenByDescending(r => r.PublishedOn);
        }
        else
        {
            query = query.OrderByDescending(r => r.PublishedOn);
        }

        var paged = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);

        // Batch enrich categories / assets / country names for the page
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
            return new PublicResourceDto(
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
                userNameMap.GetValueOrDefault(r.UploadedById) ?? string.Empty,
                r.PublishedOn!.Value,
                r.ViewCount,
                r.KnowledgeLevelId,
                r.JobSectorId);
        }).ToList();

        var result = new PagedResult<PublicResourceDto>(dtos, paged.Page, paged.PageSize, paged.Total);
        return _messages.Ok(result, MessageKeys.General.ITEMS_LISTED);
    }

}
