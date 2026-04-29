using CCE.Application.Country;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Country;

public sealed class CountryAdminService : ICountryAdminService
{
    private readonly CceDbContext _db;

    public CountryAdminService(CceDbContext db)
    {
        _db = db;
    }

    public async Task<CCE.Domain.Country.Country?> FindAsync(System.Guid id, CancellationToken ct)
    {
        return await _db.Countries.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(CCE.Domain.Country.Country country, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
