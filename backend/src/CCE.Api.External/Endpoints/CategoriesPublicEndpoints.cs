using CCE.Application.Content.Public.Queries.ListPublicResourceCategories;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class CategoriesPublicEndpoints
{
    public static IEndpointRouteBuilder MapCategoriesPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var categories = app.MapGroup("/api/categories").WithTags("CategoriesPublic");

        categories.MapGet("", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListPublicResourceCategoriesQuery(), ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("ListPublicCategories");

        return app;
    }
}
