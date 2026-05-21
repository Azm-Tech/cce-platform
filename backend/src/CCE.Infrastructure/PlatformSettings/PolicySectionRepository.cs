using CCE.Application.PlatformSettings;
using CCE.Domain.PlatformSettings;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.PlatformSettings;

public sealed class PolicySectionRepository : IPolicySectionRepository
{
    private readonly CceDbContext _db;

    public PolicySectionRepository(CceDbContext db) => _db = db;

    public async Task<PolicySection?> FindAsync(System.Guid id, CancellationToken ct)
        => await _db.PolicySections.FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);
}
