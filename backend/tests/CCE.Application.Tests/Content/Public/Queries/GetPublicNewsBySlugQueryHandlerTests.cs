using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.GetPublicNewsBySlug;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Public.Queries;

public class GetPublicNewsBySlugQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_dto_when_news_is_published_and_slug_matches()
    {
        var news = News.Draft("عنوان", "Published News", "محتوى", "Content", "published-slug", System.Guid.NewGuid(), null, Clock);
        news.Publish(Clock);

        var db = BuildDb([news]);
        var sut = new GetPublicNewsBySlugQueryHandler(db);

        var result = await sut.Handle(new GetPublicNewsBySlugQuery("published-slug"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Slug.Should().Be("published-slug");
        result.TitleEn.Should().Be("Published News");
        result.PublishedOn.Should().NotBe(default);
    }

    [Fact]
    public async Task Returns_null_when_slug_not_found()
    {
        var db = BuildDb(Array.Empty<News>());
        var sut = new GetPublicNewsBySlugQueryHandler(db);

        var result = await sut.Handle(new GetPublicNewsBySlugQuery("no-such-slug"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_null_when_news_found_but_not_published()
    {
        var news = News.Draft("مسودة", "Draft News", "محتوى", "Content", "draft-slug", System.Guid.NewGuid(), null, Clock);

        var db = BuildDb([news]);
        var sut = new GetPublicNewsBySlugQueryHandler(db);

        var result = await sut.Handle(new GetPublicNewsBySlugQuery("draft-slug"), CancellationToken.None);

        result.Should().BeNull();
    }

    private static ICceDbContext BuildDb(IEnumerable<News> news)
    {
        var db = Substitute.For<ICceDbContext>();
        db.News.Returns(news.AsQueryable());
        return db;
    }
}
