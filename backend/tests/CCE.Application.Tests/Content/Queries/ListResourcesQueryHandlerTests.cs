using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.ListResources;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;
using DomainCountry = CCE.Domain.Country;

namespace CCE.Application.Tests.Content.Queries;

public class ListResourcesQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_empty_paged_result_when_no_resources_exist()
    {
        var sut = BuildSut(Array.Empty<Resource>());

        var result = await sut.Handle(new ListResourcesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.Total.Should().Be(0);
        result.Data.Page.Should().Be(1);
        result.Data.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Returns_resources_sorted_by_PublishedOn_descending()
    {
        var cat = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var older = Resource.Draft("أ", "A", "وصف أ", "Desc A",
            ResourceType.Paper, cat, null, uploader, asset, System.Array.Empty<System.Guid>(), Clock);
        older.Publish(Clock);
        Clock.Advance(System.TimeSpan.FromSeconds(1));
        var newer = Resource.Draft("ب", "B", "وصف ب", "Desc B",
            ResourceType.Article, cat, null, uploader, asset, System.Array.Empty<System.Guid>(), Clock);
        newer.Publish(Clock);

        var sut = BuildSut([newer, older]);

        var result = await sut.Handle(new ListResourcesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Data!.Total.Should().Be(2);
        result.Data.Items.Should().HaveCount(2);
        result.Data.Items[0].TitleEn.Should().Be("B");
        result.Data.Items[1].TitleEn.Should().Be("A");
    }

    [Fact]
    public async Task Search_filter_matches_title_ar_title_en_description_ar_or_description_en()
    {
        var cat = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var resource = Resource.Draft("مطابق", "matching", "وصف", "desc",
            ResourceType.Paper, cat, null, uploader, asset, System.Array.Empty<System.Guid>(), Clock);

        var sut = BuildSut([resource]);

        var result = await sut.Handle(new ListResourcesQuery(Search: "matching"), CancellationToken.None);

        result.Data!.Total.Should().Be(1);
        result.Data.Items.Single().TitleEn.Should().Be("matching");
    }

    [Fact]
    public async Task IsPublished_filter_returns_only_published_resources()
    {
        var cat = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var published = Resource.Draft("منشور", "published", "وصف", "desc",
            ResourceType.Paper, cat, null, uploader, asset, System.Array.Empty<System.Guid>(), Clock);
        published.Publish(Clock);

        var draft = Resource.Draft("مسودة", "draft", "وصف", "desc",
            ResourceType.Paper, cat, null, uploader, asset, System.Array.Empty<System.Guid>(), Clock);

        var sut = BuildSut([published, draft]);

        var result = await sut.Handle(new ListResourcesQuery(IsPublished: true), CancellationToken.None);

        result.Data!.Total.Should().Be(1);
        result.Data.Items.Single().TitleEn.Should().Be("published");
        result.Data.Items.Single().IsPublished.Should().BeTrue();
    }

    [Fact]
    public async Task CategoryId_filter_returns_only_matching_resources()
    {
        var catA = System.Guid.NewGuid();
        var catB = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var match = Resource.Draft("أ", "Match", "وصف", "desc",
            ResourceType.Paper, catA, null, uploader, asset, System.Array.Empty<System.Guid>(), Clock);
        var noMatch = Resource.Draft("ب", "NoMatch", "وصف", "desc",
            ResourceType.Paper, catB, null, uploader, asset, System.Array.Empty<System.Guid>(), Clock);

        var sut = BuildSut([match, noMatch]);

        var result = await sut.Handle(new ListResourcesQuery(CategoryId: catA), CancellationToken.None);

        result.Data!.Total.Should().Be(1);
        result.Data.Items.Single().TitleEn.Should().Be("Match");
    }

    private static ListResourcesQueryHandler BuildSut(IEnumerable<Resource> resources)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Resources.Returns(resources.AsQueryable());
        db.ResourceCategories.Returns(Array.Empty<ResourceCategory>().AsQueryable());
        db.AssetFiles.Returns(Array.Empty<AssetFile>().AsQueryable());
        db.Countries.Returns(Array.Empty<DomainCountry.Country>().AsQueryable());
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return new ListResourcesQueryHandler(db, new MessageFactory(localization, Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageFactory>.Instance));
    }
}
