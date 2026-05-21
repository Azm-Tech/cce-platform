using CCE.Api.Common.Extensions;
using CCE.Application.PlatformSettings.Public.Queries.GetPublicPoliciesSettings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class PoliciesSettingsPublicEndpoints
{
    public static IEndpointRouteBuilder MapPoliciesSettingsPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var policies = app.MapGroup("/api/policies").WithTags("Policies");

        policies.MapGet("", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetPublicPoliciesSettingsQuery(), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetPublicPoliciesSettings");

        return app;
    }
}
