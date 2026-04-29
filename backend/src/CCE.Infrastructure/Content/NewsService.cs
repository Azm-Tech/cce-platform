using CCE.Application.Content;
using CCE.Domain.Content;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Content;

public sealed class NewsService : INewsService
{
    private readonly CceDbContext _db;

    public NewsService(CceDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(News news, CancellationToken ct)
    {
        _db.News.Add(news);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<News?> FindAsync(System.Guid id, CancellationToken ct)
    {
        return await _db.News.FirstOrDefaultAsync(n => n.Id == id, ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(News news, byte[] expectedRowVersion, CancellationToken ct)
    {
        var entry = _db.Entry(news);
        entry.OriginalValues[nameof(News.RowVersion)] = expectedRowVersion;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
