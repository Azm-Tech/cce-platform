using CCE.Domain.Content;

namespace CCE.Application.Content;

public interface IPageService
{
    Task SaveAsync(Page page, CancellationToken ct);
    Task<Page?> FindAsync(System.Guid id, CancellationToken ct);
    Task UpdateAsync(Page page, byte[] expectedRowVersion, CancellationToken ct);
}
