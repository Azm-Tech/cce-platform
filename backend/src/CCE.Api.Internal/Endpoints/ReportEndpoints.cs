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

        reports.MapGet("/experts.csv", async (
            HttpContext httpContext,
            System.DateTimeOffset? from,
            System.DateTimeOffset? to,
            IExpertReportService service,
            ICsvStreamWriter writer,
            CancellationToken cancellationToken) =>
        {
            var filename = $"experts-{System.DateTimeOffset.UtcNow:yyyy-MM-dd}.csv";
            httpContext.Response.ContentType = "text/csv; charset=utf-8";
            httpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{filename}\"";
            var rows = service.QueryAsync(from, to, cancellationToken);
            await writer.WriteAsync(httpContext.Response.Body, rows, cancellationToken).ConfigureAwait(false);
        })
        .RequireAuthorization(Permissions.Report_ExpertList)
        .WithName("ExpertsReport");

        reports.MapGet("/satisfaction-survey.csv", async (
            HttpContext httpContext,
            System.DateTimeOffset? from,
            System.DateTimeOffset? to,
            ISatisfactionSurveyReportService service,
            ICsvStreamWriter writer,
            CancellationToken cancellationToken) =>
        {
            var filename = $"satisfaction-survey-{System.DateTimeOffset.UtcNow:yyyy-MM-dd}.csv";
            httpContext.Response.ContentType = "text/csv; charset=utf-8";
            httpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{filename}\"";
            var rows = service.QueryAsync(from, to, cancellationToken);
            await writer.WriteAsync(httpContext.Response.Body, rows, cancellationToken).ConfigureAwait(false);
        })
        .RequireAuthorization(Permissions.Report_SatisfactionSurvey)
        .WithName("SatisfactionSurveyReport");

        reports.MapGet("/community-posts.csv", async (
            HttpContext httpContext,
            System.DateTimeOffset? from,
            System.DateTimeOffset? to,
            ICommunityPostReportService service,
            ICsvStreamWriter writer,
            CancellationToken cancellationToken) =>
        {
            var filename = $"community-posts-{System.DateTimeOffset.UtcNow:yyyy-MM-dd}.csv";
            httpContext.Response.ContentType = "text/csv; charset=utf-8";
            httpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{filename}\"";
            var rows = service.QueryAsync(from, to, cancellationToken);
            await writer.WriteAsync(httpContext.Response.Body, rows, cancellationToken).ConfigureAwait(false);
        })
        .RequireAuthorization(Permissions.Report_CommunityPosts)
        .WithName("CommunityPostsReport");

        reports.MapGet("/news.csv", async (
            HttpContext httpContext,
            System.DateTimeOffset? from,
            System.DateTimeOffset? to,
            INewsReportService service,
            ICsvStreamWriter writer,
            CancellationToken cancellationToken) =>
        {
            var filename = $"news-{System.DateTimeOffset.UtcNow:yyyy-MM-dd}.csv";
            httpContext.Response.ContentType = "text/csv; charset=utf-8";
            httpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{filename}\"";
            var rows = service.QueryAsync(from, to, cancellationToken);
            await writer.WriteAsync(httpContext.Response.Body, rows, cancellationToken).ConfigureAwait(false);
        })
        .RequireAuthorization(Permissions.Report_News)
        .WithName("NewsReport");

        reports.MapGet("/events.csv", async (
            HttpContext httpContext,
            System.DateTimeOffset? from,
            System.DateTimeOffset? to,
            IEventReportService service,
            ICsvStreamWriter writer,
            CancellationToken cancellationToken) =>
        {
            var filename = $"events-{System.DateTimeOffset.UtcNow:yyyy-MM-dd}.csv";
            httpContext.Response.ContentType = "text/csv; charset=utf-8";
            httpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{filename}\"";
            var rows = service.QueryAsync(from, to, cancellationToken);
            await writer.WriteAsync(httpContext.Response.Body, rows, cancellationToken).ConfigureAwait(false);
        })
        .RequireAuthorization(Permissions.Report_Events)
        .WithName("EventsReport");

        reports.MapGet("/resources.csv", async (
            HttpContext httpContext,
            System.DateTimeOffset? from,
            System.DateTimeOffset? to,
            IResourceReportService service,
            ICsvStreamWriter writer,
            CancellationToken cancellationToken) =>
        {
            var filename = $"resources-{System.DateTimeOffset.UtcNow:yyyy-MM-dd}.csv";
            httpContext.Response.ContentType = "text/csv; charset=utf-8";
            httpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{filename}\"";
            var rows = service.QueryAsync(from, to, cancellationToken);
            await writer.WriteAsync(httpContext.Response.Body, rows, cancellationToken).ConfigureAwait(false);
        })
        .RequireAuthorization(Permissions.Report_Resources)
        .WithName("ResourcesReport");

        return app;
    }
}
