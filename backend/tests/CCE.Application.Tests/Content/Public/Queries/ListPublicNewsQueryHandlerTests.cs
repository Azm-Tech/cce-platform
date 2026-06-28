using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.ListPublicNews;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Public.Queries;

public class ListPublicNewsQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_empty_paged_result_when_no_news_exist()
    {
        var sut = BuildSut(Array.Empty<News>());

        var result = await sut.Handle(new ListPublicNewsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.Total.Should().Be(0);
        result.Data.Page.Should().Be(1);
        result.Data.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Only_published_news_are_returned()
    {
        var topicId = System.Guid.NewGuid();
        var published = News.Draft("منشور", "Published", "محتوى", "Content", topicId, System.Guid.NewGuid(), null, Clock);
        published.Publish(Clock);

        var draft = News.Draft("مسودة", "Draft", "محتوى", "Content", topicId, System.Guid.NewGuid(), null, Clock);

        var sut = BuildSut([published, draft]);

        var result = await sut.Handle(new ListPublicNewsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Data!.Total.Should().Be(1);
        result.Data.Items.Single().TitleEn.Should().Be("Published");
    }

    [Fact]
    public async Task IsFeatured_filter_returns_only_featured_published_news()
    {
        var topicId = System.Guid.NewGuid();
        var featured = News.Draft("مميز", "Featured", "محتوى", "Content", topicId, System.Guid.NewGuid(), null, Clock);
        featured.Publish(Clock);
        featured.MarkFeatured();

        var notFeatured = News.Draft("عادي", "Regular", "محتوى", "Content", topicId, System.Guid.NewGuid(), null, Clock);
        notFeatured.Publish(Clock);

        var sut = BuildSut([featured, notFeatured]);

        var result = await sut.Handle(new ListPublicNewsQuery(Page: 1, PageSize: 20, IsFeatured: true), CancellationToken.None);

        result.Data!.Total.Should().Be(1);
        result.Data.Items.Single().TitleEn.Should().Be("Featured");
        result.Data.Items.Single().IsFeatured.Should().BeTrue();
    }

    private static ListPublicNewsQueryHandler BuildSut(IEnumerable<News> news)
    {
        var db = Substitute.For<ICceDbContext>();
        db.News.Returns(news.AsQueryable());
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return new ListPublicNewsQueryHandler(db, new MessageFactory(localization, Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageFactory>.Instance));
    }
}
