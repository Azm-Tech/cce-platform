using CCE.Api.Common.Authorization;
using CCE.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Api.IntegrationTests.Authorization;

public class PermissionPolicyTests
{
    [Fact]
    public async Task Registers_policy_for_each_permission_in_All()
    {
        var services = new ServiceCollection();
        services.AddCcePermissionPolicies();
        await using var sp = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<IAuthorizationPolicyProvider>();

        foreach (var permission in Permissions.All)
        {
            var policy = await provider.GetPolicyAsync(permission);
            policy.Should().NotBeNull($"policy for permission {permission} should be registered");
        }
    }
}
