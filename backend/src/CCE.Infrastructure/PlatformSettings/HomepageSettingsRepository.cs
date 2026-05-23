using CCE.Application.PlatformSettings;
using CCE.Domain.PlatformSettings;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.PlatformSettings;

public sealed class HomepageSettingsRepository : IHomepageSettingsRepository
{
    private readonly CceDbContext _db;

    public HomepageSettingsRepository(CceDbContext db) => _db = db;

    public async Task<HomepageSettings?> GetAsync(CancellationToken ct)
        => await _db.HomepageSettings
            .Include(s => s.Countries)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
}
