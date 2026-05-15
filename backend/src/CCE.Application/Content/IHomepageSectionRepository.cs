using CCE.Domain.Content;

namespace CCE.Application.Content;

public interface IHomepageSectionRepository
{
    Task SaveAsync(HomepageSection section, CancellationToken ct);
    Task<HomepageSection?> FindAsync(System.Guid id, CancellationToken ct);
    Task UpdateAsync(HomepageSection section, CancellationToken ct);
    Task ReorderAsync(System.Collections.Generic.IReadOnlyList<(System.Guid Id, int OrderIndex)> assignments, CancellationToken ct);
}
