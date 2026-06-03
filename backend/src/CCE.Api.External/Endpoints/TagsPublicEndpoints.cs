using CCE.Api.Common.Extensions;
using CCE.Application.Content.Public.Queries.ListPublicTags;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class TagsPublicEndpoints
{
    public static IEndpointRouteBuilder MapTagsPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var tags = app.MapGroup("/api/tags").WithTags("Tags");

        tags.MapGet("", async (
            [FromQuery] string? search,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new ListPublicTagsQuery(search), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("ListPublicTags");

        return app;
    }
}
