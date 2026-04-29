using CCE.Application.Common.Interfaces;
using CCE.Application.Reports;
using CCE.Application.Reports.Rows;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Reports;

public sealed class ExpertReportService : IExpertReportService
{
    private readonly ICceDbContext _db;

    public ExpertReportService(ICceDbContext db)
    {
        _db = db;
    }

    public async System.Collections.Generic.IAsyncEnumerable<ExpertReportRow> QueryAsync(
        System.DateTimeOffset? from,
        System.DateTimeOffset? to,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var fromFilter = from;
        var toFilter = to;

        var query = _db.ExpertProfiles
            .Where(ep => (fromFilter == null || ep.ApprovedOn >= fromFilter) &&
                         (toFilter == null || ep.ApprovedOn <= toFilter))
            .Join(_db.Users,
                ep => ep.UserId,
                u => u.Id,
                (ep, u) => new
                {
                    ep.Id,
                    ep.UserId,
                    u.UserName,
                    ep.AcademicTitleEn,
                    ep.AcademicTitleAr,
                    ep.ExpertiseTags,
                    ep.ApprovedOn,
                });

        await foreach (var row in StreamAsAsyncEnumerable(query).WithCancellation(ct).ConfigureAwait(false))
        {
            yield return new ExpertReportRow
            {
                Id = row.Id,
                UserId = row.UserId,
                UserName = row.UserName,
                AcademicTitleEn = row.AcademicTitleEn,
                AcademicTitleAr = row.AcademicTitleAr,
                ExpertiseTags = string.Join("; ", row.ExpertiseTags ?? Enumerable.Empty<string>()),
                ApprovedOn = row.ApprovedOn,
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
