using CCE.Application.Identity;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Identity;

public sealed class ExpertWorkflowRepository : IExpertWorkflowRepository
{
    private readonly CceDbContext _db;

    public ExpertWorkflowRepository(CceDbContext db)
    {
        _db = db;
    }

    public async Task<ExpertRegistrationRequest?> FindIncludingDeletedAsync(System.Guid id, CancellationToken ct)
    {
        return await _db.ExpertRegistrationRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task SaveAsync(ExpertRegistrationRequest request, ExpertProfile? newProfile, CancellationToken ct)
    {
        if (newProfile is not null)
        {
            _db.ExpertProfiles.Add(newProfile);
        }
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
