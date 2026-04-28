namespace CCE.Application.Identity;

/// <summary>
/// Replaces the role assignments for a user with the given set of role names.
/// Implemented in Infrastructure (writes via <c>CceDbContext</c>); handlers
/// stay clear of EF tracker calls.
/// </summary>
public interface IUserRoleAssignmentService
{
    /// <summary>
    /// Replaces user <paramref name="userId"/>'s role assignments. <paramref name="targetRoleNames"/>
    /// must be valid (all known roles); the service does NOT validate names — that's the validator's job.
    /// </summary>
    /// <returns><c>true</c> if the user exists and the operation completed; <c>false</c> if the user wasn't found.</returns>
    Task<bool> ReplaceRolesAsync(
        Guid userId,
        IReadOnlyCollection<string> targetRoleNames,
        CancellationToken ct);
}
