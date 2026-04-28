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
        services.TryAddSingleton<IClaimsTransformation, RoleToPermissionClaimsTransformer>();

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
