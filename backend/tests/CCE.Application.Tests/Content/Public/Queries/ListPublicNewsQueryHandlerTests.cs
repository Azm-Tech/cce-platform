using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.ListPublicNews;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Public.Queries;

public class ListPublicNewsQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_empty_paged_result_when_no_news_exist()
    {
        var db = BuildDb(Array.Empty<News>());
        var sut = new ListPublicNewsQueryHandler(db);

        var result = await sut.Handle(new ListPublicNewsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Only_published_news_are_returned()
    {
        var published = News.Draft("منشور", "Published", "محتوى", "Content", "published-slug", System.Guid.NewGuid(), null, Clock);
        published.Publish(Clock);

        var draft = News.Draft("مسودة", "Draft", "محتوى", "Content", "draft-slug", System.Guid.NewGuid(), null, Clock);

        var db = BuildDb([published, draft]);
        var sut = new ListPublicNewsQueryHandler(db);

        var result = await sut.Handle(new ListPublicNewsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("Published");
    }

    [Fact]
    public async Task IsFeatured_filter_returns_only_featured_published_news()
    {
        var featured = News.Draft("مميز", "Featured", "محتوى", "Content", "featured-slug", System.Guid.NewGuid(), null, Clock);
        featured.Publish(Clock);
        featured.MarkFeatured();

        var notFeatured = News.Draft("عادي", "Regular", "محتوى", "Content", "regular-slug", System.Guid.NewGuid(), null, Clock);
        notFeatured.Publish(Clock);

        var db = BuildDb([featured, notFeatured]);
        var sut = new ListPublicNewsQueryHandler(db);

        var result = await sut.Handle(new ListPublicNewsQuery(Page: 1, PageSize: 20, IsFeatured: true), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("Featured");
        result.Items.Single().IsFeatured.Should().BeTrue();
    }

    private static ICceDbContext BuildDb(IEnumerable<News> news)
    {
        var db = Substitute.For<ICceDbContext>();
        db.News.Returns(news.AsQueryable());
        return db;
    }
}
