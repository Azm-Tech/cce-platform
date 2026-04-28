using CCE.Application.Content;
using CCE.Domain.Country;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Content;

public sealed class CountryResourceRequestService : ICountryResourceRequestService
{
    private readonly CceDbContext _db;

    public CountryResourceRequestService(CceDbContext db)
    {
        _db = db;
    }

    public async Task<CountryResourceRequest?> FindIncludingDeletedAsync(System.Guid id, CancellationToken ct)
    {
        return await _db.CountryResourceRequests
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(CountryResourceRequest request, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
