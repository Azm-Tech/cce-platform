using CCE.Application.Content;
using CCE.Application.Content.Commands.UpdateResource;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class UpdateResourceCommandHandlerTests
{
    [Fact]
    public async Task Returns_null_when_resource_not_found()
    {
        var service = Substitute.For<IResourceRepository>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((Resource?)null);
        var sut = new UpdateResourceCommandHandler(service);

        var result = await sut.Handle(BuildCommand(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Updates_content_and_calls_UpdateAsync_with_expected_rowversion()
    {
        var clock = new FakeSystemClock();
        var resource = Resource.Draft(
            "old-ar", "old-en", "old-desc-ar", "old-desc-en",
            ResourceType.Pdf, System.Guid.NewGuid(), null,
            System.Guid.NewGuid(), System.Guid.NewGuid(), clock);

        var service = Substitute.For<IResourceRepository>();
        service.FindAsync(resource.Id, Arg.Any<CancellationToken>()).Returns(resource);

        var sut = new UpdateResourceCommandHandler(service);
        var rowVersion = new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var cmd = new UpdateResourceCommand(
            resource.Id,
            "new-ar", "new-en", "new-desc-ar", "new-desc-en",
            ResourceType.Video, System.Guid.NewGuid(),
            rowVersion);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result!.TitleEn.Should().Be("new-en");
        result.ResourceType.Should().Be(ResourceType.Video);
        await service.Received(1).UpdateAsync(resource, rowVersion, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Propagates_DomainException_from_UpdateContent_when_title_empty()
    {
        var clock = new FakeSystemClock();
        var resource = Resource.Draft(
            "old-ar", "old-en", "old-desc-ar", "old-desc-en",
            ResourceType.Pdf, System.Guid.NewGuid(), null,
            System.Guid.NewGuid(), System.Guid.NewGuid(), clock);

        var service = Substitute.For<IResourceRepository>();
        service.FindAsync(resource.Id, Arg.Any<CancellationToken>()).Returns(resource);

        var sut = new UpdateResourceCommandHandler(service);
        var cmd = new UpdateResourceCommand(
            resource.Id,
            "", "new-en", "new-desc-ar", "new-desc-en",
            ResourceType.Video, System.Guid.NewGuid(),
            new byte[8]);

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Propagates_ConcurrencyException_from_UpdateAsync()
    {
        var clock = new FakeSystemClock();
        var resource = Resource.Draft(
            "ar", "en", "desc-ar", "desc-en",
            ResourceType.Pdf, System.Guid.NewGuid(), null,
            System.Guid.NewGuid(), System.Guid.NewGuid(), clock);

        var service = Substitute.For<IResourceRepository>();
        service.FindAsync(resource.Id, Arg.Any<CancellationToken>()).Returns(resource);
        service.UpdateAsync(default!, default!, default).ReturnsForAnyArgs<Task>(_ =>
            throw new ConcurrencyException("conflict"));

        var sut = new UpdateResourceCommandHandler(service);
        var cmd = BuildCommand(resource.Id);

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyException>();
    }

    private static UpdateResourceCommand BuildCommand(System.Guid id) =>
        new(id, "ar", "en", "desc-ar", "desc-en", ResourceType.Pdf, System.Guid.NewGuid(), new byte[8]);
}
