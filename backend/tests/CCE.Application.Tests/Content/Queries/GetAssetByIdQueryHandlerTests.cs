using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.GetAssetById;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Queries;

public class GetAssetByIdQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_null_when_asset_not_found()
    {
        var db = BuildDb(Array.Empty<AssetFile>());
        var sut = new GetAssetByIdQueryHandler(db);

        var result = await sut.Handle(new GetAssetByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_dto_when_asset_found()
    {
        var asset = AssetFile.Register(
            "uploads/2026/04/abc.pdf",
            "report.pdf",
            1024,
            "application/pdf",
            System.Guid.NewGuid(),
            Clock);
        asset.MarkClean(Clock);

        var db = BuildDb([asset]);
        var sut = new GetAssetByIdQueryHandler(db);

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

    private static ICceDbContext BuildDb(IEnumerable<AssetFile> assets)
    {
        var db = Substitute.For<ICceDbContext>();
        db.AssetFiles.Returns(assets.AsQueryable());
        return db;
    }
}
