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

    public S3FileStorage(IOptions<CceInfrastructureOptions> options)
    {
        var opts = options.Value;
        _bucket = opts.S3BucketName;

        var config = new AmazonS3Config
        {
            ServiceURL = opts.S3EndpointUrl,
            ForcePathStyle = true,
            UseHttp = false,
        };

        var credentials = new BasicAWSCredentials(opts.S3AccessKey, opts.S3SecretKey);
        _client = new AmazonS3Client(credentials, config);
    }

    public async Task<string> SaveAsync(Stream content, string suggestedFileName, CancellationToken ct)
    {
        var now = System.DateTimeOffset.UtcNow;
        var ext = System.IO.Path.GetExtension(suggestedFileName);
        var key = $"uploads/{now:yyyy}/{now:MM}/{System.Guid.NewGuid():N}{ext}";

        var req = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            AutoCloseStream = false,
        };

        await _client.PutObjectAsync(req, ct).ConfigureAwait(false);
        return key;
    }

    public async Task<System.IO.Stream> OpenReadAsync(string storageKey, CancellationToken ct)
    {
        var req = new GetObjectRequest
        {
            BucketName = _bucket,
            Key = storageKey,
        };

        var response = await _client.GetObjectAsync(req, ct).ConfigureAwait(false);
        return response.ResponseStream;
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
