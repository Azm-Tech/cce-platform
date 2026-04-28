using CCE.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CCE.Api.Common.Authorization;

public static class PermissionPolicyRegistration
{
    public static IServiceCollection AddCcePermissionPolicies(this IServiceCollection services)
    {
        // Use AddSingleton (not TryAddSingleton) so our transformer replaces the default
        // NoopClaimsTransformation registered by AddAuthentication(). AddCcePermissionPolicies is
        // called after AddCceJwtAuth, and TryAdd would silently do nothing since the Noop is
        // already registered. With AddSingleton, the last-registered implementation wins in
        // Microsoft.Extensions.DependencyInjection, giving us the real transformer.
        services.AddSingleton<IClaimsTransformation, RoleToPermissionClaimsTransformer>();

        services.AddAuthorization(opts =>
        {
            foreach (var permission in Permissions.All)
            {
                opts.AddPolicy(permission, policy => policy.RequireClaim("groups", permission));
            }
        });
        return services;
    }
}
