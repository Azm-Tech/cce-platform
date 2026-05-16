using CCE.Application.Common.Interfaces;
using CCE.Domain.Identity;

namespace CCE.Application.Identity;

/// <summary>
/// Persistence helper for the expert-registration workflow.
/// Tracking-only — handlers call <c>ICceDbContext.SaveChangesAsync</c> to commit.
/// </summary>
public interface IExpertWorkflowRepository : IRepository<ExpertRegistrationRequest, System.Guid>
{
    /// <summary>
    /// Loads the request by Id, including soft-deleted rows. Returns null when missing.
    /// </summary>
    Task<ExpertRegistrationRequest?> FindIncludingDeletedAsync(System.Guid id, CancellationToken ct);

    /// <summary>
    /// Registers a new <see cref="ExpertProfile"/> in the change tracker
    /// (created as a side-effect of approving an expert request).
    /// </summary>
    void AddProfile(ExpertProfile profile);
}
