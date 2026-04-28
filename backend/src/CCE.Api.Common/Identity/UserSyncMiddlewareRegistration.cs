using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Api.Common.Identity;

public static class UserSyncMiddlewareRegistration
{
    public static IServiceCollection AddCceUserSync(this IServiceCollection services)
    {
        services.AddMemoryCache();
        return services;
    }

    public static IApplicationBuilder UseCceUserSync(this IApplicationBuilder app) =>
        app.UseMiddleware<UserSyncMiddleware>();
}
