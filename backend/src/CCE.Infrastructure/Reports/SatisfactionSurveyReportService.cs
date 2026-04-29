using CCE.Application.Common.Interfaces;
using CCE.Application.Reports;
using CCE.Application.Reports.Rows;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Reports;

public sealed class SatisfactionSurveyReportService : ISatisfactionSurveyReportService
{
    private readonly ICceDbContext _db;

    public SatisfactionSurveyReportService(ICceDbContext db)
    {
        _db = db;
    }

    public async System.Collections.Generic.IAsyncEnumerable<SatisfactionSurveyReportRow> QueryAsync(
        System.DateTimeOffset? from,
        System.DateTimeOffset? to,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var fromFilter = from;
        var toFilter = to;

        var query = _db.ServiceRatings
            .Where(sr => (fromFilter == null || sr.SubmittedOn >= fromFilter) &&
                         (toFilter == null || sr.SubmittedOn <= toFilter))
            .Select(sr => new
            {
                sr.Id,
                sr.UserId,
                sr.Rating,
                sr.CommentAr,
                sr.CommentEn,
                sr.Page,
                sr.Locale,
                sr.SubmittedOn,
            });

        await foreach (var row in StreamAsAsyncEnumerable(query).WithCancellation(ct).ConfigureAwait(false))
        {
            yield return new SatisfactionSurveyReportRow
            {
                Id = row.Id,
                UserId = row.UserId,
                Rating = row.Rating,
                CommentAr = row.CommentAr,
                CommentEn = row.CommentEn,
                Page = row.Page,
                Locale = row.Locale,
                SubmittedOn = row.SubmittedOn,
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
