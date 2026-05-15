using CCE.Domain.Identity;

namespace CCE.Application.Identity.Public;

public interface IExpertRequestSubmissionRepository
{
    Task SaveAsync(ExpertRegistrationRequest request, CancellationToken ct);
}
