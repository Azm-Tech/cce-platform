using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Api.Common.Auth;

/// <summary>
/// Sub-11d follow-up — dev-only sign-in / sign-out endpoints. Sets the
/// <c>cce-dev-role</c> cookie that <see cref="DevAuthHandler"/> reads.
/// Mounted only when <c>Auth:DevMode=true</c>.
///
/// Also stubs <c>GET /auth/login</c> + <c>POST /auth/logout</c> for
/// frontend compatibility — the SPA's <c>auth.service.signIn()</c> hits
/// <c>/auth/login</c> (legacy BFF surface deleted in Sub-11 Phase 04).
/// In dev mode, we redirect that to <c>/dev/sign-in</c> with the
/// configured default role, so the existing frontend flow keeps working.
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

        // ─── Frontend-compat shims at /auth/* ───────────────────────────
        // The SPA's auth.service.signIn() calls window.location.assign(
        // '/auth/login?returnUrl=...'). The legacy BFF surface that owned
        // this path was deleted in Sub-11 Phase 04. In DevMode we proxy
        // /auth/login → /dev/sign-in with the configured default role so
        // the existing frontend flow keeps working without rewiring.
        app.MapGet("/auth/login", (HttpContext ctx, string? returnUrl) =>
        {
            var config = ctx.RequestServices.GetRequiredService<IConfiguration>();
            var defaultRole = config.GetValue<string>("Auth:DefaultDevRole") ?? "cce-user";
            if (!DevAuthHandler.RoleToUserId.ContainsKey(defaultRole))
            {
                defaultRole = "cce-user";
            }
            var rurl = string.IsNullOrEmpty(returnUrl) || !returnUrl.StartsWith('/') ? "/" : returnUrl;
            var target = $"/dev/sign-in?role={Uri.EscapeDataString(defaultRole)}&returnUrl={Uri.EscapeDataString(rurl)}";
            return Results.Redirect(target);
        }).AllowAnonymous().WithName("AuthLoginShim");

        app.MapPost("/auth/logout", (HttpContext ctx) =>
        {
            ctx.Response.Cookies.Delete(DevAuthHandler.DevCookieName);
            return Results.Ok(new { signedOut = true });
        }).AllowAnonymous().WithName("AuthLogoutShim");

        return app;
    }
}
