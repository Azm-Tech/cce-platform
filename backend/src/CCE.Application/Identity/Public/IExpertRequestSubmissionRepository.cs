using CCE.Application.Common.Interfaces;
using CCE.Domain.Identity;

namespace CCE.Application.Identity.Public;

public interface IExpertRequestSubmissionRepository : IRepository<ExpertRegistrationRequest, System.Guid>
{
}
