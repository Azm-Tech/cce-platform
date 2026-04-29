using CCE.Application.Search;
using CCE.Application.Search.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var search = app.MapGroup("/api/search").WithTags("Search");

        search.MapGet("", async (
            string q, SearchableType? type, int? page, int? pageSize,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new SearchQuery(q ?? string.Empty, type, page ?? 1, pageSize ?? 20);
            var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("Search");

        return app;
    }
}
