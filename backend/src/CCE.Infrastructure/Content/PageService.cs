using CCE.Application.Content;
using CCE.Domain.Content;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Content;

public sealed class PageService : IPageService
{
    private readonly CceDbContext _db;

    public PageService(CceDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(Page page, CancellationToken ct)
    {
        _db.Pages.Add(page);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<Page?> FindAsync(System.Guid id, CancellationToken ct)
    {
        return await _db.Pages.FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Page page, byte[] expectedRowVersion, CancellationToken ct)
    {
        var entry = _db.Entry(page);
        entry.OriginalValues[nameof(Page.RowVersion)] = expectedRowVersion;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
