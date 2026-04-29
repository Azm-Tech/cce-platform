using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.ListNews;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Queries;

public class ListNewsQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_paged_result_when_no_news_exist()
    {
        var db = BuildDb(System.Array.Empty<News>());
        var sut = new ListNewsQueryHandler(db);

        var result = await sut.Handle(new ListNewsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Returns_news_sorted_by_PublishedOn_descending()
    {
        var clock = new FakeSystemClock();
        var authorId = System.Guid.NewGuid();

        var older = News.Draft("أ", "Older", "محتوى", "Content A", "older-article", authorId, null, clock);
        var newer = News.Draft("ب", "Newer", "محتوى ب", "Content B", "newer-article", authorId, null, clock);

        older.Publish(clock);
        clock.Advance(System.TimeSpan.FromMinutes(5));
        newer.Publish(clock);

        var db = BuildDb(new[] { older, newer });
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
        var clock = new FakeSystemClock();
        var authorId = System.Guid.NewGuid();

        var match = News.Draft("مطابق", "matching-title", "محتوى", "content", "matching-slug", authorId, null, clock);
        var noMatch = News.Draft("آخر", "other-title", "محتوى آخر", "other content", "other-slug", authorId, null, clock);

        var db = BuildDb(new[] { match, noMatch });
        var sut = new ListNewsQueryHandler(db);

        var result = await sut.Handle(new ListNewsQuery(Search: "matching"), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("matching-title");
    }

    [Fact]
    public async Task IsPublished_and_IsFeatured_filters_work()
    {
        var clock = new FakeSystemClock();
        var authorId = System.Guid.NewGuid();

        var published = News.Draft("منشور", "published-news", "محتوى", "content", "published-news", authorId, null, clock);
        var draft = News.Draft("مسودة", "draft-news", "محتوى", "content", "draft-news", authorId, null, clock);
        var featured = News.Draft("مميز", "featured-news", "محتوى", "content", "featured-news", authorId, null, clock);

        published.Publish(clock);
        featured.Publish(clock);
        featured.MarkFeatured();

        var db = BuildDb(new[] { published, draft, featured });
        var sut = new ListNewsQueryHandler(db);

        var publishedResult = await sut.Handle(new ListNewsQuery(IsPublished: true), CancellationToken.None);
        publishedResult.Total.Should().Be(2);
        publishedResult.Items.Should().OnlyContain(n => n.IsPublished);

        var featuredResult = await sut.Handle(new ListNewsQuery(IsFeatured: true), CancellationToken.None);
        featuredResult.Total.Should().Be(1);
        featuredResult.Items.Single().TitleEn.Should().Be("featured-news");
    }

    private static ICceDbContext BuildDb(IEnumerable<News> news)
    {
        var db = Substitute.For<ICceDbContext>();
        db.News.Returns(news.AsQueryable());
        db.Users.Returns(System.Array.Empty<CCE.Domain.Identity.User>().AsQueryable());
        db.Roles.Returns(System.Array.Empty<CCE.Domain.Identity.Role>().AsQueryable());
        db.UserRoles.Returns(System.Array.Empty<Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>>().AsQueryable());
        db.Resources.Returns(System.Array.Empty<CCE.Domain.Content.Resource>().AsQueryable());
        return db;
    }
}
