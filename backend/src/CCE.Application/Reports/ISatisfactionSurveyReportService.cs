using CCE.Application.Reports.Rows;

namespace CCE.Application.Reports;

public interface ISatisfactionSurveyReportService
{
    System.Collections.Generic.IAsyncEnumerable<SatisfactionSurveyReportRow> QueryAsync(
        System.DateTimeOffset? from,
        System.DateTimeOffset? to,
        CancellationToken ct);
}
