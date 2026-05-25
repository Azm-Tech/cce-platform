using CCE.Application.PlatformSettings;
using CCE.Domain.PlatformSettings;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.PlatformSettings;

public sealed class PoliciesSettingsRepository : IPoliciesSettingsRepository
{
    private readonly CceDbContext _db;

    public PoliciesSettingsRepository(CceDbContext db) => _db = db;

    public async Task<PoliciesSettings?> GetAsync(CancellationToken ct)
        => await _db.PoliciesSettings
            .Include(s => s.Sections)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
}
