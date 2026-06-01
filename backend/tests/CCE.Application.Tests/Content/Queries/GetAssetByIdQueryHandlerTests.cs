using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.GetAssetById;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Queries;

public class GetAssetByIdQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_not_found_when_asset_missing()
    {
        var sut = BuildSut(Array.Empty<AssetFile>());

        var result = await sut.Handle(new GetAssetByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Code.Should().Be(SystemCode.ERR045);
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

        var sut = BuildSut([asset]);

        var result = await sut.Handle(new GetAssetByIdQuery(asset.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(asset.Id);
        result.Data.Url.Should().Be("uploads/2026/04/abc.pdf");
        result.Data.OriginalFileName.Should().Be("report.pdf");
        result.Data.SizeBytes.Should().Be(1024);
        result.Data.MimeType.Should().Be("application/pdf");
        result.Data.VirusScanStatus.Should().Be(VirusScanStatus.Clean);
        result.Data.ScannedOn.Should().NotBeNull();
    }

    private static GetAssetByIdQueryHandler BuildSut(IEnumerable<AssetFile> assets)
    {
        var db = Substitute.For<ICceDbContext>();
        db.AssetFiles.Returns(assets.AsQueryable());
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return new GetAssetByIdQueryHandler(db, new MessageFactory(localization, Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageFactory>.Instance));
    }
}
