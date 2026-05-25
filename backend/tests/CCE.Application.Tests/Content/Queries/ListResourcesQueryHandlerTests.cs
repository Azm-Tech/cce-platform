using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.ListResources;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Queries;

public class ListResourcesQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_empty_paged_result_when_no_resources_exist()
    {
        var db = BuildDb(Array.Empty<Resource>());
        var sut = new ListResourcesQueryHandler(db);

        var result = await sut.Handle(new ListResourcesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Returns_resources_sorted_by_PublishedOn_descending()
    {
        var cat = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var older = Resource.Draft("أ", "A", "وصف أ", "Desc A",
            ResourceType.Pdf, cat, null, uploader, asset, Clock);
        older.Publish(Clock);
        Clock.Advance(System.TimeSpan.FromSeconds(1));
        var newer = Resource.Draft("ب", "B", "وصف ب", "Desc B",
            ResourceType.Video, cat, null, uploader, asset, Clock);
        newer.Publish(Clock);

        var db = BuildDb([newer, older]);
        var sut = new ListResourcesQueryHandler(db);

        var result = await sut.Handle(new ListResourcesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].TitleEn.Should().Be("B");
        result.Items[1].TitleEn.Should().Be("A");
    }

    [Fact]
    public async Task Search_filter_matches_title_ar_title_en_description_ar_or_description_en()
    {
        var cat = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var resource = Resource.Draft("مطابق", "matching", "وصف", "desc",
            ResourceType.Pdf, cat, null, uploader, asset, Clock);

        var db = BuildDb([resource]);
        var sut = new ListResourcesQueryHandler(db);

        var result = await sut.Handle(new ListResourcesQuery(Search: "matching"), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("matching");
    }

    [Fact]
    public async Task IsPublished_filter_returns_only_published_resources()
    {
        var cat = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var published = Resource.Draft("منشور", "published", "وصف", "desc",
            ResourceType.Pdf, cat, null, uploader, asset, Clock);
        published.Publish(Clock);

        var draft = Resource.Draft("مسودة", "draft", "وصف", "desc",
            ResourceType.Pdf, cat, null, uploader, asset, Clock);

        var db = BuildDb([published, draft]);
        var sut = new ListResourcesQueryHandler(db);

        var result = await sut.Handle(new ListResourcesQuery(IsPublished: true), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("published");
        result.Items.Single().IsPublished.Should().BeTrue();
    }

    [Fact]
    public async Task CategoryId_filter_returns_only_matching_resources()
    {
        var catA = System.Guid.NewGuid();
        var catB = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var match = Resource.Draft("أ", "Match", "وصف", "desc",
            ResourceType.Pdf, catA, null, uploader, asset, Clock);
        var noMatch = Resource.Draft("ب", "NoMatch", "وصف", "desc",
            ResourceType.Pdf, catB, null, uploader, asset, Clock);

        var db = BuildDb([match, noMatch]);
        var sut = new ListResourcesQueryHandler(db);

        var result = await sut.Handle(new ListResourcesQuery(CategoryId: catA), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("Match");
    }

    private static ICceDbContext BuildDb(IEnumerable<Resource> resources)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Resources.Returns(resources.AsQueryable());
        return db;
    }
}
