using CCE.Api.Common.Extensions;
using CCE.Application.PlatformSettings.Public.Queries.GetPublicAboutSettings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class AboutSettingsPublicEndpoints
{
    public static IEndpointRouteBuilder MapAboutSettingsPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var about = app.MapGroup("/api/about").WithTags("About");

        about.MapGet("", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPublicAboutSettingsQuery(), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetPublicAboutSettings");

        return app;
    }
}
