using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.Content.Commands.CreateResource;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class CreateResourceCommandHandlerTests
{
    [Fact]
    public async Task Throws_KeyNotFound_when_asset_missing()
    {
        var (sut, _, asset, _) = BuildSut();
        asset.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((AssetFile?)null);

        var act = async () => await sut.Handle(BuildCmd(System.Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>();
    }

    [Fact]
    public async Task Throws_DomainException_when_asset_not_clean()
    {
        var (sut, _, asset, _) = BuildSut();
        var clock = new FakeSystemClock();
        var pendingAsset = AssetFile.Register("k", "x.pdf", 1, "application/pdf", System.Guid.NewGuid(), clock);
        asset.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns(pendingAsset);

        var act = async () => await sut.Handle(BuildCmd(pendingAsset.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*virus scan*");
    }

    [Fact]
    public async Task Throws_DomainException_when_actor_unknown()
    {
        var (sut, _, asset, _) = BuildSut(noUser: true);
        var clock = new FakeSystemClock();
        var clean = AssetFile.Register("k", "x.pdf", 1, "application/pdf", System.Guid.NewGuid(), clock);
        clean.MarkClean(clock);
        asset.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns(clean);

        var act = async () => await sut.Handle(BuildCmd(clean.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Persists_resource_when_inputs_valid()
    {
        var (sut, service, asset, _) = BuildSut();
        var clock = new FakeSystemClock();
        var clean = AssetFile.Register("k", "x.pdf", 1, "application/pdf", System.Guid.NewGuid(), clock);
        clean.MarkClean(clock);
        asset.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns(clean);

        var dto = await sut.Handle(BuildCmd(clean.Id), CancellationToken.None);

        dto.TitleAr.Should().Be("عنوان");
        dto.TitleEn.Should().Be("Title");
        dto.AssetFileId.Should().Be(clean.Id);
        dto.IsPublished.Should().BeFalse();
        await service.Received(1).SaveAsync(Arg.Any<Resource>(), Arg.Any<CancellationToken>());
    }

    private static CreateResourceCommand BuildCmd(System.Guid assetFileId) =>
        new(
            "عنوان", "Title",
            "وصف", "Description",
            ResourceType.Pdf,
            System.Guid.NewGuid(),
            null,
            assetFileId);

    private static (CreateResourceCommandHandler sut, IResourceService service, IAssetService asset, ICurrentUserAccessor user) BuildSut(bool noUser = false)
    {
        var service = Substitute.For<IResourceService>();
        var asset = Substitute.For<IAssetService>();
        var user = Substitute.For<ICurrentUserAccessor>();
        if (noUser)
            user.GetUserId().Returns((System.Guid?)null);
        else
            user.GetUserId().Returns(System.Guid.NewGuid());
        var sut = new CreateResourceCommandHandler(service, asset, user, new FakeSystemClock());
        return (sut, service, asset, user);
    }
}
