using CCE.Application.Identity;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Identity;

public sealed class ExpertWorkflowRepository
    : Repository<ExpertRegistrationRequest, System.Guid>, IExpertWorkflowRepository
{
    public ExpertWorkflowRepository(CceDbContext db) : base(db) { }

    public async Task<ExpertRegistrationRequest?> FindIncludingDeletedAsync(System.Guid id, CancellationToken ct)
    {
        return await Db.ExpertRegistrationRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            .ConfigureAwait(false);
    }

    public void AddProfile(ExpertProfile profile)
    {
        Db.ExpertProfiles.Add(profile);
    }
}
