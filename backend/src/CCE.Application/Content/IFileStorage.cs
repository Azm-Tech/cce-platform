namespace CCE.Application.Content;

/// <summary>
/// Abstraction over the asset-file blob store. Phase 03 ships <c>LocalFileStorage</c>;
/// sub-project 8 will add S3 / Azure Blob implementations.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Persists <paramref name="content"/> under a generated key derived from the current
    /// year/month and a fresh Guid (preserving the original extension).
    /// Returns the storage key (e.g. <c>uploads/2026/04/abc123.pdf</c>) — NOT a URL.
    /// </summary>
    Task<string> SaveAsync(Stream content, string suggestedFileName, CancellationToken ct);

    /// <summary>Opens a read stream for the previously-saved key.</summary>
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct);

    /// <summary>Deletes the stored object. No-op if the key doesn't exist.</summary>
    Task DeleteAsync(string storageKey, CancellationToken ct);
}
