using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Commands.PublishResource;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class PublishResourceCommandHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_not_found_when_resource_missing()
    {
        var (sut, _) = BuildSut(null, Array.Empty<AssetFile>());

        var result = await sut.Handle(new PublishResourceCommand(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_asset_not_clean_when_asset_pending()
    {
        var pendingAsset = AssetFile.Register("k", "x.pdf", 1, "application/pdf", System.Guid.NewGuid(), Clock);
        var resource = Resource.Draft("ar", "en", "desc-ar", "desc-en", ResourceType.Paper, System.Guid.NewGuid(), null, System.Guid.NewGuid(), pendingAsset.Id, System.Array.Empty<System.Guid>(), Clock);

        var (sut, _) = BuildSut(resource, [pendingAsset]);

        var result = await sut.Handle(new PublishResourceCommand(resource.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_asset_not_found_when_asset_missing()
    {
        var resource = Resource.Draft("ar", "en", "desc-ar", "desc-en", ResourceType.Paper, System.Guid.NewGuid(), null, System.Guid.NewGuid(), System.Guid.NewGuid(), System.Array.Empty<System.Guid>(), Clock);

        var (sut, _) = BuildSut(resource, Array.Empty<AssetFile>());

        var result = await sut.Handle(new PublishResourceCommand(resource.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Publishes_resource_when_asset_clean()
    {
        var clean = AssetFile.Register("k", "x.pdf", 1, "application/pdf", System.Guid.NewGuid(), Clock);
        clean.MarkClean(Clock);
        var resource = Resource.Draft("ar", "en", "desc-ar", "desc-en", ResourceType.Paper, System.Guid.NewGuid(), null, System.Guid.NewGuid(), clean.Id, System.Array.Empty<System.Guid>(), Clock);

        var (sut, db) = BuildSut(resource, [clean]);

        var result = await sut.Handle(new PublishResourceCommand(resource.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.IsPublished.Should().BeTrue();
        result.Data.PublishedOn.Should().NotBeNull();
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_dto_unchanged_when_already_published()
    {
        var clean = AssetFile.Register("k", "x.pdf", 1, "application/pdf", System.Guid.NewGuid(), Clock);
        clean.MarkClean(Clock);
        var resource = Resource.Draft("ar", "en", "desc-ar", "desc-en", ResourceType.Paper, System.Guid.NewGuid(), null, System.Guid.NewGuid(), clean.Id, System.Array.Empty<System.Guid>(), Clock);
        resource.Publish(Clock);
        var firstPublishedOn = resource.PublishedOn;

        var (sut, _) = BuildSut(resource, [clean]);

        var result = await sut.Handle(new PublishResourceCommand(resource.Id), CancellationToken.None);

        result.Data!.IsPublished.Should().BeTrue();
        result.Data.PublishedOn.Should().Be(firstPublishedOn);
    }

    private static (PublishResourceCommandHandler sut, ICceDbContext db) BuildSut(
        Resource? resourceToReturn,
        IEnumerable<AssetFile> assets)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Resources.Returns(resourceToReturn is null ? Array.Empty<Resource>().AsQueryable() : new[] { resourceToReturn }.AsQueryable());
        db.AssetFiles.Returns(assets.AsQueryable());

        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));

        return (new PublishResourceCommandHandler(db, Clock, new MessageFactory(localization, Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageFactory>.Instance)), db);
    }
}
