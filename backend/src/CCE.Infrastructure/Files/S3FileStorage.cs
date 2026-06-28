using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using CCE.Application.Content;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Files;

public sealed class S3FileStorage : IFileStorage, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly string _bucket;
    private readonly string _publicBaseUrl;

    public S3FileStorage(IOptions<CceInfrastructureOptions> options)
    {
        var opts = options.Value;
        _bucket = opts.S3BucketName;
        _publicBaseUrl = opts.S3PublicBaseUrl;

        var config = new AmazonS3Config
        {
            ServiceURL = opts.S3EndpointUrl,
            ForcePathStyle = true,
            UseHttp = false,
            // Supabase S3-compatible API always requires us-east-1 regardless of project region.
            // Without this the SDK omits the region from the Signature V4 header and Supabase
            // returns 404 "Bucket not found" even when the bucket exists.
            AuthenticationRegion = "us-east-1",
        };

        var credentials = new BasicAWSCredentials(opts.S3AccessKey, opts.S3SecretKey);
        _client = new AmazonS3Client(credentials, config);
    }

    public async Task<string> SaveAsync(Stream content, string suggestedFileName, CancellationToken ct, string? contentType = null)
    {
        var now = System.DateTimeOffset.UtcNow;
        var ext = System.IO.Path.GetExtension(suggestedFileName);
        var key = $"{now:yyyy}/{now:MM}/{System.Guid.NewGuid():N}{ext}";

        var req = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            AutoCloseStream = false,
            ContentType = contentType ?? "application/octet-stream",
        };

        await _client.PutObjectAsync(req, ct).ConfigureAwait(false);
        return key;
    }

    public System.Uri GetPublicUrl(string storageKey)
        => new($"{_publicBaseUrl.TrimEnd('/')}/{_bucket}/{storageKey}");

    public async Task<System.IO.Stream> OpenReadAsync(string storageKey, CancellationToken ct)
    {
        // Support both storage-key format (2026/06/xxx.pdf) and full URL format
        // (https://.../object/public/uploads/2026/06/xxx.pdf) for backward compatibility
        // with assets that were stored before the key-only migration.
        var key = storageKey;
        if (key.StartsWith("http", System.StringComparison.OrdinalIgnoreCase))
        {
            var prefix = $"{_publicBaseUrl.TrimEnd('/')}/{_bucket}/";
            if (key.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                key = key[prefix.Length..];
        }

        var req = new GetObjectRequest
        {
            BucketName = _bucket,
            Key = key,
        };

        try
        {
            var response = await _client.GetObjectAsync(req, ct).ConfigureAwait(false);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new System.IO.FileNotFoundException("Asset not found in storage", storageKey);
        }
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        var req = new DeleteObjectRequest
        {
            BucketName = _bucket,
            Key = storageKey,
        };

        await _client.DeleteObjectAsync(req, ct).ConfigureAwait(false);
    }

    public void Dispose() => _client.Dispose();
}
