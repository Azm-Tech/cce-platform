using CCE.Domain.Identity;

namespace CCE.Application.Identity;

/// <summary>
/// Persists new <see cref="StateRepresentativeAssignment"/> aggregates and revokes existing ones.
/// Implemented in Infrastructure (writes via <c>CceDbContext</c>).
/// </summary>
public interface IStateRepAssignmentRepository
{
    /// <summary>
    /// Persists the provided assignment. Caller is responsible for constructing it via
    /// <see cref="StateRepresentativeAssignment.Assign"/>. Throws <c>DuplicateException</c>
    /// if the (UserId, CountryId) pair already has an active assignment (filtered unique
    /// index in the schema).
    /// </summary>
    Task SaveAsync(StateRepresentativeAssignment assignment, CancellationToken ct);

    /// <summary>
    /// Loads the assignment by Id, including soft-deleted (revoked) rows. Returns null when missing.
    /// Used by the revoke command to load before mutating.
    /// </summary>
    Task<StateRepresentativeAssignment?> FindIncludingRevokedAsync(System.Guid id, CancellationToken ct);

    /// <summary>
    /// Persists the in-memory state of the assignment after domain mutations
    /// (e.g., <see cref="StateRepresentativeAssignment.Revoke"/>).
    /// </summary>
    Task UpdateAsync(StateRepresentativeAssignment assignment, CancellationToken ct);
}
