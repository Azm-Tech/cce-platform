using CCE.Application.Content.Commands.CreateResource;
using CCE.Application.Content.Commands.UpdateResource;
using CCE.Application.Content.Queries.ListResources;
using CCE.Domain;
using CCE.Domain.Content;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class ResourceEndpoints
{
    public static IEndpointRouteBuilder MapResourceEndpoints(this IEndpointRouteBuilder app)
    {
        var resources = app.MapGroup("/api/admin/resources").WithTags("Resources");

        resources.MapGet("", async (
            int? page, int? pageSize, string? search,
            System.Guid? categoryId, System.Guid? countryId, bool? isPublished,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListResourcesQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                Search: search,
                CategoryId: categoryId,
                CountryId: countryId,
                IsPublished: isPublished);
            var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("ListResources");

        resources.MapPost("", async (
            CreateResourceRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new CreateResourceCommand(
                body.TitleAr, body.TitleEn,
                body.DescriptionAr, body.DescriptionEn,
                body.ResourceType, body.CategoryId, body.CountryId, body.AssetFileId);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/admin/resources/{dto.Id}", dto);
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("CreateResource");

        resources.MapPut("/{id:guid}", async (
            System.Guid id,
            UpdateResourceRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var rowVersion = string.IsNullOrEmpty(body.RowVersion)
                ? System.Array.Empty<byte>()
                : System.Convert.FromBase64String(body.RowVersion);
            var cmd = new UpdateResourceCommand(
                id,
                body.TitleAr, body.TitleEn,
                body.DescriptionAr, body.DescriptionEn,
                body.ResourceType, body.CategoryId,
                rowVersion);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.Resource_Center_Update)
        .WithName("UpdateResource");

        return app;
    }
}

public sealed record CreateResourceRequest(
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    CCE.Domain.Content.ResourceType ResourceType,
    System.Guid CategoryId,
    System.Guid? CountryId,
    System.Guid AssetFileId);

public sealed record UpdateResourceRequest(
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    CCE.Domain.Content.ResourceType ResourceType,
    System.Guid CategoryId,
    string RowVersion);
