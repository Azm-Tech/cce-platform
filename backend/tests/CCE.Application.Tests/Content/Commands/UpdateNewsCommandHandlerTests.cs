using CCE.Application.Content;
using CCE.Application.Content.Commands.UpdateNews;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class UpdateNewsCommandHandlerTests
{
    [Fact]
    public async Task Returns_null_when_news_not_found()
    {
        var service = Substitute.For<INewsRepository>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((News?)null);
        var sut = new UpdateNewsCommandHandler(service);

        var result = await sut.Handle(BuildCommand(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Updates_content_and_calls_UpdateAsync_with_expected_rowversion()
    {
        var clock = new FakeSystemClock();
        var news = News.Draft("old-ar", "old-en", "old-content-ar", "old-content-en",
            "old-slug", System.Guid.NewGuid(), null, clock);

        var service = Substitute.For<INewsRepository>();
        service.FindAsync(news.Id, Arg.Any<CancellationToken>()).Returns(news);

        var sut = new UpdateNewsCommandHandler(service);
        var rowVersion = new byte[8] { 1, 2, 3, 4, 5, 6, 7, 8 };

        var cmd = new UpdateNewsCommand(
            news.Id,
            "new-ar", "new-en", "new-content-ar", "new-content-en",
            "new-slug", null, rowVersion);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result!.TitleEn.Should().Be("new-en");
        result.Slug.Should().Be("new-slug");
        await service.Received(1).UpdateAsync(news, rowVersion, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Propagates_ConcurrencyException_from_UpdateAsync()
    {
        var clock = new FakeSystemClock();
        var news = News.Draft("ar", "en", "content-ar", "content-en",
            "my-slug", System.Guid.NewGuid(), null, clock);

        var service = Substitute.For<INewsRepository>();
        service.FindAsync(news.Id, Arg.Any<CancellationToken>()).Returns(news);
        service.UpdateAsync(default!, default!, default).ReturnsForAnyArgs<Task>(_ =>
            throw new ConcurrencyException("conflict"));

        var sut = new UpdateNewsCommandHandler(service);
        var cmd = BuildCommand(news.Id);

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyException>();
    }

    private static UpdateNewsCommand BuildCommand(System.Guid id) =>
        new(id, "ar", "en", "content-ar", "content-en", "my-slug", null, new byte[8]);
}
