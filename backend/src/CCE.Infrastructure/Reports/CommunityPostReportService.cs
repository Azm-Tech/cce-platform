using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Reports;
using CCE.Application.Reports.Rows;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Reports;

public sealed class CommunityPostReportService : ICommunityPostReportService
{
    private readonly ICceDbContext _db;

    public CommunityPostReportService(ICceDbContext db)
    {
        _db = db;
    }

    public async System.Collections.Generic.IAsyncEnumerable<CommunityPostReportRow> QueryAsync(
        System.DateTimeOffset? from,
        System.DateTimeOffset? to,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var fromFilter = from;
        var toFilter = to;

        var query = _db.Posts.WithoutSoftDeleteFilter()
            .Where(p => (fromFilter == null || p.CreatedOn >= fromFilter) &&
                        (toFilter == null || p.CreatedOn <= toFilter))
            .Join(_db.Users,
                p => p.AuthorId,
                u => u.Id,
                (p, u) => new
                {
                    p.Id,
                    p.TopicId,
                    p.AuthorId,
                    AuthorName = u.UserName,
                    p.Locale,
                    p.IsAnswerable,
                    p.IsDeleted,
                    p.CreatedOn,
                });

        await foreach (var row in StreamAsAsyncEnumerable(query).WithCancellation(ct).ConfigureAwait(false))
        {
            yield return new CommunityPostReportRow
            {
                Id = row.Id,
                TopicId = row.TopicId,
                AuthorId = row.AuthorId,
                AuthorName = row.AuthorName,
                Locale = row.Locale,
                IsAnswerable = row.IsAnswerable,
                IsDeleted = row.IsDeleted,
                CreatedOn = row.CreatedOn,
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
