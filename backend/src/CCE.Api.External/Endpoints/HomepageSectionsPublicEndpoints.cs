using CCE.Application.Content.Public.Queries.ListPublicHomepageSections;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class HomepageSectionsPublicEndpoints
{
    public static IEndpointRouteBuilder MapHomepageSectionsPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var sections = app.MapGroup("/api/homepage-sections").WithTags("HomepagePublic");

        sections.MapGet("", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListPublicHomepageSectionsQuery(), ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("ListPublicHomepageSections");

        return app;
    }
}
