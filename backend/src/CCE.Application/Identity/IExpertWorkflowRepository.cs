using CCE.Domain.Identity;

namespace CCE.Application.Identity;

/// <summary>
/// Persistence helper for the expert-registration workflow. Implemented in Infrastructure
/// (writes via <c>CceDbContext</c>); handlers stay clear of EF tracker calls.
/// </summary>
public interface IExpertWorkflowRepository
{
    /// <summary>
    /// Loads the request by Id, including soft-deleted rows. Returns null when missing.
    /// </summary>
    Task<ExpertRegistrationRequest?> FindIncludingDeletedAsync(System.Guid id, CancellationToken ct);

    /// <summary>
    /// Persists in-memory mutations on a tracked request (Approve / Reject domain transitions)
    /// AND adds the new <paramref name="newProfile"/> if non-null. Single SaveChanges call.
    /// </summary>
    Task SaveAsync(ExpertRegistrationRequest request, ExpertProfile? newProfile, CancellationToken ct);
}
