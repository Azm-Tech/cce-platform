using CCE.Api.Common.Extensions;
using CCE.Application.Content;
using CCE.Application.Content.Queries.DownloadFile;
using CCE.Application.Media.Commands.DeleteMedia;
using CCE.Application.Media.Commands.UploadMedia;
using CCE.Application.Media.Commands.UpdateMediaMetadata;
using CCE.Application.Media.Queries.GetMediaById;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class MediaEndpoints
{
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        var media = app.MapGroup("/api/media").WithTags("Media");

        media.MapPost("", async (
            IFormFile file,
            [FromForm] string? titleAr,
            [FromForm] string? titleEn,
            [FromForm] string? descriptionAr,
            [FromForm] string? descriptionEn,
            [FromForm] string? altTextAr,
            [FromForm] string? altTextEn,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await using var stream = file.OpenReadStream();
            var cmd = new UploadMediaCommand(
                stream, file.FileName, file.ContentType, file.Length,
                titleAr, titleEn, descriptionAr, descriptionEn, altTextAr, altTextEn);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToCreatedHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .DisableAntiforgery()
        .WithName("UploadMediaInternal");

        media.MapGet("{id:guid}", async (
            System.Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetMediaByIdQuery(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("GetMediaInternal");

        media.MapPut("{id:guid}", async (
            System.Guid id,
            UpdateMediaMetadataCommand body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var cmd = body with { Id = id };
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("UpdateMediaMetadataInternal");

        media.MapGet("{id:guid}/download", async (
            System.Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new DownloadFileQuery(id, DownloadFileType.Media), ct);
            return result.Success
                ? Results.File(result.Data!.Content, result.Data.MimeType, result.Data.OriginalFileName)
                : result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("DownloadMediaInternal");

        media.MapDelete("{id:guid}", async (
            System.Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteMediaCommand(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("DeleteMediaInternal");

        return app;
    }
}
