using CCE.Api.Common.Extensions;
using CCE.Application.Community.Public.Queries.ListPublicTopics;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class TopicsPublicEndpoints
{
    public static IEndpointRouteBuilder MapTopicsPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var topics = app.MapGroup("/api/topics").WithTags("TopicsPublic");

        topics.MapGet("", async (IMediator mediator, CancellationToken ct) =>
        {
            var response = await mediator.Send(new ListPublicTopicsQuery(), ct).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("ListPublicTopics");

        return app;
    }
}
