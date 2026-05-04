using CCE.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace CCE.Api.Common.Auth;

public static class BffRegistration
{
    private static readonly string[] DownstreamScopes = { "User.Read" };

    public static IServiceCollection AddCceBff(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BffOptions>()
            .Bind(configuration.GetSection(BffOptions.SectionName));
        services.AddDataProtection();
        services.AddSingleton<BffSessionCookie>();
        services.AddSingleton<BffTokenRefresher>();
        services.AddHttpClient("keycloak-bff");

        // Sub-11: lazy UPN→Entra ID objectId linker. Called from
        // OnTokenValidated event below. Scoped because it depends on
        // CceDbContext which is scoped.
        services.AddScoped<EntraIdUserResolver>();

        // Sub-11: Microsoft.Identity.Web layered on top of OpenIdConnect.
        // Registers the OIDC + Cookie auth schemes against multi-tenant
        // Entra ID. EnableTokenAcquisitionToCallDownstreamApi enables the
        // in-memory token cache that EntraIdRegistrationService (Phase 01)
        // uses for downstream Microsoft Graph calls.
        //
        // The existing custom BFF middleware (BffSessionMiddleware,
        // BffSessionCookie, BffTokenRefresher) coexists with M.I.W's
        // cookie scheme through Phase 03. Phase 04 deletes the custom
        // implementation once the cutover is verified stable.
        services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(configuration, configSectionName: EntraIdOptions.SectionName)
            .EnableTokenAcquisitionToCallDownstreamApi(DownstreamScopes)
            .AddInMemoryTokenCaches();

        services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, opts =>
        {
            // Hook the lazy UPN→objectId resolver to fire once per sign-in.
            // Don't block sign-in on resolver failure (resolver swallows
            // exceptions internally and logs them).
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

    public static IApplicationBuilder UseCceBff(this IApplicationBuilder app)
        => app.UseMiddleware<BffSessionMiddleware>();
}
