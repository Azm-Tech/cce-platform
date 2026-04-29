using CCE.Application.Reports.Rows;

namespace CCE.Application.Reports;

public interface IExpertReportService
{
    System.Collections.Generic.IAsyncEnumerable<ExpertReportRow> QueryAsync(
        System.DateTimeOffset? from,
        System.DateTimeOffset? to,
        CancellationToken ct);
}
