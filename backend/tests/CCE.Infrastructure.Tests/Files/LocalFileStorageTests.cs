using CCE.Infrastructure;
using CCE.Infrastructure.Files;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Tests.Files;

public sealed class LocalFileStorageTests : System.IDisposable
{
    private readonly string _root;
    private readonly LocalFileStorage _sut;

    public LocalFileStorageTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"cce-uploads-{System.Guid.NewGuid():N}");
        _sut = new LocalFileStorage(Options.Create(new CceInfrastructureOptions
        {
            SqlConnectionString = "x", RedisConnectionString = "x", LocalUploadsRoot = _root,
        }));
    }

    [Fact]
    public async Task Save_returns_key_under_year_month_subdirectory()
    {
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hello"));

        var key = await _sut.SaveAsync(ms, "report.pdf", CancellationToken.None);

        var now = System.DateTimeOffset.UtcNow;
        key.Should().StartWith($"uploads/{now:yyyy}/{now:MM}/");
        key.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task Save_round_trips_via_OpenRead()
    {
        var payload = System.Text.Encoding.UTF8.GetBytes("round-trip-test");
        using var ms = new MemoryStream(payload);

        var key = await _sut.SaveAsync(ms, "x.bin", CancellationToken.None);

        await using var read = await _sut.OpenReadAsync(key, CancellationToken.None);
        using var buffer = new MemoryStream();
        await read.CopyToAsync(buffer);
        buffer.ToArray().Should().Equal(payload);
    }

    [Fact]
    public async Task Delete_removes_the_file()
    {
        using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var key = await _sut.SaveAsync(ms, "x.bin", CancellationToken.None);

        await _sut.DeleteAsync(key, CancellationToken.None);

        File.Exists(Path.Combine(_root, key)).Should().BeFalse();
    }

    [Fact]
    public async Task Delete_is_idempotent_when_missing()
    {
        var act = async () => await _sut.DeleteAsync("uploads/9999/01/missing.bin", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
