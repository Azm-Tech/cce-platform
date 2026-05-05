using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CCE.Api.IntegrationTests.Identity;

/// <summary>
/// Sub-11 — replaces the production JwtBearer / Microsoft.Identity.Web auth
/// chain in IntegrationTests. The pre-Sub-11 setup hit a real Keycloak via
/// <see cref="AdminAuthFixture"/> to mint a service-account token; Phase 04
/// deleted Keycloak so we now synthesize a principal in-process from the
/// bearer header value.
///
/// Convention: a request with <c>Authorization: Bearer cce-admin</c> resolves
/// to a <see cref="ClaimsPrincipal"/> with <c>roles=cce-admin</c>, the role
/// name doubling as the bearer-token value. Useful tokens:
/// <list type="bullet">
///   <item><c>cce-admin</c> — full admin permissions</item>
///   <item><c>cce-editor</c> — content-authoring permissions</item>
///   <item><c>cce-reviewer</c> — review-queue access</item>
///   <item><c>cce-expert</c> — expert-only access</item>
///   <item><c>cce-user</c> — base end-user role</item>
/// </list>
/// Any other token value resolves to an authenticated principal with that
/// literal role; unknown role values flow through
/// <see cref="CCE.Api.Common.Authorization.RoleToPermissionClaimsTransformer"/>
/// untouched (no permissions added).
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestAuth";

    /// <summary>Deterministic objectId injected into every test-auth principal.</summary>
    public static readonly Guid TestObjectId = Guid.Parse("00000000-0000-0000-0000-00000000beef");

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var raw = authHeader.ToString();
        if (!raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var role = raw.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrEmpty(role))
        {
            return Task.FromResult(AuthenticateResult.Fail("Empty bearer token"));
        }

        var claims = new[]
        {
            new Claim("oid", TestObjectId.ToString()),
            new Claim("preferred_username", $"{role}@cce.local"),
            new Claim("name", $"test-{role}"),
            new Claim("roles", role),
        };
        var identity = new ClaimsIdentity(claims, SchemeName, "preferred_username", "roles");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
