using CCE.Domain.Content;

namespace CCE.Application.Content;

public interface IResourceRepository
{
    Task SaveAsync(Resource resource, CancellationToken ct);
    Task<Resource?> FindAsync(System.Guid id, CancellationToken ct);
    Task UpdateAsync(Resource resource, byte[] expectedRowVersion, CancellationToken ct);
}
