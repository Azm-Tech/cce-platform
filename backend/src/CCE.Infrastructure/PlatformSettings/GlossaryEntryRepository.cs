using CCE.Application.PlatformSettings;
using CCE.Domain.PlatformSettings;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.PlatformSettings;

public sealed class GlossaryEntryRepository : IGlossaryEntryRepository
{
    private readonly CceDbContext _db;

    public GlossaryEntryRepository(CceDbContext db) => _db = db;

    public async Task<GlossaryEntry?> FindAsync(System.Guid id, CancellationToken ct)
        => await _db.GlossaryEntries.FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);
}
