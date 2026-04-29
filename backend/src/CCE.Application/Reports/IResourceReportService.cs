using CCE.Application.Reports.Rows;

namespace CCE.Application.Reports;

public interface IResourceReportService
{
    System.Collections.Generic.IAsyncEnumerable<ResourceReportRow> QueryAsync(
        System.DateTimeOffset? from,
        System.DateTimeOffset? to,
        CancellationToken ct);
}
