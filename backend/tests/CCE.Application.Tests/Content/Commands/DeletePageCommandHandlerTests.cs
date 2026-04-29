using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.Content.Commands.DeletePage;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class DeletePageCommandHandlerTests
{
    [Fact]
    public async Task Throws_KeyNotFound_when_page_missing()
    {
        var service = Substitute.For<IPageService>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((Page?)null);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        var sut = new DeletePageCommandHandler(service, currentUser, new FakeSystemClock());

        var act = async () => await sut.Handle(new DeletePageCommand(System.Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>();
    }

    [Fact]
    public async Task Throws_DomainException_when_actor_unknown()
    {
        var page = Page.Create("my-slug", PageType.Custom, "ar", "en", "content-ar", "content-en");

        var service = Substitute.For<IPageService>();
        service.FindAsync(page.Id, Arg.Any<CancellationToken>()).Returns(page);

        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns((System.Guid?)null);

        var sut = new DeletePageCommandHandler(service, currentUser, new FakeSystemClock());

        var act = async () => await sut.Handle(new DeletePageCommand(page.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Soft_deletes_and_calls_UpdateAsync()
    {
        var clock = new FakeSystemClock();
        var actorId = System.Guid.NewGuid();
        var page = Page.Create("my-slug", PageType.Custom, "ar", "en", "content-ar", "content-en");

        var service = Substitute.For<IPageService>();
        service.FindAsync(page.Id, Arg.Any<CancellationToken>()).Returns(page);

        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.GetUserId().Returns(actorId);

        var sut = new DeletePageCommandHandler(service, currentUser, clock);

        await sut.Handle(new DeletePageCommand(page.Id), CancellationToken.None);

        page.IsDeleted.Should().BeTrue();
        await service.Received(1).UpdateAsync(page, Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }
}
