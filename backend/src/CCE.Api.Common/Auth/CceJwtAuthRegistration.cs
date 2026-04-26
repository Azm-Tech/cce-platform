using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CCE.Api.Common.Auth;

public sealed class CceJwtOptions
{
    public const string SectionName = "Keycloak";
    public string Authority { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public bool RequireHttpsMetadata { get; init; }

    /// <summary>
    /// Optional list of additional issuers accepted alongside <see cref="Authority"/>.
    /// Used in dev / load-test scenarios where Keycloak is reachable under multiple hostnames
    /// (e.g., <c>localhost:8080</c> on the host vs <c>host.docker.internal:8080</c> from a
    /// k6 container). Production should leave this empty so only the canonical authority is
    /// accepted.
    /// </summary>
    public IReadOnlyList<string> AdditionalValidIssuers { get; init; } = Array.Empty<string>();
}

public static class CceJwtAuthRegistration
{
    public static IServiceCollection AddCceJwtAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(CceJwtOptions.SectionName).Get<CceJwtOptions>()
            ?? throw new InvalidOperationException("Keycloak section missing from configuration.");

        var validIssuers = new List<string>(options.AdditionalValidIssuers.Count + 1) { options.Authority };
        foreach (var extra in options.AdditionalValidIssuers)
        {
            if (!string.IsNullOrWhiteSpace(extra))
            {
                validIssuers.Add(extra);
            }
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwt =>
            {
                jwt.Authority = options.Authority;
                jwt.Audience = options.Audience;
                jwt.RequireHttpsMetadata = options.RequireHttpsMetadata;
                jwt.MapInboundClaims = false;   // keep claim names exactly as Keycloak issues them
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Authority,
                    ValidIssuers = validIssuers,
                    ValidateAudience = false,   // Keycloak's user tokens often lack `aud`; we validate via `azp` instead
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),
                    NameClaimType = "preferred_username",
                    RoleClaimType = "groups"
                };
            });

        services.AddAuthorization();
        return services;
    }
}
