using CCE.Application.Reports.Rows;

namespace CCE.Application.Reports;

public interface ICommunityPostReportService
{
    System.Collections.Generic.IAsyncEnumerable<CommunityPostReportRow> QueryAsync(
        System.DateTimeOffset? from,
        System.DateTimeOffset? to,
        CancellationToken ct);
}
