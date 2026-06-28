using System.Text;
using CCE.Api.Common.Results;
using CCE.Application.Identity.Auth.Common;
using CCE.Application.Messages;
using CCE.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CCE.Api.Common.Auth;

public static class CceJwtAuthRegistration
{
    public static IServiceCollection AddCceJwtAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        LocalAuthApi api = LocalAuthApi.External)
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
            services.Configure<LocalAuthOptions>(configuration.GetSection(LocalAuthOptions.SectionName));
            services.AddHostedService<DevUsersSeeder>();
            services.Configure<EntraIdOptions>(configuration.GetSection(EntraIdOptions.SectionName));
            services.AddAuthorization();
            return services;
        }

        var authOptions = configuration.GetSection(LocalAuthOptions.SectionName).Get<LocalAuthOptions>() ?? new LocalAuthOptions();
        var profile = authOptions.GetProfile(api);
        ValidateProfile(profile, api);

        services.Configure<LocalAuthOptions>(configuration.GetSection(LocalAuthOptions.SectionName));
        services.Configure<EntraIdOptions>(configuration.GetSection(EntraIdOptions.SectionName));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = profile.Issuer,
                    ValidateAudience = true,
                    ValidAudience = profile.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(profile.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles",
                };
                // SignalR browser WebSocket clients can't set the Authorization header — they pass the JWT
                // via ?access_token=. Accept it for hub requests so the hub authenticates over WebSockets.
                // OnChallenge/OnForbidden write the standard CCE error envelope instead of the default
                // empty 401/403 body, keeping the WWW-Authenticate header on 401 (RFC 6750).
                jwt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"].ToString();
                        if (!string.IsNullOrEmpty(accessToken)
            && context.HttpContext.Request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.Headers.WWWAuthenticate = "Bearer";
                        await EnvelopeWriter.WriteAsync(
                            context.HttpContext,
                            StatusCodes.Status401Unauthorized,
                            MessageKeys.General.UNAUTHORIZED,
                            context.AuthenticateFailure?.Message).ConfigureAwait(false);
                    },
                    OnForbidden = async context =>
                    {
                        await EnvelopeWriter.WriteAsync(
                            context.HttpContext,
                            StatusCodes.Status403Forbidden,
                            MessageKeys.General.FORBIDDEN).ConfigureAwait(false);
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }

    private static void ValidateProfile(LocalAuthJwtProfile profile, LocalAuthApi api)
    {
        if (string.IsNullOrWhiteSpace(profile.Issuer)
            || string.IsNullOrWhiteSpace(profile.Audience)
            || Encoding.UTF8.GetByteCount(profile.SigningKey) < 32)
        {
            throw new InvalidOperationException(
                $"LocalAuth:{api} requires Issuer, Audience, and a 32+ byte SigningKey.");
        }
    }
}
