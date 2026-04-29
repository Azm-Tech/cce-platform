using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace CCE.Api.Common.Auth;

public static class BffAuthEndpoints
{
    private const string PkceCookieName = "cce.pkce";

    public static IEndpointRouteBuilder MapBffAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/auth").WithTags("Auth");

        auth.MapGet("/login", (
            string? returnUrl,
            HttpContext ctx,
            IOptions<BffOptions> opts) =>
        {
            var (verifier, challenge) = GeneratePkcePair();
            var state = $"{System.Guid.NewGuid():N}|{returnUrl ?? "/"}";

            ctx.Response.Cookies.Append(PkceCookieName, $"{verifier}|{state}", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax, // login redirect crosses sites
                IsEssential = true,
                MaxAge = System.TimeSpan.FromMinutes(5),
                Path = "/auth",
            });

            var o = opts.Value;
            var redirect = $"{ctx.Request.Scheme}://{ctx.Request.Host}/auth/callback";
            var authorizeUrl = $"{o.KeycloakBaseUrl}/realms/{o.KeycloakRealm}/protocol/openid-connect/auth"
                + $"?response_type=code"
                + $"&client_id={System.Uri.EscapeDataString(o.KeycloakClientId)}"
                + $"&redirect_uri={System.Uri.EscapeDataString(redirect)}"
                + $"&scope={System.Uri.EscapeDataString("openid profile email")}"
                + $"&code_challenge={System.Uri.EscapeDataString(challenge)}"
                + $"&code_challenge_method=S256"
                + $"&state={System.Uri.EscapeDataString(state)}";
            return Results.Redirect(authorizeUrl);
        })
        .AllowAnonymous()
        .WithName("BffLogin");

        auth.MapGet("/callback", async (
            string code,
            string state,
            HttpContext ctx,
            IHttpClientFactory httpFactory,
            BffSessionCookie cookie,
            IOptions<BffOptions> opts) =>
        {
            if (!ctx.Request.Cookies.TryGetValue(PkceCookieName, out var pkce) || string.IsNullOrEmpty(pkce))
            {
                return Results.BadRequest(new { error = "missing pkce cookie" });
            }
            ctx.Response.Cookies.Delete(PkceCookieName, new CookieOptions { Path = "/auth" });

            var parts = pkce.Split('|', 2);
            if (parts.Length != 2 || parts[1] != state)
            {
                return Results.BadRequest(new { error = "state mismatch" });
            }
            var verifier = parts[0];
            var stateParts = state.Split('|', 2);
            var returnUrl = stateParts.Length == 2 ? stateParts[1] : "/";

            var o = opts.Value;
            var redirect = $"{ctx.Request.Scheme}://{ctx.Request.Host}/auth/callback";
            var http = httpFactory.CreateClient("keycloak-bff");
            using var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirect),
                new KeyValuePair<string, string>("client_id", o.KeycloakClientId),
                new KeyValuePair<string, string>("client_secret", o.KeycloakClientSecret),
                new KeyValuePair<string, string>("code_verifier", verifier),
            });
            using var resp = await http.PostAsync(new System.Uri($"{o.KeycloakBaseUrl}/realms/{o.KeycloakRealm}/protocol/openid-connect/token"), form, ctx.RequestAborted).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return Results.BadRequest(new { error = "code exchange failed" });
            }
            var tokens = await resp.Content.ReadFromJsonAsync<BffTokenResponse>(cancellationToken: ctx.RequestAborted).ConfigureAwait(false);
            if (tokens is null)
            {
                return Results.BadRequest(new { error = "token response empty" });
            }

            cookie.Write(ctx, new BffSession(tokens.AccessToken, tokens.RefreshToken, System.DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn)));
            return Results.Redirect(returnUrl);
        })
        .AllowAnonymous()
        .WithName("BffCallback");

        auth.MapPost("/refresh", async (
            HttpContext ctx,
            BffSessionCookie cookie,
            BffTokenRefresher refresher) =>
        {
            var session = cookie.TryRead(ctx);
            if (session is null) return Results.Unauthorized();
            var refreshed = await refresher.TryRefreshAsync(session, ctx.RequestAborted).ConfigureAwait(false);
            if (refreshed is null)
            {
                cookie.Clear(ctx);
                return Results.Unauthorized();
            }
            cookie.Write(ctx, refreshed);
            return Results.Ok();
        })
        .AllowAnonymous()
        .WithName("BffRefresh");

        auth.MapPost("/logout", async (
            HttpContext ctx,
            BffSessionCookie cookie,
            IHttpClientFactory httpFactory,
            IOptions<BffOptions> opts) =>
        {
            var session = cookie.TryRead(ctx);
            cookie.Clear(ctx);
            if (session is not null)
            {
                var o = opts.Value;
                var http = httpFactory.CreateClient("keycloak-bff");
                using var form = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", o.KeycloakClientId),
                    new KeyValuePair<string, string>("client_secret", o.KeycloakClientSecret),
                    new KeyValuePair<string, string>("refresh_token", session.RefreshToken),
                });
                try
                {
                    using var _ = await http.PostAsync(new System.Uri($"{o.KeycloakBaseUrl}/realms/{o.KeycloakRealm}/protocol/openid-connect/logout"), form, ctx.RequestAborted).ConfigureAwait(false);
                }
                catch (System.Exception)
                {
                    // best effort; cookie is already cleared
                }
            }
            return Results.Ok();
        })
        .AllowAnonymous()
        .WithName("BffLogout");

        return app;
    }

    private static (string verifier, string challenge) GeneratePkcePair()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var verifier = Base64UrlTextEncoder.Encode(bytes);
        var challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        var challenge = Base64UrlTextEncoder.Encode(challengeBytes);
        return (verifier, challenge);
    }
}
