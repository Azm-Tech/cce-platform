using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using CCE.Application.Identity.Auth.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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
        ["cce-super-admin"]   = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-000000000000"),
        ["cce-admin"]          = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-000000000001"),
        ["cce-content-manager"] = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-000000000002"),
        ["cce-state-representative"]      = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-000000000006"),
        ["cce-reviewer"]       = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-000000000003"),
        ["cce-expert"]         = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-000000000004"),
        ["cce-user"]           = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-000000000005"),
    };

    private readonly IOptions<LocalAuthOptions> _localAuthOptions;

    public DevAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<LocalAuthOptions> localAuthOptions)
        : base(options, logger, encoder)
    {
        _localAuthOptions = localAuthOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // PRIORITY 1: If the request carries a real JWT (e.g. from /api/auth/login),
        // authenticate as the real user and skip dev-mode entirely.
        var realJwtResult = TryAuthenticateRealJwt();
        if (realJwtResult is not null)
            return Task.FromResult(realJwtResult);

        // PRIORITY 2: Dev-mode auth — cookie or dev-prefixed bearer header.
        // Only reached when no valid real JWT is present.
        var roles = ReadDevRoles();
        if (roles is null || roles.Count == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Use the first recognised role for the deterministic userId lookup.
        var primaryRole = roles.FirstOrDefault(r => RoleToUserId.ContainsKey(r))
                          ?? roles[0];
        if (!RoleToUserId.TryGetValue(primaryRole, out var userId))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Unknown dev role '{primaryRole}'"));
        }

        var claims = new List<Claim>
        {
            new("sub", userId.ToString()),
            new("oid", userId.ToString()),
            new("preferred_username", $"{primaryRole}@cce.local"),
            new("name", $"Dev {primaryRole}"),
            new("email", $"{primaryRole}@cce.local"),
        };
        claims.AddRange(roles.Select(role => new Claim("roles", role)));

        var identity = new ClaimsIdentity(claims, SchemeName, "preferred_username", "roles");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Attempts to validate the Authorization header as a real JWT issued by
    /// <c>/api/auth/login</c>. Returns <c>null</c> when no header is present,
    /// the token is invalid, or it is a dev-mode token.
    /// </summary>
    private AuthenticateResult? TryAuthenticateRealJwt()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var auth))
            return null;

        var raw = auth.ToString();

        // Skip dev-prefixed tokens — they are handled by the dev-mode path.
        const string devPrefix = "Bearer dev:";
        if (raw.StartsWith(devPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        const string bearerPrefix = "Bearer ";
        if (!raw.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var token = raw.Substring(bearerPrefix.Length).Trim();

        var opts = _localAuthOptions.Value;
        var profiles = new[] { opts.External, opts.Internal };
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };

        foreach (var profile in profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.SigningKey))
                continue;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(profile.SigningKey));
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = profile.Issuer,
                ValidateAudience = true,
                ValidAudience = profile.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
            };

            ClaimsPrincipal principal;
            try
            {
                principal = handler.ValidateToken(token, parameters, out _);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "JWT validation failed for profile {Issuer} in DevAuthHandler", profile.Issuer);
                continue;
            }

            // Extract claims directly from the validated JWT — do NOT remap to dev users.
            var sub = principal.FindFirstValue("sub")
                      ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(sub))
                continue;

            var email = principal.FindFirstValue("email") ?? string.Empty;
            var preferredUsername = principal.FindFirstValue("preferred_username") ?? email;
            var name = principal.FindFirstValue("name")
                       ?? principal.FindFirstValue(ClaimTypes.Name)
                       ?? preferredUsername;

            var claims = new List<Claim>
            {
                new("sub", sub),
                new("oid", sub),
                new("preferred_username", preferredUsername),
                new("name", name),
                new("email", email),
            };
            claims.AddRange(principal.FindAll("roles").Select(c => new Claim("roles", c.Value)));

            var identity = new ClaimsIdentity(claims, SchemeName, "preferred_username", "roles");
            var realPrincipal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(realPrincipal, SchemeName);
            return AuthenticateResult.Success(ticket);
        }

        Logger.LogDebug("No valid real JWT found in DevAuthHandler; falling back to dev-mode auth");
        return null;
    }

    /// <summary>
    /// Reads dev-mode credentials from cookie or the <c>Bearer dev:&lt;role&gt;</c> header.
    /// Returns <c>null</c> when neither is present.
    /// </summary>
    private List<string>? ReadDevRoles()
    {
        // Prefer bearer header (curl / Postman) over cookie.
        if (Request.Headers.TryGetValue("Authorization", out var auth))
        {
            var raw = auth.ToString();
            const string devPrefix = "Bearer dev:";
            if (raw.StartsWith(devPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return new List<string> { raw.Substring(devPrefix.Length).Trim() };
            }
        }

        // Fall back to cookie (browser path).
        if (Request.Cookies.TryGetValue(DevCookieName, out var cookieValue) && !string.IsNullOrEmpty(cookieValue))
        {
            return new List<string> { cookieValue.Trim() };
        }

        return null;
    }
}
