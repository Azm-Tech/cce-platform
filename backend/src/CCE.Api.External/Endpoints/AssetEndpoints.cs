using CCE.Api.Common.Auth;
using CCE.Api.Common.Extensions;
using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Commands.UploadAsset;
using CCE.Application.Content.Queries.GetAssetById;
using CCE.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace CCE.Api.External.Endpoints;

public static class AssetEndpoints
{
    public static IEndpointRouteBuilder MapAssetEndpoints(this IEndpointRouteBuilder app)
    {
        var assets = app.MapGroup("/api/assets").WithTags("Assets").RequireAuthorization();

        assets.MapPost("", async (
            IFormFile file,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            IOptions<CceInfrastructureOptions> infraOpts,
            CancellationToken ct) =>
        {
            if (currentUser.GetUserId() is null) return Results.Unauthorized();

            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "Upload requires a non-empty file." });

            var allowed = infraOpts.Value.AllowedAssetMimeTypes;
            if (!allowed.Contains(file.ContentType, System.StringComparer.OrdinalIgnoreCase))
                return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);

            await using var stream = file.OpenReadStream();
            var result = await mediator.Send(
                new UploadAssetCommand(stream, file.FileName, file.ContentType, file.Length),
                ct).ConfigureAwait(false);

            return result.Success
                ? Results.Created($"/api/assets/{result.Data!.Id}", result)
                : result.ToHttpResult();
        })
        .WithName("UploadAsset")
        .DisableAntiforgery()
        .WithMetadata(new RequestSizeLimitMetadataImpl(20L * 1024L * 1024L));

        assets.MapGet("{id:guid}", async (
            System.Guid id,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();

            var result = await mediator.Send(new GetAssetByIdQuery(id), ct).ConfigureAwait(false);

            if (!result.Success || result.Data!.UploadedById != userId)
                return Results.NotFound();

            return result.ToHttpResult();
        })
        .WithName("GetAssetById");

        return app;
    }

    private sealed class RequestSizeLimitMetadataImpl : Microsoft.AspNetCore.Http.Metadata.IRequestSizeLimitMetadata
    {
        public RequestSizeLimitMetadataImpl(long? max) { MaxRequestBodySize = max; }
        public long? MaxRequestBodySize { get; }
    }
}
