using CCE.Domain.Content;

namespace CCE.Application.Content;

public interface INewsService
{
    Task SaveAsync(News news, CancellationToken ct);
    Task<News?> FindAsync(System.Guid id, CancellationToken ct);
    Task UpdateAsync(News news, byte[] expectedRowVersion, CancellationToken ct);
}
