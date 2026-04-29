using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Reports;
using CCE.Application.Reports.Rows;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Reports;

public sealed class EventReportService : IEventReportService
{
    private readonly ICceDbContext _db;

    public EventReportService(ICceDbContext db)
    {
        _db = db;
    }

    public async System.Collections.Generic.IAsyncEnumerable<EventReportRow> QueryAsync(
        System.DateTimeOffset? from,
        System.DateTimeOffset? to,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var fromFilter = from;
        var toFilter = to;

        var query = _db.Events.WithoutSoftDeleteFilter()
            .Where(e => (fromFilter == null || e.StartsOn >= fromFilter) &&
                        (toFilter == null || e.StartsOn <= toFilter))
            .Select(e => new
            {
                e.Id,
                e.TitleEn,
                e.TitleAr,
                e.StartsOn,
                e.EndsOn,
                e.LocationEn,
                e.OnlineMeetingUrl,
                e.ICalUid,
                e.IsDeleted,
            });

        await foreach (var row in StreamAsAsyncEnumerable(query).WithCancellation(ct).ConfigureAwait(false))
        {
            yield return new EventReportRow
            {
                Id = row.Id,
                TitleEn = row.TitleEn,
                TitleAr = row.TitleAr,
                StartsOn = row.StartsOn,
                EndsOn = row.EndsOn,
                LocationEn = row.LocationEn,
                OnlineMeetingUrl = row.OnlineMeetingUrl,
                ICalUid = row.ICalUid,
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
