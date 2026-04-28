using CCE.Application.Content;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Files;

/// <summary>
/// Dev/local-disk implementation of <see cref="IFileStorage"/>.
/// Storage key shape: <c>uploads/yyyy/MM/{guid}{ext}</c> (relative to <c>LocalUploadsRoot</c>).
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<CceInfrastructureOptions> options)
    {
        _root = options.Value.LocalUploadsRoot;
    }

    public async Task<string> SaveAsync(Stream content, string suggestedFileName, CancellationToken ct)
    {
        var now = System.DateTimeOffset.UtcNow;
        var ext = Path.GetExtension(suggestedFileName);
        var key = $"uploads/{now:yyyy}/{now:MM}/{System.Guid.NewGuid():N}{ext}";

        var fullPath = Path.Combine(_root, key);
        var dir = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(dir);

        await using (var fs = File.Create(fullPath))
        {
            await content.CopyToAsync(fs, ct).ConfigureAwait(false);
        }

        return key;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct)
    {
        var fullPath = Path.Combine(_root, storageKey);
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        var fullPath = Path.Combine(_root, storageKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }
}
