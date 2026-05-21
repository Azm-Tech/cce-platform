using CCE.Application.PlatformSettings;
using CCE.Domain.PlatformSettings;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.PlatformSettings;

public sealed class AboutSettingsRepository : IAboutSettingsRepository
{
    private readonly CceDbContext _db;

    public AboutSettingsRepository(CceDbContext db) => _db = db;

    public async Task<AboutSettings?> GetAsync(CancellationToken ct)
        => await _db.AboutSettings.FirstOrDefaultAsync(ct).ConfigureAwait(false);
}
