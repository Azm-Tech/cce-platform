using CCE.Application.Content;
using CCE.Domain.Country;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Content;

public sealed class CountryContentRequestRepository : ICountryContentRequestRepository
{
    private readonly CceDbContext _db;

    public CountryContentRequestRepository(CceDbContext db)
    {
        _db = db;
    }

    public async Task<CountryContentRequest?> FindIncludingDeletedAsync(System.Guid id, CancellationToken ct)
    {
        return await _db.CountryContentRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(CountryContentRequest request, CancellationToken ct)
    {
        await _db.CountryContentRequests.AddAsync(request, ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(CountryContentRequest request, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
