using CCE.Api.Common.Extensions;
using CCE.Application.Identity.Auth.AdLogin;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class AdminAuthEndpoints
{
    public static IEndpointRouteBuilder MapAdminAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        auth.MapPost("/ad-login", async (
            AdLoginRequest body,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new AdLoginCommand(
                body.Username,
                body.Password,
                ctx.Connection.RemoteIpAddress?.ToString(),
                ctx.Request.Headers.UserAgent.ToString()), ct).ConfigureAwait(false);

            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("InternalAdLogin");

        return app;
    }
}
