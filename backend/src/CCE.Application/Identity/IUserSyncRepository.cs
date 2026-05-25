namespace CCE.Application.Identity;

/// <summary>
/// Ensures a row exists in <c>users</c> for the given JWT sub, creating it with
/// role assignments derived from <c>groups</c> claims if missing.
/// Implemented in Infrastructure (writes via <c>CceDbContext</c>).
/// </summary>
public interface IUserSyncRepository
{
    Task EnsureUserExistsAsync(
        Guid userId,
        string email,
        string preferredUsername,
        IReadOnlyCollection<string> groupClaims,
        CancellationToken ct);
}
