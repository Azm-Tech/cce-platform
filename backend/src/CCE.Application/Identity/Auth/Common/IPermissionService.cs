namespace CCE.Application.Identity.Auth.Common;

public interface IPermissionService
{
    Task<IReadOnlyList<string>> GetRolePermissionsAsync(string roleName, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetUserEffectivePermissionsAsync(Guid userId, CancellationToken ct = default);
    void InvalidateCacheForRole(string roleName);
    void InvalidateCacheForUser(Guid userId);
}
