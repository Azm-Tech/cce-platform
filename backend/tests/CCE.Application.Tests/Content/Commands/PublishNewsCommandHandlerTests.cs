using CCE.Application.Content;
using CCE.Application.Content.Commands.PublishNews;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class PublishNewsCommandHandlerTests
{
    [Fact]
    public async Task Returns_null_when_news_not_found()
    {
        var service = Substitute.For<INewsRepository>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns((News?)null);
        var sut = new PublishNewsCommandHandler(service, new FakeSystemClock());

        var result = await sut.Handle(new PublishNewsCommand(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Publishes_and_returns_dto_when_valid()
    {
        var clock = new FakeSystemClock();
        var news = News.Draft("ar", "en", "content-ar", "content-en", "slug", System.Guid.NewGuid(), null, clock);

        var service = Substitute.For<INewsRepository>();
        service.FindAsync(news.Id, Arg.Any<CancellationToken>()).Returns(news);

        var sut = new PublishNewsCommandHandler(service, clock);

        var dto = await sut.Handle(new PublishNewsCommand(news.Id), CancellationToken.None);

        dto.Should().NotBeNull();
        dto!.IsPublished.Should().BeTrue();
        dto.PublishedOn.Should().NotBeNull();
        await service.Received(1).UpdateAsync(news, Arg.Any<byte[]>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_dto_unchanged_when_already_published()
    {
        var clock = new FakeSystemClock();
        var news = News.Draft("ar", "en", "content-ar", "content-en", "slug", System.Guid.NewGuid(), null, clock);
        news.Publish(clock); // already published
        var firstPublishedOn = news.PublishedOn;

        var service = Substitute.For<INewsRepository>();
        service.FindAsync(news.Id, Arg.Any<CancellationToken>()).Returns(news);

        var sut = new PublishNewsCommandHandler(service, clock);

        var dto = await sut.Handle(new PublishNewsCommand(news.Id), CancellationToken.None);

        dto!.IsPublished.Should().BeTrue();
        dto.PublishedOn.Should().Be(firstPublishedOn);
    }
}
