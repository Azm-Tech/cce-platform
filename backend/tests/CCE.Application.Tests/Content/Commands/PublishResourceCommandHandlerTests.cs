using CCE.Application.Content;
using CCE.Application.Content.Commands.PublishResource;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class PublishResourceCommandHandlerTests
{
    [Fact]
    public async Task Returns_null_when_resource_not_found()
    {
        var (sut, _, _) = BuildSut();
        var result = await sut.Handle(new PublishResourceCommand(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Throws_DomainException_when_asset_not_clean()
    {
        var clock = new FakeSystemClock();
        var (sut, resourceService, assetService) = BuildSut();
        var assetId = System.Guid.NewGuid();
        var resource = Resource.Draft("ar", "en", "desc-ar", "desc-en", ResourceType.Pdf, System.Guid.NewGuid(), null, System.Guid.NewGuid(), assetId, clock);
        resourceService.FindAsync(resource.Id, Arg.Any<CancellationToken>()).Returns(resource);
        var pendingAsset = AssetFile.Register("k", "x.pdf", 1, "application/pdf", System.Guid.NewGuid(), clock);
        assetService.FindAsync(assetId, Arg.Any<CancellationToken>()).Returns(pendingAsset);

        var act = async () => await sut.Handle(new PublishResourceCommand(resource.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*virus scan*");
    }

    [Fact]
    public async Task Throws_DomainException_when_asset_not_found()
    {
        var clock = new FakeSystemClock();
        var (sut, resourceService, assetService) = BuildSut();
        var resource = Resource.Draft("ar", "en", "desc-ar", "desc-en", ResourceType.Pdf, System.Guid.NewGuid(), null, System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        resourceService.FindAsync(resource.Id, Arg.Any<CancellationToken>()).Returns(resource);
        assetService.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((AssetFile?)null);

        var act = async () => await sut.Handle(new PublishResourceCommand(resource.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task Publishes_resource_when_asset_clean()
    {
        var clock = new FakeSystemClock();
        var (sut, resourceService, assetService) = BuildSut();
        var assetId = System.Guid.NewGuid();
        var resource = Resource.Draft("ar", "en", "desc-ar", "desc-en", ResourceType.Pdf, System.Guid.NewGuid(), null, System.Guid.NewGuid(), assetId, clock);
        resourceService.FindAsync(resource.Id, Arg.Any<CancellationToken>()).Returns(resource);
        var clean = AssetFile.Register("k", "x.pdf", 1, "application/pdf", System.Guid.NewGuid(), clock);
        clean.MarkClean(clock);
        assetService.FindAsync(assetId, Arg.Any<CancellationToken>()).Returns(clean);

        var dto = await sut.Handle(new PublishResourceCommand(resource.Id), CancellationToken.None);

        dto.Should().NotBeNull();
        dto!.IsPublished.Should().BeTrue();
        dto.PublishedOn.Should().NotBeNull();
        await resourceService.Received(1).UpdateAsync(resource, Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_dto_unchanged_when_already_published()
    {
        var clock = new FakeSystemClock();
        var (sut, resourceService, assetService) = BuildSut();
        var assetId = System.Guid.NewGuid();
        var resource = Resource.Draft("ar", "en", "desc-ar", "desc-en", ResourceType.Pdf, System.Guid.NewGuid(), null, System.Guid.NewGuid(), assetId, clock);
        resource.Publish(clock); // already published
        resourceService.FindAsync(resource.Id, Arg.Any<CancellationToken>()).Returns(resource);
        var clean = AssetFile.Register("k", "x.pdf", 1, "application/pdf", System.Guid.NewGuid(), clock);
        clean.MarkClean(clock);
        assetService.FindAsync(assetId, Arg.Any<CancellationToken>()).Returns(clean);

        var firstPublishedOn = resource.PublishedOn;
        var dto = await sut.Handle(new PublishResourceCommand(resource.Id), CancellationToken.None);

        dto!.IsPublished.Should().BeTrue();
        dto.PublishedOn.Should().Be(firstPublishedOn);
    }

    private static (PublishResourceCommandHandler sut, IResourceService rs, IAssetService asset) BuildSut()
    {
        var rs = Substitute.For<IResourceService>();
        var asset = Substitute.For<IAssetService>();
        var sut = new PublishResourceCommandHandler(rs, asset, new FakeSystemClock());
        return (sut, rs, asset);
    }
}
