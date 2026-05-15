using CCE.Application.Content;
using CCE.Application.Content.Commands.UpdatePage;
using CCE.Domain.Common;
using CCE.Domain.Content;

namespace CCE.Application.Tests.Content.Commands;

public class UpdatePageCommandHandlerTests
{
    [Fact]
    public async Task Returns_null_when_page_not_found()
    {
        var service = Substitute.For<IPageRepository>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((Page?)null);
        var sut = new UpdatePageCommandHandler(service);

        var result = await sut.Handle(BuildCommand(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Updates_content_and_calls_UpdateAsync_with_expected_rowversion()
    {
        var page = Page.Create("test-slug", PageType.Custom, "old-ar", "old-en", "old-content-ar", "old-content-en");

        var service = Substitute.For<IPageRepository>();
        service.FindAsync(page.Id, Arg.Any<CancellationToken>()).Returns(page);

        var sut = new UpdatePageCommandHandler(service);
        var rowVersion = new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var cmd = new UpdatePageCommand(
            page.Id,
            "new-ar", "new-en", "new-content-ar", "new-content-en",
            rowVersion);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result!.TitleEn.Should().Be("new-en");
        result.TitleAr.Should().Be("new-ar");
        await service.Received(1).UpdateAsync(page, rowVersion, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Propagates_ConcurrencyException_from_UpdateAsync()
    {
        var page = Page.Create("my-slug", PageType.Custom, "ar", "en", "content-ar", "content-en");

        var service = Substitute.For<IPageRepository>();
        service.FindAsync(page.Id, Arg.Any<CancellationToken>()).Returns(page);
        service.UpdateAsync(default!, default!, default).ReturnsForAnyArgs<Task>(_ =>
            throw new ConcurrencyException("conflict"));

        var sut = new UpdatePageCommandHandler(service);
        var cmd = BuildCommand(page.Id);

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyException>();
    }

    private static UpdatePageCommand BuildCommand(System.Guid id) =>
        new(id, "ar", "en", "content-ar", "content-en", new byte[8]);
}
