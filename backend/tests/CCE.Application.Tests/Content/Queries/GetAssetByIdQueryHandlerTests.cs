using CCE.Application.Content;
using CCE.Application.Content.Queries.GetAssetById;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Queries;

public class GetAssetByIdQueryHandlerTests
{
    [Fact]
    public async Task Returns_null_when_asset_not_found()
    {
        var service = Substitute.For<IAssetService>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((AssetFile?)null);
        var sut = new GetAssetByIdQueryHandler(service);

        var result = await sut.Handle(new GetAssetByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_dto_when_asset_found()
    {
        var clock = new FakeSystemClock();
        var asset = AssetFile.Register(
            url: "uploads/2026/04/abc.pdf",
            originalFileName: "report.pdf",
            sizeBytes: 1024,
            mimeType: "application/pdf",
            uploadedById: System.Guid.NewGuid(),
            clock: clock);
        asset.MarkClean(clock);

        var service = Substitute.For<IAssetService>();
        service.FindAsync(asset.Id, Arg.Any<CancellationToken>()).Returns(asset);
        var sut = new GetAssetByIdQueryHandler(service);

        var result = await sut.Handle(new GetAssetByIdQuery(asset.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(asset.Id);
        result.Url.Should().Be("uploads/2026/04/abc.pdf");
        result.OriginalFileName.Should().Be("report.pdf");
        result.SizeBytes.Should().Be(1024);
        result.MimeType.Should().Be("application/pdf");
        result.VirusScanStatus.Should().Be(VirusScanStatus.Clean);
        result.ScannedOn.Should().NotBeNull();
    }
}
