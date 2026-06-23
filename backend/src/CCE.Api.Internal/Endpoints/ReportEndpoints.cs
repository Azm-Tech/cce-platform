using CCE.Api.Common.Extensions;
using CCE.Application.Reports;
using CCE.Application.Reports.Queries.GetCommunityPostReport;
using CCE.Application.Reports.Queries.GetEventsReport;
using CCE.Application.Reports.Queries.GetExpertReport;
using CCE.Application.Reports.Queries.GetNewsReport;
using CCE.Application.Reports.Queries.GetSatisfactionSurveyReport;
using CCE.Application.Reports.Queries.GetUserPreferenceReport;
using CCE.Application.Reports.Queries.GetUserRegistrationReport;
using CCE.Domain;
using MediatR;
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

        reports.MapGet("/country-profiles.csv", async (
            HttpContext httpContext,
            System.DateTimeOffset? from,
            System.DateTimeOffset? to,
            ICountryProfilesReportService service,
            ICsvStreamWriter writer,
            CancellationToken cancellationToken) =>
        {
            var filename = $"country-profiles-{System.DateTimeOffset.UtcNow:yyyy-MM-dd}.csv";
            httpContext.Response.ContentType = "text/csv; charset=utf-8";
            httpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{filename}\"";
            var rows = service.QueryAsync(from, to, cancellationToken);
            await writer.WriteAsync(httpContext.Response.Body, rows, cancellationToken).ConfigureAwait(false);
        })
        .RequireAuthorization(Permissions.Report_CountryProfiles)
        .WithName("CountryProfilesReport");

        reports.MapGet("/user-registration", async (ISender sender) =>
        {
            var result = await sender.Send(new GetUserRegistrationReportQuery());
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Report_UserRegistrations)
        .WithName("UserRegistrationReport");

        reports.MapGet("/satisfaction-survey", async (ISender sender) =>
        {
            var result = await sender.Send(new GetSatisfactionSurveyReportQuery());
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Report_SatisfactionSurvey)
        .WithName("SatisfactionSurveyReportJson");

        reports.MapGet("/user-preferences", async (ISender sender) =>
        {
            var result = await sender.Send(new GetUserPreferenceReportQuery());
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Report_UserPreferences)
        .WithName("UserPreferenceReport");

        reports.MapGet("/experts", async (ISender sender) =>
        {
            var result = await sender.Send(new GetExpertReportQuery());
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Report_Experts)
        .WithName("ExpertReport");

        reports.MapGet("/news", async (
            ISender sender,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await sender.Send(new GetNewsReportQuery(from, to, page, pageSize));
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Report_News)
        .WithName("NewsReportJson");

        reports.MapGet("/community-posts", async (
            ISender sender,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await sender.Send(new GetCommunityPostReportQuery(from, to, page, pageSize));
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Report_CommunityPosts)
        .WithName("CommunityPostReportJson");

        reports.MapGet("/events", async (
            ISender sender,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await sender.Send(new GetEventsReportQuery(from, to, page, pageSize));
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Report_Events)
        .WithName("EventsReportJson");

        return app;
    }
}
