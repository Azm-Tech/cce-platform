using System.IO;
using CCE.Api.Common.Extensions;
using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.Content.Public;
using CCE.Application.Content.Public.Queries.GetPublicResourceById;
using CCE.Application.Content.Public.Queries.ListPublicResources;
using CCE.Domain.Content;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CCE.Api.External.Endpoints;

public static class ResourcesPublicEndpoints
{
    public static IEndpointRouteBuilder MapResourcesPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var resources = app.MapGroup("/api/resources").WithTags("Resources");

        resources.MapGet("", async (
            int? page, int? pageSize, string? search,
            System.Guid? categoryId, System.Guid? countryId, ResourceType? resourceType,
            System.Guid? knowledgeLevelId, System.Guid? jobSectorId,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListPublicResourcesQuery(
                Page: page ?? 1, PageSize: pageSize ?? 20,
                Search: search,
                CategoryId: categoryId, CountryId: countryId, ResourceType: resourceType,
                KnowledgeLevelId: knowledgeLevelId,
                JobSectorId: jobSectorId);
            var response = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("ListPublicResources");

        resources.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new GetPublicResourceByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetPublicResourceById");

        resources.MapGet("/{id:guid}/download", async (
            System.Guid id,
            HttpContext httpContext,
            ICceDbContext db,
            IFileStorageFactory storageFactory,
            IResourceViewCountRepository viewCounter,
            CancellationToken cancellationToken) =>
        {
            var resource = await db.Resources.FirstOrDefaultAsync(r => r.Id == id, cancellationToken).ConfigureAwait(false);
            if (resource is null || resource.PublishedOn is null)
                return Results.NotFound();

            var asset = await db.AssetFiles.FirstOrDefaultAsync(a => a.Id == resource.AssetFileId, cancellationToken).ConfigureAwait(false);
            if (asset is null)
                return Results.NotFound();
            if (asset.VirusScanStatus != VirusScanStatus.Clean)
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            httpContext.Response.ContentType = asset.MimeType;
            httpContext.Response.Headers.ContentDisposition =
                $"inline; filename=\"{System.Net.WebUtility.UrlEncode(asset.OriginalFileName)}\"";

            Stream fileStream;
            try
            {
                var storage = storageFactory.GetStorage(DownloadFileType.Asset);
                fileStream = await storage.OpenReadAsync(asset.Url, cancellationToken).ConfigureAwait(false);
            }
            catch (FileNotFoundException)
            {
                return Results.NotFound();
            }

            await using (fileStream)
            {
                await fileStream.CopyToAsync(httpContext.Response.Body, cancellationToken).ConfigureAwait(false);
            }

            _ = Task.Run(async () =>
            {
                try { await viewCounter.IncrementAsync(id, CancellationToken.None).ConfigureAwait(false); }
                catch (System.Exception) { /* best effort */ }
            }, CancellationToken.None);

            return Results.Empty;
        })
        .AllowAnonymous()
        .WithName("DownloadPublicResource");

        return app;
    }
}
