using CCE.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace CCE.Api.Common.Auth;

/// <summary>
/// Sub-11 — registers Microsoft.Identity.Web's OpenIdConnect + Cookie
/// auth schemes against multi-tenant Entra ID, enables the in-memory
/// token cache for downstream Graph calls, and hooks the lazy
/// UPN→objectId resolver onto OnTokenValidated.
///
/// Pre-Sub-11 this file co-hosted a custom BFF (BffSessionMiddleware,
/// BffSessionCookie, BffTokenRefresher) for the Keycloak path — Phase
/// 04 deleted that surface; Microsoft.Identity.Web is now the only
/// auth path.
/// </summary>
public static class BffRegistration
{
    private static readonly string[] DownstreamScopes = { "User.Read" };

    public static IServiceCollection AddCceBff(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<EntraIdUserResolver>();

        services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(configuration, configSectionName: EntraIdOptions.SectionName)
            .EnableTokenAcquisitionToCallDownstreamApi(DownstreamScopes)
            .AddInMemoryTokenCaches();

        services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, opts =>
        {
            var existingOnTokenValidated = opts.Events.OnTokenValidated;
            opts.Events.OnTokenValidated = async ctx =>
            {
                if (existingOnTokenValidated is not null)
                {
                    await existingOnTokenValidated(ctx).ConfigureAwait(false);
                }
                var resolver = ctx.HttpContext.RequestServices.GetRequiredService<EntraIdUserResolver>();
                await resolver.EnsureLinkedAsync(ctx.Principal!).ConfigureAwait(false);
            };
        });

        return services;
    }
}
