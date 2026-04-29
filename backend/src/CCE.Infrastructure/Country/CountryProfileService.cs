using CCE.Application.Country;
using CCE.Domain.Country;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Country;

public sealed class CountryProfileService : ICountryProfileService
{
    private readonly CceDbContext _db;

    public CountryProfileService(CceDbContext db)
    {
        _db = db;
    }

    public async Task<CountryProfile?> FindByCountryIdAsync(System.Guid countryId, CancellationToken ct)
    {
        return await _db.CountryProfiles
            .FirstOrDefaultAsync(p => p.CountryId == countryId, ct)
            .ConfigureAwait(false);
    }

    public async Task SaveAsync(CountryProfile profile, CancellationToken ct)
    {
        _db.CountryProfiles.Add(profile);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(CountryProfile profile, byte[] expectedRowVersion, CancellationToken ct)
    {
        var entry = _db.Entry(profile);
        entry.OriginalValues[nameof(CountryProfile.RowVersion)] = expectedRowVersion;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
