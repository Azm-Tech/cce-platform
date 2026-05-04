using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Api.Common.Auth;

public static class BffRegistration
{
    public static IServiceCollection AddCceBff(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BffOptions>()
            .Bind(configuration.GetSection(BffOptions.SectionName));
        services.AddDataProtection();
        services.AddSingleton<BffSessionCookie>();
        services.AddSingleton<BffTokenRefresher>();
        services.AddHttpClient("keycloak-bff");

        // Sub-11: lazy UPN→Entra ID objectId linker. Called from BFF's
        // OnTokenValidated event (Task 0.6 wires that). Scoped because it
        // depends on CceDbContext which is scoped.
        services.AddScoped<EntraIdUserResolver>();
        return services;
    }

    public static IApplicationBuilder UseCceBff(this IApplicationBuilder app)
        => app.UseMiddleware<BffSessionMiddleware>();
}
