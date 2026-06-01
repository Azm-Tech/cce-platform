using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.GetPublicNewsBySlug;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Public.Queries;

public class GetPublicNewsBySlugQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_dto_when_news_is_published_and_slug_matches()
    {
        var topicId = System.Guid.NewGuid();
        var authorId = System.Guid.NewGuid();
        var news = News.Draft("عنوان", "Published News", "محتوى", "Content", topicId, authorId, null, Clock);
        news.Publish(Clock);

        var sut = BuildSut([news]);

        var result = await sut.Handle(new GetPublicNewsBySlugQuery("published-news"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(news.Id);
        result.Data.TitleEn.Should().Be("Published News");
        result.Data.PublishedOn.Should().NotBe(default);
    }

    [Fact]
    public async Task Returns_not_found_when_slug_missing()
    {
        var sut = BuildSut(Array.Empty<News>());

        var result = await sut.Handle(new GetPublicNewsBySlugQuery("no-such-slug"), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_not_found_when_news_exists_but_not_published()
    {
        var news = News.Draft("مسودة", "Draft News", "محتوى", "Content", System.Guid.NewGuid(), System.Guid.NewGuid(), null, Clock);

        var sut = BuildSut([news]);

        var result = await sut.Handle(new GetPublicNewsBySlugQuery("draft-news"), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    private static GetPublicNewsBySlugQueryHandler BuildSut(IEnumerable<News> news)
    {
        var db = Substitute.For<ICceDbContext>();
        db.News.Returns(news.AsQueryable());
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return new GetPublicNewsBySlugQueryHandler(db, new MessageFactory(localization));
    }
}
