using CCE.Application.Reports.Rows;

namespace CCE.Application.Reports;

public interface IUserRegistrationsReportService
{
    System.Collections.Generic.IAsyncEnumerable<UserRegistrationRow> QueryAsync(
        System.DateTimeOffset? from,
        System.DateTimeOffset? to,
        CancellationToken ct);
}
