using CCE.Domain.Identity;

namespace CCE.Application.Identity.Public;

public interface IExpertRequestSubmissionService
{
    Task SaveAsync(ExpertRegistrationRequest request, CancellationToken ct);
}
