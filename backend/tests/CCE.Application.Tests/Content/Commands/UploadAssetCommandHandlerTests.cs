using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.Content.Commands.UploadAsset;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;
using Microsoft.Extensions.Logging.Abstractions;

namespace CCE.Application.Tests.Content.Commands;

public class UploadAssetCommandHandlerTests
{
    [Fact]
    public async Task Throws_DomainException_when_actor_unknown()
    {
        var sut = BuildSut(out _, out _, out _, currentUserId: null);

        var act = async () => await sut.Handle(BuildCommand("x.pdf", "application/pdf"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Marks_clean_when_scanner_returns_clean()
    {
        var sut = BuildSut(out _, out var scanner, out var service, currentUserId: System.Guid.NewGuid());
        scanner.ScanAsync(default!, default).ReturnsForAnyArgs(VirusScanResult.Clean);

        var dto = await sut.Handle(BuildCommand("x.pdf", "application/pdf"), CancellationToken.None);

        dto.VirusScanStatus.Should().Be(VirusScanStatus.Clean);
        await service.Received(1).SaveAsync(Arg.Is<AssetFile>(a => a.VirusScanStatus == VirusScanStatus.Clean), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Marks_infected_and_deletes_storage_when_scanner_returns_infected()
    {
        var sut = BuildSut(out var storage, out var scanner, out var service, currentUserId: System.Guid.NewGuid());
        scanner.ScanAsync(default!, default).ReturnsForAnyArgs(VirusScanResult.Infected);
        storage.SaveAsync(default!, default!, default).ReturnsForAnyArgs(Task.FromResult("uploads/2026/04/key.pdf"));

        var dto = await sut.Handle(BuildCommand("x.pdf", "application/pdf"), CancellationToken.None);

        dto.VirusScanStatus.Should().Be(VirusScanStatus.Infected);
        await storage.Received(1).DeleteAsync("uploads/2026/04/key.pdf", Arg.Any<CancellationToken>());
        await service.Received(1).SaveAsync(Arg.Is<AssetFile>(a => a.VirusScanStatus == VirusScanStatus.Infected), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Marks_scan_failed_when_scanner_returns_scan_failed()
    {
        var sut = BuildSut(out var storage, out var scanner, out var service, currentUserId: System.Guid.NewGuid());
        scanner.ScanAsync(default!, default).ReturnsForAnyArgs(VirusScanResult.ScanFailed);
        storage.SaveAsync(default!, default!, default).ReturnsForAnyArgs(Task.FromResult("uploads/2026/04/key.pdf"));

        var dto = await sut.Handle(BuildCommand("x.pdf", "application/pdf"), CancellationToken.None);

        dto.VirusScanStatus.Should().Be(VirusScanStatus.ScanFailed);
        await storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await service.Received(1).SaveAsync(Arg.Is<AssetFile>(a => a.VirusScanStatus == VirusScanStatus.ScanFailed), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Buffers_content_and_passes_size_through()
    {
        var sut = BuildSut(out _, out var scanner, out _, currentUserId: System.Guid.NewGuid());
        scanner.ScanAsync(default!, default).ReturnsForAnyArgs(VirusScanResult.Clean);

        var payload = System.Text.Encoding.UTF8.GetBytes("hello world");
        using var content = new MemoryStream(payload);
        var dto = await sut.Handle(new UploadAssetCommand(content, "x.txt", "text/plain", payload.Length), CancellationToken.None);

        dto.SizeBytes.Should().Be(payload.Length);
        dto.OriginalFileName.Should().Be("x.txt");
        dto.MimeType.Should().Be("text/plain");
    }

    private static UploadAssetCommandHandler BuildSut(
        out IFileStorage storage,
        out IClamAvScanner scanner,
        out IAssetService service,
        System.Guid? currentUserId)
    {
        storage = Substitute.For<IFileStorage>();
        // Default: SaveAsync returns a non-empty key so AssetFile.Register doesn't throw.
        // Individual tests that need to verify DeleteAsync can override this.
        storage.SaveAsync(default!, default!, default).ReturnsForAnyArgs(Task.FromResult("uploads/default/key.bin"));
        scanner = Substitute.For<IClamAvScanner>();
        service = Substitute.For<IAssetService>();
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(currentUserId);
        return new UploadAssetCommandHandler(
            storage, scanner, service, currentUser, new FakeSystemClock(),
            NullLogger<UploadAssetCommandHandler>.Instance);
    }

    private static UploadAssetCommand BuildCommand(string filename, string mimeType)
    {
        var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("payload"));
        return new UploadAssetCommand(content, filename, mimeType, content.Length);
    }
}
