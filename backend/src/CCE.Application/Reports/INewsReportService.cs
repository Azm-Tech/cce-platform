using CCE.Application.Reports.Rows;

namespace CCE.Application.Reports;

public interface INewsReportService
{
    System.Collections.Generic.IAsyncEnumerable<NewsReportRow> QueryAsync(
        System.DateTimeOffset? from,
        System.DateTimeOffset? to,
        CancellationToken ct);
}
