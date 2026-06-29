using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.Reports.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Reports.Queries.GetResourcesReport;

internal sealed class GetResourcesReportQueryHandler(
    ICceDbContext _db,
    MessageFactory _msg)
    : IRequestHandler<GetResourcesReportQuery, Response<PagedResult<ResourcesReportDto>>>
{
    public async Task<Response<PagedResult<ResourcesReportDto>>> Handle(
        GetResourcesReportQuery q, CancellationToken ct)
    {
        var query = from r in _db.Resources
                    join cat in _db.ResourceCategories on r.CategoryId equals cat.Id
                    select new { r, CategoryNameAr = cat.NameAr, CategoryNameEn = cat.NameEn };

        if (q.From.HasValue)
            query = query.Where(x => x.r.CreatedOn >= q.From.Value);
        if (q.To.HasValue)
            query = query.Where(x => x.r.CreatedOn <= q.To.Value);

        query = query.OrderByDescending(x => x.r.CreatedOn);

        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, PaginationExtensions.MaxPageSize);

        var total = await query.LongCountAsync(ct).ConfigureAwait(false);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var resourceIds = items.Select(x => x.r.Id).ToList();
        var countryMap = await _db.Resources
            .Where(r => resourceIds.Contains(r.Id))
            .SelectMany(r => r.Countries.Select(rc => new { r.Id, rc.CountryId }))
            .GroupBy(x => x.Id)
            .ToDictionaryAsync(g => g.Key, g => g.Select(x => x.CountryId).ToArray(), ct)
            .ConfigureAwait(false);

        var dtos = items.Select(x => new ResourcesReportDto(
            x.r.Id,
            x.r.TitleAr,
            x.r.TitleEn,
            x.r.DescriptionAr,
            x.r.DescriptionEn,
            x.r.CategoryId,
            x.CategoryNameAr,
            x.CategoryNameEn,
            x.r.ResourceType,
            countryMap.GetValueOrDefault(x.r.Id, []),
            x.r.CreatedOn
        )).ToList();

        var paged = new PagedResult<ResourcesReportDto>(dtos, page, pageSize, total);
        return _msg.Ok(paged, MessageKeys.General.ITEMS_LISTED);
    }
}
