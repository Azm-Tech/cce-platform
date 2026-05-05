using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CCE.Api.Common.Auth;

/// <summary>
/// Sub-11d follow-up — dev-mode auth shim. Sub-11 deleted Keycloak;
/// the production auth path now requires a real Entra ID tenant. This
/// handler restores a working local-dev path WITHOUT depending on any
/// external IdP.
///
/// Activation:
/// <list type="bullet">
///   <item>Only registered when <c>Auth:DevMode=true</c> in config.</item>
///   <item>Default is false — production deployments never see this code path.</item>
/// </list>
///
/// Recognizes auth in two ways:
/// <list type="bullet">
///   <item>Header <c>Authorization: Bearer dev:&lt;role&gt;</c> — for API
///   tests via curl / Postman.</item>
///   <item>Cookie <c>cce-dev-role=&lt;role&gt;</c> — set by
///   <c>GET /dev/sign-in?role=&lt;role&gt;</c>; consumed by the SPA.</item>
/// </list>
///
/// Synthesizes a <see cref="ClaimsPrincipal"/> with deterministic
/// <c>sub</c> + <c>oid</c> claims keyed by role so the same role always
/// resolves to the same CCE.DB User row (created by
/// <c>DevUsersSeeder</c> at app startup).
/// </summary>
public sealed class DevAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DevAuth";
    public const string DevCookieName = "cce-dev-role";

    /// <summary>
    /// Deterministic User.Id per dev role. <c>DevUsersSeeder</c> creates
    /// matching CCE.DB rows; the same Guid lands in the synthesized
    /// principal's <c>sub</c> claim so <c>ICurrentUserAccessor.GetUserId()</c>
    /// resolves cleanly.
    /// </summary>
    public static readonly Dictionary<string, Guid> RoleToUserId = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cce-admin"]    = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-000000000001"),
        ["cce-editor"]   = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-000000000002"),
        ["cce-reviewer"] = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-000000000003"),
        ["cce-expert"]   = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-000000000004"),
        ["cce-user"]     = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-000000000005"),
    };

    public DevAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = ReadRole();
        if (string.IsNullOrEmpty(role))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!RoleToUserId.TryGetValue(role, out var userId))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Unknown dev role '{role}'"));
        }

        var claims = new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim("oid", userId.ToString()),
            new Claim("preferred_username", $"{role}@cce.local"),
            new Claim("name", $"Dev {role}"),
            new Claim("roles", role),
            new Claim("email", $"{role}@cce.local"),
        };
        var identity = new ClaimsIdentity(claims, SchemeName, "preferred_username", "roles");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private string? ReadRole()
    {
        // Prefer cookie (browser path); fall back to bearer header (curl / Postman).
        if (Request.Cookies.TryGetValue(DevCookieName, out var cookieValue) && !string.IsNullOrEmpty(cookieValue))
        {
            return cookieValue.Trim();
        }

        if (Request.Headers.TryGetValue("Authorization", out var auth))
        {
            var raw = auth.ToString();
            const string devPrefix = "Bearer dev:";
            if (raw.StartsWith(devPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return raw.Substring(devPrefix.Length).Trim();
            }
        }

        return null;
    }
}
