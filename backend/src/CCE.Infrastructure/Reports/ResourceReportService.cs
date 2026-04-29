using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Reports;
using CCE.Application.Reports.Rows;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Reports;

public sealed class ResourceReportService : IResourceReportService
{
    private readonly ICceDbContext _db;

    public ResourceReportService(ICceDbContext db)
    {
        _db = db;
    }

    public async System.Collections.Generic.IAsyncEnumerable<ResourceReportRow> QueryAsync(
        System.DateTimeOffset? from,
        System.DateTimeOffset? to,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var fromFilter = from;
        var toFilter = to;

        var query = _db.Resources.WithoutSoftDeleteFilter()
            .Where(r => (fromFilter == null || (r.PublishedOn != null && r.PublishedOn >= fromFilter)) &&
                        (toFilter == null || (r.PublishedOn != null && r.PublishedOn <= toFilter)))
            .Select(r => new
            {
                r.Id,
                r.TitleEn,
                r.TitleAr,
                r.ResourceType,
                r.CategoryId,
                r.CountryId,
                r.PublishedOn,
                r.ViewCount,
                r.IsDeleted,
            });

        await foreach (var row in StreamAsAsyncEnumerable(query).WithCancellation(ct).ConfigureAwait(false))
        {
            yield return new ResourceReportRow
            {
                Id = row.Id,
                TitleEn = row.TitleEn,
                TitleAr = row.TitleAr,
                ResourceType = row.ResourceType.ToString(),
                CategoryId = row.CategoryId,
                CountryId = row.CountryId,
                IsCenterManaged = row.CountryId is null,
                IsPublished = row.PublishedOn is not null,
                PublishedOn = row.PublishedOn,
                ViewCount = row.ViewCount,
                IsDeleted = row.IsDeleted,
            };
        }
    }

    private static async System.Collections.Generic.IAsyncEnumerable<T> StreamAsAsyncEnumerable<T>(IQueryable<T> query)
    {
        if (query is System.Collections.Generic.IAsyncEnumerable<T> asyncEnum)
        {
            await foreach (var item in asyncEnum)
            {
                yield return item;
            }
        }
        else
        {
            foreach (var item in query)
            {
                yield return item;
            }
        }
    }
}
