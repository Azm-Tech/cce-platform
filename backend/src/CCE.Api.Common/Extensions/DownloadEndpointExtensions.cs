using CCE.Application.Content.Download;
using CCE.Domain.Content;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Common.Extensions;

public static class DownloadEndpointExtensions
{
    public static IEndpointRouteBuilder MapDownloadEndpoints(
        this IEndpointRouteBuilder app,
        string prefix)
    {
        var group = app.MapGroup($"{prefix}/download").WithTags("Download");

        group.MapGet("{id:guid}", async (
            Guid id,
            int? type,
            DownloadServiceFactory factory,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            DownloadType downloadType = type switch
            {
                1 => DownloadType.Asset,
                2 => DownloadType.Image,
                _ => DownloadType.Asset
            };

            var service = factory.Create(downloadType);
            var result = await service.DownloadAsync(id, ct).ConfigureAwait(false);
            if (result is null)
                return Results.NotFound();

            httpContext.Response.ContentType = result.MimeType;
            httpContext.Response.Headers.ContentDisposition =
                $"inline; filename=\"{System.Net.WebUtility.UrlEncode(result.FileName)}\"";

            await result.Stream.CopyToAsync(httpContext.Response.Body, ct).ConfigureAwait(false);

            return Results.Empty;
        })
        .WithName("Download");

        return app;
    }
}
