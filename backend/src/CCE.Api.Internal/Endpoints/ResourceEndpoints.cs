using CCE.Application.Content.Queries.ListResources;
using CCE.Domain;
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

        return app;
    }
}
