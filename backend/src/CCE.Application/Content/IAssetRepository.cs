using CCE.Domain.Content;

namespace CCE.Application.Content;

public interface IAssetRepository
{
    /// <summary>
    /// Persists a newly-registered asset file. Single SaveChanges call.
    /// </summary>
    Task SaveAsync(AssetFile asset, CancellationToken ct);

    /// <summary>Loads by Id (no soft-delete filter on AssetFile — it's not soft-deletable).</summary>
    Task<AssetFile?> FindAsync(System.Guid id, CancellationToken ct);
}
