using CCE.Api.Common.Extensions;
using CCE.Application.CommunityLaws.Queries.GetCommunityLaws;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class CommunityLawsPublicEndpoints
{
    public static IEndpointRouteBuilder MapCommunityLawsPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/community-laws").WithTags("CommunityLaws");

        group.MapGet("", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCommunityLawsQuery(), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetCommunityLaws");

        return app;
    }
}
