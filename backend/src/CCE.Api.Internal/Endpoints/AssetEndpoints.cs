using CCE.Application.Content.Commands.UploadAsset;
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
            {
                return Results.BadRequest(new { error = "Multipart form-data with a single 'file' field is required." });
            }
            var form = await httpContext.Request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
            var file = form.Files["file"] ?? (form.Files.Count > 0 ? form.Files[0] : null);
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "Upload requires a non-empty 'file' field." });
            }

            var allowed = infraOpts.Value.AllowedAssetMimeTypes;
            if (!allowed.Contains(file.ContentType, System.StringComparer.OrdinalIgnoreCase))
            {
                return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
            }

            await using var stream = file.OpenReadStream();
            var dto = await mediator.Send(
                new UploadAssetCommand(stream, file.FileName, file.ContentType, file.Length),
                cancellationToken).ConfigureAwait(false);

            return Results.Created($"/api/admin/assets/{dto.Id}", dto);
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("UploadAsset")
        .DisableAntiforgery()
        .WithMetadata(new RequestSizeLimitMetadataImpl(MaxRequestSizeBytes));

        assets.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var dto = await mediator.Send(new GetAssetByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("GetAssetById");

        return app;
    }

    private sealed class RequestSizeLimitMetadataImpl : Microsoft.AspNetCore.Http.Metadata.IRequestSizeLimitMetadata
    {
        public RequestSizeLimitMetadataImpl(long? max) { MaxRequestBodySize = max; }
        public long? MaxRequestBodySize { get; }
    }
}
