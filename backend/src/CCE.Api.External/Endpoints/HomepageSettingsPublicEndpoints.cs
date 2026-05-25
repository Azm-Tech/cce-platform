using CCE.Api.Common.Extensions;
using CCE.Application.PlatformSettings.Public.Queries.GetPublicHomepage;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class HomepageSettingsPublicEndpoints
{
    public static IEndpointRouteBuilder MapHomepageSettingsPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var homepage = app.MapGroup("/api/homepage").WithTags("Homepage");

        homepage.MapGet("", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPublicHomepageQuery(), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetPublicHomepage");

        return app;
    }
}
