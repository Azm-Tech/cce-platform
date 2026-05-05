using CCE.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;

namespace CCE.Api.Common.Auth;

public static class CceJwtAuthRegistration
{
    public static IServiceCollection AddCceJwtAuth(this IServiceCollection services, IConfiguration configuration)
    {
        // Sub-11d follow-up — DevMode shim. When Auth:DevMode=true, register
        // DevAuthHandler as the default scheme (replacing M.I.W's JwtBearer)
        // so local-dev sign-in works without a real Entra ID tenant.
        // Production deployments leave the flag false → DevAuth is never
        // registered + the production JwtBearer chain runs as before.
        var devMode = configuration.GetValue<bool>("Auth:DevMode");
        if (devMode)
        {
            services
                .AddAuthentication(opts =>
                {
                    opts.DefaultAuthenticateScheme = DevAuthHandler.SchemeName;
                    opts.DefaultChallengeScheme = DevAuthHandler.SchemeName;
                    opts.DefaultScheme = DevAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>(
                    DevAuthHandler.SchemeName, _ => { });
            services.AddHostedService<DevUsersSeeder>();
            services.Configure<EntraIdOptions>(configuration.GetSection(EntraIdOptions.SectionName));
            services.AddAuthorization();
            return services;
        }

        // Microsoft.Identity.Web layers on top of JwtBearer: registers the JwtBearer
        // scheme, points it at Entra ID's OIDC discovery endpoint, and pulls keys
        // from the JWKS automatically. configSectionName must match the JSON section
        // (EntraId:) in appsettings.json.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration, configSectionName: EntraIdOptions.SectionName);

        // Bind our strongly-typed options for downstream services to inject.
        services.Configure<EntraIdOptions>(configuration.GetSection(EntraIdOptions.SectionName));

        // Override JwtBearer options post-AddMicrosoftIdentityWebApi to enforce
        // multi-tenant issuer + roles claim type + match Sub-3-era pattern of
        // MapInboundClaims=false.
        services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, jwt =>
        {
            jwt.MapInboundClaims = false;

            jwt.TokenValidationParameters.NameClaimType = "preferred_username";
            jwt.TokenValidationParameters.RoleClaimType = "roles";

            // Multi-tenant: any Entra ID tenant's issuer is acceptable, as long as it
            // matches the canonical login.microsoftonline.com/<tenant>/v2.0 shape.
            jwt.TokenValidationParameters.ValidateIssuer = true;
            jwt.TokenValidationParameters.IssuerValidator = (issuer, _, _) => EntraIdIssuerValidator.Validate(issuer);

            // Audience validation re-enabled. Entra ID always issues an `aud` claim
            // matching the API's app ID URI (api://<application-id-guid>).
            jwt.TokenValidationParameters.ValidateAudience = true;

            jwt.TokenValidationParameters.ValidateLifetime = true;
            jwt.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(5);
        });

        services.AddAuthorization();
        return services;
    }
}
