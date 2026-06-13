using CCE.Domain.Content;

namespace CCE.Application.Content;

public sealed record NewsNotificationData(string TitleAr, string TitleEn, string ContentAr, string ContentEn);

public interface INewsRepository
{
    Task SaveAsync(News news, CancellationToken ct);
    Task<News?> FindAsync(System.Guid id, CancellationToken ct);
    Task UpdateAsync(News news, byte[] expectedRowVersion, CancellationToken ct);
    Task<ContentTitle?> GetTitleAsync(System.Guid id, CancellationToken ct);
    Task<NewsNotificationData?> GetNotificationDataAsync(System.Guid id, CancellationToken ct);
}
