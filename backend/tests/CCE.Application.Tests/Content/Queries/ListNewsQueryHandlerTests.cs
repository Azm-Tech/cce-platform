using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.ListNews;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Queries;

public class ListNewsQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_empty_when_no_news()
    {
        var db = BuildDb(Array.Empty<News>());
        var sut = new ListNewsQueryHandler(db);

        var result = await sut.Handle(new ListNewsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Returns_news_sorted_by_PublishedOn_descending()
    {
        var older = News.Draft("أ", "Older", "محتوى", "Content A", "older-article", System.Guid.NewGuid(), null, Clock);
        older.Publish(Clock);
        Clock.Advance(System.TimeSpan.FromSeconds(1));
        var newer = News.Draft("ب", "Newer", "محتوى ب", "Content B", "newer-article", System.Guid.NewGuid(), null, Clock);
        newer.Publish(Clock);

        var db = BuildDb([newer, older]);
        var sut = new ListNewsQueryHandler(db);

        var result = await sut.Handle(new ListNewsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].TitleEn.Should().Be("Newer");
        result.Items[1].TitleEn.Should().Be("Older");
    }

    [Fact]
    public async Task Search_filter_matches_title_ar_title_en_or_slug()
    {
        var news = News.Draft("مطابق", "matching-title", "محتوى", "content", "matching-slug", System.Guid.NewGuid(), null, Clock);

        var db = BuildDb([news]);
        var sut = new ListNewsQueryHandler(db);

        var result = await sut.Handle(new ListNewsQuery(Search: "matching"), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("matching-title");
    }

    [Fact]
    public async Task IsPublished_and_IsFeatured_filters_work()
    {
        var published = News.Draft("منشور", "published-news", "محتوى", "content", "published-news", System.Guid.NewGuid(), null, Clock);
        published.Publish(Clock);

        var featured = News.Draft("مميز", "featured-news", "محتوى", "content", "featured-news", System.Guid.NewGuid(), null, Clock);
        featured.Publish(Clock);
        featured.MarkFeatured();

        var draft = News.Draft("مسودة", "draft-news", "محتوى", "content", "draft-news", System.Guid.NewGuid(), null, Clock);

        var db = BuildDb([published, featured, draft]);
        var sut = new ListNewsQueryHandler(db);

        var publishedResult = await sut.Handle(new ListNewsQuery(IsPublished: true), CancellationToken.None);
        publishedResult.Total.Should().Be(2);
        publishedResult.Items.Should().OnlyContain(n => n.IsPublished);

        var featuredResult = await sut.Handle(new ListNewsQuery(IsFeatured: true), CancellationToken.None);
        featuredResult.Total.Should().Be(1);
        featuredResult.Items.Single().TitleEn.Should().Be("featured-news");

        var draftResult = await sut.Handle(new ListNewsQuery(IsPublished: false), CancellationToken.None);
        draftResult.Total.Should().Be(1);
        draftResult.Items.Single().TitleEn.Should().Be("draft-news");
    }

    private static ICceDbContext BuildDb(IEnumerable<News> news)
    {
        var db = Substitute.For<ICceDbContext>();
        db.News.Returns(news.AsQueryable());
        return db;
    }
}
