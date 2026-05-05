using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Common.Auth;

/// <summary>
/// Sub-11d follow-up — dev-only sign-in / sign-out endpoints. Sets the
/// <c>cce-dev-role</c> cookie that <see cref="DevAuthHandler"/> reads.
/// Mounted only when <c>Auth:DevMode=true</c>.
/// </summary>
public static class DevAuthEndpoints
{
    public static IEndpointRouteBuilder MapDevAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var dev = app.MapGroup("/dev").WithTags("DevAuth");

        dev.MapGet("/sign-in", (HttpContext ctx, string? role, string? returnUrl) =>
        {
            var roleValue = role ?? "cce-admin";
            if (!DevAuthHandler.RoleToUserId.ContainsKey(roleValue))
            {
                return Results.BadRequest(new
                {
                    error = $"Unknown dev role '{roleValue}'.",
                    validRoles = DevAuthHandler.RoleToUserId.Keys,
                });
            }

            ctx.Response.Cookies.Append(DevAuthHandler.DevCookieName, roleValue, new CookieOptions
            {
                HttpOnly = false, // SPA can inspect for diagnostics; cookie itself isn't a secret in dev
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(7),
            });

            // Redirect to returnUrl if relative + safe; else home.
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/'))
            {
                return Results.Redirect(returnUrl);
            }
            return Results.Redirect("/");
        }).AllowAnonymous().WithName("DevSignIn");

        dev.MapPost("/sign-out", (HttpContext ctx) =>
        {
            ctx.Response.Cookies.Delete(DevAuthHandler.DevCookieName);
            return Results.Ok(new { signedOut = true });
        }).AllowAnonymous().WithName("DevSignOut");

        dev.MapGet("/whoami", (HttpContext ctx) =>
        {
            var name = ctx.User.Identity?.Name ?? "(anonymous)";
            var roles = ctx.User.FindAll("roles").Select(c => c.Value).ToArray();
            var sub = ctx.User.FindFirst("sub")?.Value ?? "(none)";
            return Results.Ok(new { name, sub, roles });
        }).AllowAnonymous().WithName("DevWhoAmI");

        return app;
    }
}
