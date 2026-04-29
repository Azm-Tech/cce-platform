using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Reports;
using CCE.Application.Reports.Rows;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Reports;

public sealed class NewsReportService : INewsReportService
{
    private readonly ICceDbContext _db;

    public NewsReportService(ICceDbContext db)
    {
        _db = db;
    }

    public async System.Collections.Generic.IAsyncEnumerable<NewsReportRow> QueryAsync(
        System.DateTimeOffset? from,
        System.DateTimeOffset? to,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var fromFilter = from;
        var toFilter = to;

        var query = _db.News.WithoutSoftDeleteFilter()
            .Where(n => (fromFilter == null || (n.PublishedOn != null && n.PublishedOn >= fromFilter)) &&
                        (toFilter == null || (n.PublishedOn != null && n.PublishedOn <= toFilter)))
            .Join(_db.Users,
                n => n.AuthorId,
                u => u.Id,
                (n, u) => new
                {
                    n.Id,
                    n.TitleEn,
                    n.TitleAr,
                    n.Slug,
                    n.AuthorId,
                    AuthorName = u.UserName,
                    n.IsFeatured,
                    n.PublishedOn,
                });

        await foreach (var row in StreamAsAsyncEnumerable(query).WithCancellation(ct).ConfigureAwait(false))
        {
            yield return new NewsReportRow
            {
                Id = row.Id,
                TitleEn = row.TitleEn,
                TitleAr = row.TitleAr,
                Slug = row.Slug,
                AuthorId = row.AuthorId,
                AuthorName = row.AuthorName,
                IsPublished = row.PublishedOn is not null,
                IsFeatured = row.IsFeatured,
                PublishedOn = row.PublishedOn,
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
