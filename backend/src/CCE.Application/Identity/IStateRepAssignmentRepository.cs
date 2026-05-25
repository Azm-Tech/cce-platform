using CCE.Application.Common.Interfaces;
using CCE.Domain.Identity;

namespace CCE.Application.Identity;

/// <summary>
/// Persists new <see cref="StateRepresentativeAssignment"/> aggregates and revokes existing ones.
/// </summary>
public interface IStateRepAssignmentRepository : IRepository<StateRepresentativeAssignment, System.Guid>
{
    /// <summary>
    /// Loads the assignment by Id, including soft-deleted (revoked) rows. Returns null when missing.
    /// </summary>
    Task<StateRepresentativeAssignment?> FindIncludingRevokedAsync(System.Guid id, CancellationToken ct);
}
