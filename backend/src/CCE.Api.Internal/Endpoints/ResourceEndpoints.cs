using CCE.Api.Common.Extensions;
using CCE.Application.Content.Commands.CreateResource;
using CCE.Application.Content.Commands.DeleteResource;
using CCE.Application.Content.Commands.PublishResource;
using CCE.Application.Content.Commands.UpdateResource;
using CCE.Application.Content.Queries.GetResourceById;
using CCE.Application.Content.Queries.ListResources;
using CCE.Domain;
using CCE.Domain.Content;
using MediatR;
using Microsoft.AspNetCore.Builder;
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
            var response = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("ListResources");

        resources.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new GetResourceByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("GetResourceById");

        resources.MapPost("", async (
            CreateResourceRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new CreateResourceCommand(
                body.TitleAr, body.TitleEn,
                body.DescriptionAr, body.DescriptionEn,
                body.ResourceType, body.CategoryId, body.CountryId, body.AssetFileId,
                body.CountryIds);
            var response = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("CreateResource");

        resources.MapPut("/{id:guid}", async (
            System.Guid id,
            UpdateResourceRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new UpdateResourceCommand(
                id,
                body.TitleAr, body.TitleEn,
                body.DescriptionAr, body.DescriptionEn,
                body.ResourceType, body.CategoryId,
                body.CountryIds);
            var response = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Update)
        .WithName("UpdateResource");

        resources.MapPost("/{id:guid}/publish", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new PublishResourceCommand(id), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("PublishResource");

        resources.MapDelete("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new DeleteResourceCommand(id), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Delete)
        .WithName("DeleteResource");

        return app;
    }
}


