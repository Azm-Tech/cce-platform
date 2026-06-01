using CCE.Api.Common.Extensions;
using CCE.Application.Content.Commands.CreateResourceCategory;
using CCE.Application.Content.Commands.DeleteResourceCategory;
using CCE.Application.Content.Commands.UpdateResourceCategory;
using CCE.Application.Content.Queries.GetResourceCategoryById;
using CCE.Application.Content.Queries.ListResourceCategories;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class ResourceCategoryEndpoints
{
    public static IEndpointRouteBuilder MapResourceCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var categories = app.MapGroup("/api/admin/resource-categories").WithTags("ResourceCategories");

        categories.MapGet("", async (
            int? page, int? pageSize, System.Guid? parentId, bool? isActive,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListResourceCategoriesQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                ParentId: parentId,
                IsActive: isActive);
            var response = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("ListResourceCategories");

        categories.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new GetResourceCategoryByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("GetResourceCategoryById");

        categories.MapPost("", async (
            CreateResourceCategoryRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new CreateResourceCategoryCommand(
                body.NameAr, body.NameEn, body.Slug, body.ParentId, body.OrderIndex);
            var response = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return response.ToCreatedHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("CreateResourceCategory");

        categories.MapPut("/{id:guid}", async (
            System.Guid id,
            UpdateResourceCategoryRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new UpdateResourceCategoryCommand(
                id, body.NameAr, body.NameEn, body.OrderIndex, body.IsActive);
            var response = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("UpdateResourceCategory");

        categories.MapDelete("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new DeleteResourceCategoryCommand(id), cancellationToken).ConfigureAwait(false);
            return response.ToNoContentHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Center_Upload)
        .WithName("DeleteResourceCategory");

        return app;
    }
}

public sealed record CreateResourceCategoryRequest(
    string NameAr,
    string NameEn,
    string Slug,
    System.Guid? ParentId,
    int OrderIndex);

public sealed record UpdateResourceCategoryRequest(
    string NameAr,
    string NameEn,
    int OrderIndex,
    bool IsActive);
