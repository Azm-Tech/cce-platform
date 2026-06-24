using CCE.Api.Common.Extensions;
using CCE.Api.Common.Results;
using CCE.Application.Content;
using CCE.Application.Content.Commands.UploadAsset;
using CCE.Application.Content.Queries.DownloadFile;
using CCE.Application.Content.Queries.GetAssetById;
using CCE.Domain;
using CCE.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace CCE.Api.Internal.Endpoints;

public static class AssetEndpoints
{
    private const long MaxRequestSizeBytes = 100L * 1024L * 1024L; // 100 MB

    public static IEndpointRouteBuilder MapAssetEndpoints(this IEndpointRouteBuilder app)
    {
        var assets = app.MapGroup("/api/admin/assets").WithTags("Assets");

        assets.MapPost("", async (
            HttpContext httpContext,
            IMediator mediator,
            IOptions<CceInfrastructureOptions> infraOpts,
            CancellationToken cancellationToken) =>
        {
            if (!httpContext.Request.HasFormContentType)
                return EnvelopeResults.BadRequest();

            var form = await httpContext.Request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
            var file = form.Files["file"] ?? (form.Files.Count > 0 ? form.Files[0] : null);
            if (file is null || file.Length == 0)
                return EnvelopeResults.BadRequest();

            var allowed = infraOpts.Value.AllowedAssetMimeTypes;
            if (!allowed.Contains(file.ContentType, System.StringComparer.OrdinalIgnoreCase))
                return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);

            await using var stream = file.OpenReadStream();
            var result = await mediator.Send(
                new UploadAssetCommand(stream, file.FileName, file.ContentType, file.Length),
                cancellationToken).ConfigureAwait(false);

            return result.Success
                ? Results.Created($"/api/admin/assets/{result.Data!.Id}", result)
                : result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("UploadAsset")
        .DisableAntiforgery()
        .WithMetadata(new RequestSizeLimitMetadataImpl(MaxRequestSizeBytes));

        assets.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetAssetByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("GetAssetById");

        assets.MapGet("/{id:guid}/download", async (
            System.Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new DownloadFileQuery(id, DownloadFileType.Asset), ct);
            return result.Success
                ? Results.File(result.Data!.Content, result.Data.MimeType, result.Data.OriginalFileName)
                : result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("DownloadAsset");

        return app;
    }

    private sealed class RequestSizeLimitMetadataImpl : Microsoft.AspNetCore.Http.Metadata.IRequestSizeLimitMetadata
    {
        public RequestSizeLimitMetadataImpl(long? max) { MaxRequestBodySize = max; }
        public long? MaxRequestBodySize { get; }
    }
}
