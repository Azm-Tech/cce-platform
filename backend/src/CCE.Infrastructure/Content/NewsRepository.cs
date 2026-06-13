using CCE.Application.Content;
using CCE.Domain.Content;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Content;

public sealed class NewsRepository : INewsRepository
{
    private readonly CceDbContext _db;

    public NewsRepository(CceDbContext db)
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
        _db.SetExpectedRowVersion(news, expectedRowVersion);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public Task<ContentTitle?> GetTitleAsync(System.Guid id, CancellationToken ct)
        => _db.News
            .AsNoTracking()
            .Where(n => n.Id == id)
            .Select(n => new ContentTitle(n.TitleAr, n.TitleEn))
            .FirstOrDefaultAsync(ct);

    public Task<NewsNotificationData?> GetNotificationDataAsync(System.Guid id, CancellationToken ct)
        => _db.News
            .AsNoTracking()
            .Where(n => n.Id == id)
            .Select(n => new NewsNotificationData(n.TitleAr, n.TitleEn, n.ContentAr, n.ContentEn))
            .FirstOrDefaultAsync(ct);
}
