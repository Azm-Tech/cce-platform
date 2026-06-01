using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Commands.PublishNews;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Commands;

public class PublishNewsCommandHandlerTests
{
    [Fact]
    public async Task Returns_not_found_when_news_missing()
    {
        var (sut, _, _) = BuildSut(null);

        var result = await sut.Handle(new PublishNewsCommand(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Publishes_and_returns_dto_when_valid()
    {
        var clock = new FakeSystemClock();
        var news = News.Draft("ar", "en", "content-ar", "content-en", System.Guid.NewGuid(), System.Guid.NewGuid(), null, clock);

        var (sut, repo, db) = BuildSut(news);

        var result = await sut.Handle(new PublishNewsCommand(news.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.IsPublished.Should().BeTrue();
        result.Data.PublishedOn.Should().NotBeNull();
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_dto_unchanged_when_already_published()
    {
        var clock = new FakeSystemClock();
        var news = News.Draft("ar", "en", "content-ar", "content-en", System.Guid.NewGuid(), System.Guid.NewGuid(), null, clock);
        news.Publish(clock);
        var firstPublishedOn = news.PublishedOn;

        var (sut, _, _) = BuildSut(news);

        var result = await sut.Handle(new PublishNewsCommand(news.Id), CancellationToken.None);

        result.Data!.IsPublished.Should().BeTrue();
        result.Data.PublishedOn.Should().Be(firstPublishedOn);
    }

    private static (PublishNewsCommandHandler sut,
        IRepository<News, System.Guid> repo,
        ICceDbContext db) BuildSut(News? newsToReturn)
    {
        var clock = new FakeSystemClock();
        var repo = Substitute.For<IRepository<News, System.Guid>>();
        repo.GetByIdAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns(newsToReturn);
        var db = Substitute.For<ICceDbContext>();
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return (new PublishNewsCommandHandler(repo, db, clock, new MessageFactory(localization, Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageFactory>.Instance)), repo, db);
    }
}
