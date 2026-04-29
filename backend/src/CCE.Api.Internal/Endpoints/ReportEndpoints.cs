using CCE.Application.Reports;
using CCE.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var reports = app.MapGroup("/api/admin/reports").WithTags("Reports");

        reports.MapGet("/users-registrations.csv", async (
            HttpContext httpContext,
            System.DateTimeOffset? from,
            System.DateTimeOffset? to,
            IUserRegistrationsReportService service,
            ICsvStreamWriter writer,
            CancellationToken cancellationToken) =>
        {
            var filename = $"users-registrations-{System.DateTimeOffset.UtcNow:yyyy-MM-dd}.csv";
            httpContext.Response.ContentType = "text/csv; charset=utf-8";
            httpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{filename}\"";
            var rows = service.QueryAsync(from, to, cancellationToken);
            await writer.WriteAsync(httpContext.Response.Body, rows, cancellationToken).ConfigureAwait(false);
        })
        .RequireAuthorization(Permissions.Report_UserRegistrations)
        .WithName("UsersRegistrationsReport");

        return app;
    }
}
