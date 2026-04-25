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
}

public static class CceJwtAuthRegistration
{
    public static IServiceCollection AddCceJwtAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(CceJwtOptions.SectionName).Get<CceJwtOptions>()
            ?? throw new InvalidOperationException("Keycloak section missing from configuration.");

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
