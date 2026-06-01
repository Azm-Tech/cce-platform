using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.ListNews;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Queries;

public class ListNewsQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_empty_when_no_news()
    {
        var sut = BuildSut(Array.Empty<News>());

        var result = await sut.Handle(new ListNewsQuery(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.Total.Should().Be(0);
        result.Data.Page.Should().Be(1);
        result.Data.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Returns_news_sorted_by_PublishedOn_descending()
    {
        var topicId = System.Guid.NewGuid();
        var older = News.Draft("أ", "Older", "محتوى", "Content A", topicId, System.Guid.NewGuid(), null, Clock);
        older.Publish(Clock);
        Clock.Advance(System.TimeSpan.FromSeconds(1));
        var newer = News.Draft("ب", "Newer", "محتوى ب", "Content B", topicId, System.Guid.NewGuid(), null, Clock);
        newer.Publish(Clock);

        var sut = BuildSut([newer, older]);

        var result = await sut.Handle(new ListNewsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Data!.Total.Should().Be(2);
        result.Data.Items.Should().HaveCount(2);
        result.Data.Items[0].TitleEn.Should().Be("Newer");
        result.Data.Items[1].TitleEn.Should().Be("Older");
    }

    [Fact]
    public async Task Search_filter_matches_title_ar_title_en_or_slug()
    {
        var topicId = System.Guid.NewGuid();
        var news = News.Draft("مطابق", "matching-title", "محتوى", "content", topicId, System.Guid.NewGuid(), null, Clock);

        var sut = BuildSut([news]);

        var result = await sut.Handle(new ListNewsQuery(Search: "matching"), CancellationToken.None);

        result.Data!.Total.Should().Be(1);
        result.Data.Items.Single().TitleEn.Should().Be("matching-title");
    }

    [Fact]
    public async Task IsPublished_and_IsFeatured_filters_work()
    {
        var topicId = System.Guid.NewGuid();
        var published = News.Draft("منشور", "published-news", "محتوى", "content", topicId, System.Guid.NewGuid(), null, Clock);
        published.Publish(Clock);

        var featured = News.Draft("مميز", "featured-news", "محتوى", "content", topicId, System.Guid.NewGuid(), null, Clock);
        featured.Publish(Clock);
        featured.MarkFeatured();

        var draft = News.Draft("مسودة", "draft-news", "محتوى", "content", topicId, System.Guid.NewGuid(), null, Clock);

        var sut = BuildSut([published, featured, draft]);

        var publishedResult = await sut.Handle(new ListNewsQuery(IsPublished: true), CancellationToken.None);
        publishedResult.Data!.Total.Should().Be(2);
        publishedResult.Data.Items.Should().OnlyContain(n => n.IsPublished);

        var featuredResult = await sut.Handle(new ListNewsQuery(IsFeatured: true), CancellationToken.None);
        featuredResult.Data!.Total.Should().Be(1);
        featuredResult.Data.Items.Single().TitleEn.Should().Be("featured-news");

        var draftResult = await sut.Handle(new ListNewsQuery(IsPublished: false), CancellationToken.None);
        draftResult.Data!.Total.Should().Be(1);
        draftResult.Data.Items.Single().TitleEn.Should().Be("draft-news");
    }

    private static ListNewsQueryHandler BuildSut(IEnumerable<News> news)
    {
        var db = Substitute.For<ICceDbContext>();
        db.News.Returns(news.AsQueryable());
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return new ListNewsQueryHandler(db, new MessageFactory(localization, Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageFactory>.Instance));
    }
}
