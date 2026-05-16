using CCE.Application.Identity.Public;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;

namespace CCE.Infrastructure.Identity;

public sealed class ExpertRequestSubmissionRepository
    : Repository<ExpertRegistrationRequest, System.Guid>, IExpertRequestSubmissionRepository
{
    public ExpertRequestSubmissionRepository(CceDbContext db) : base(db) { }
}
