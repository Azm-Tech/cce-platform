using CCE.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Api.Common.Authorization;

public static class PermissionPolicyRegistration
{
    public static IServiceCollection AddCcePermissionPolicies(this IServiceCollection services)
    {
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
