using CCE.Application.Reports.Rows;

namespace CCE.Application.Reports;

public interface IEventReportService
{
    System.Collections.Generic.IAsyncEnumerable<EventReportRow> QueryAsync(
        System.DateTimeOffset? from,
        System.DateTimeOffset? to,
        CancellationToken ct);
}
