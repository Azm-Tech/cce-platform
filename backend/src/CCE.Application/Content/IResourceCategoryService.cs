using CCE.Domain.Content;

namespace CCE.Application.Content;

public interface IResourceCategoryService
{
    Task SaveAsync(ResourceCategory category, CancellationToken ct);
    Task<ResourceCategory?> FindAsync(System.Guid id, CancellationToken ct);
    Task UpdateAsync(ResourceCategory category, CancellationToken ct);
}
