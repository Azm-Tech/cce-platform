namespace CCE.Application.Identity.Permissions;

public interface IRolePermissionRepository
{
    /// <summary>
    /// Atomically replaces the permission set for <paramref name="roleName"/> with
    /// <paramref name="desiredPermissions"/>. Writes audit rows for each grant/revoke.
    /// Returns <c>null</c> when the role does not exist.
    /// </summary>
    Task<RolePermissionsResult?> UpsertAsync(
        string roleName,
        IReadOnlySet<string> desiredPermissions,
        Guid actorId,
        string actorEmail,
        DateTimeOffset now,
        CancellationToken ct = default);
}

public sealed record RolePermissionsResult(
    string RoleName,
    IReadOnlyList<string> Permissions,
    int Granted,
    int Revoked,
    int Total);

public sealed record GrantRevokeResult(
    string RoleName,
    int Granted,
    int Revoked,
    int Total);
