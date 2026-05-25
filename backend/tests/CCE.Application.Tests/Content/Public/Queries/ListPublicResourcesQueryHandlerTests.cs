using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.ListPublicResources;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Public.Queries;

public class ListPublicResourcesQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_empty_paged_result_when_no_resources_exist()
    {
        var db = BuildDb(Array.Empty<Resource>());
        var sut = new ListPublicResourcesQueryHandler(db);

        var result = await sut.Handle(new ListPublicResourcesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Only_published_resources_are_returned()
    {
        var cat = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var published = Resource.Draft("عنوان", "Published", "وصف", "Description",
            ResourceType.Document, cat, null, uploader, asset, Clock);
        published.Publish(Clock);

        var draft = Resource.Draft("مسودة", "Draft", "وصف", "Description",
            ResourceType.Document, cat, null, uploader, asset, Clock);

        var db = BuildDb([published, draft]);
        var sut = new ListPublicResourcesQueryHandler(db);

        var result = await sut.Handle(new ListPublicResourcesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("Published");
    }

    [Fact]
    public async Task CategoryId_filter_returns_only_matching_published_resources()
    {
        var catA = System.Guid.NewGuid();
        var catB = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var match = Resource.Draft("فئة أ", "Category A", "وصف", "Description",
            ResourceType.Document, catA, null, uploader, asset, Clock);
        match.Publish(Clock);

        var noMatch = Resource.Draft("فئة ب", "Category B", "وصف", "Description",
            ResourceType.Document, catB, null, uploader, asset, Clock);
        noMatch.Publish(Clock);

        var db = BuildDb([match, noMatch]);
        var sut = new ListPublicResourcesQueryHandler(db);

        var result = await sut.Handle(new ListPublicResourcesQuery(Page: 1, PageSize: 20, CategoryId: catA), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("Category A");
        result.Items.Single().CategoryId.Should().Be(catA);
    }

    [Fact]
    public async Task ResourceType_filter_returns_only_matching_published_resources()
    {
        var cat = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var doc = Resource.Draft("وثيقة", "Document", "وصف", "Description",
            ResourceType.Document, cat, null, uploader, asset, Clock);
        doc.Publish(Clock);

        var video = Resource.Draft("فيديو", "Video", "وصف", "Description",
            ResourceType.Video, cat, null, uploader, asset, Clock);
        video.Publish(Clock);

        var db = BuildDb([doc, video]);
        var sut = new ListPublicResourcesQueryHandler(db);

        var result = await sut.Handle(new ListPublicResourcesQuery(Page: 1, PageSize: 20, ResourceType: ResourceType.Video), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("Video");
    }

    private static ICceDbContext BuildDb(IEnumerable<Resource> resources)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Resources.Returns(resources.AsQueryable());
        return db;
    }
}
