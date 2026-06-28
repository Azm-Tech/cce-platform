using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Api.Common.Authorization;

public static class PermissionPolicyRegistration
{
    public static IServiceCollection AddCcePermissionPolicies(this IServiceCollection services)
    {
        // Use AddSingleton (not TryAddSingleton) so our transformer replaces the default
        // NoopClaimsTransformation registered by AddAuthentication().
        services.AddSingleton<IClaimsTransformation, RoleToPermissionClaimsTransformer>();

        // DynamicPermissionPolicyProvider resolves any dotted policy name as a
        // RequireClaim("groups", name) check — no pre-registration loop needed.
        services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();

        services.AddAuthorization();
        return services;
    }
}
