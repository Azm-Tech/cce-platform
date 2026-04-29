using CCE.Application.Reports.Rows;

namespace CCE.Application.Reports;

public interface ICountryProfilesReportService
{
    System.Collections.Generic.IAsyncEnumerable<CountryProfileReportRow> QueryAsync(
        System.DateTimeOffset? from,
        System.DateTimeOffset? to,
        CancellationToken ct);
}
