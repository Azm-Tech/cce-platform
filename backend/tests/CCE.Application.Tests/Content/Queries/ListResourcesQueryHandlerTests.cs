using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.ListResources;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Queries;

public class ListResourcesQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_paged_result_when_no_resources_exist()
    {
        var db = BuildDb(System.Array.Empty<Resource>());
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
        var clock = new FakeSystemClock();
        var cat = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var older = Resource.Draft("أ", "A", "وصف أ", "Desc A", ResourceType.Pdf, cat, null, uploader, asset, clock);
        var newer = Resource.Draft("ب", "B", "وصف ب", "Desc B", ResourceType.Video, cat, null, uploader, asset, clock);

        older.Publish(clock);
        clock.Advance(System.TimeSpan.FromMinutes(5));
        newer.Publish(clock);

        var db = BuildDb(new[] { older, newer });
        var sut = new ListResourcesQueryHandler(db);

        var result = await sut.Handle(new ListResourcesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].TitleEn.Should().Be("B");
        result.Items[1].TitleEn.Should().Be("A");
    }

    [Fact]
    public async Task Search_filter_matches_title_ar_or_title_en()
    {
        var clock = new FakeSystemClock();
        var cat = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var match = Resource.Draft("مطابق", "matching", "وصف", "desc", ResourceType.Pdf, cat, null, uploader, asset, clock);
        var noMatch = Resource.Draft("آخر", "other", "وصف آخر", "other desc", ResourceType.Pdf, cat, null, uploader, asset, clock);

        var db = BuildDb(new[] { match, noMatch });
        var sut = new ListResourcesQueryHandler(db);

        var result = await sut.Handle(new ListResourcesQuery(Search: "matching"), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("matching");
    }

    [Fact]
    public async Task IsPublished_filter_returns_only_published_resources()
    {
        var clock = new FakeSystemClock();
        var cat = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var published = Resource.Draft("منشور", "published", "وصف", "desc", ResourceType.Pdf, cat, null, uploader, asset, clock);
        var draft = Resource.Draft("مسودة", "draft-resource", "وصف", "desc", ResourceType.Pdf, cat, null, uploader, asset, clock);
        published.Publish(clock);

        var db = BuildDb(new[] { published, draft });
        var sut = new ListResourcesQueryHandler(db);

        var result = await sut.Handle(new ListResourcesQuery(IsPublished: true), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("published");
        result.Items.Single().IsPublished.Should().BeTrue();
    }

    private static ICceDbContext BuildDb(IEnumerable<Resource> resources)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Resources.Returns(resources.AsQueryable());
        db.Users.Returns(System.Array.Empty<CCE.Domain.Identity.User>().AsQueryable());
        db.Roles.Returns(System.Array.Empty<CCE.Domain.Identity.Role>().AsQueryable());
        db.UserRoles.Returns(System.Array.Empty<Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>>().AsQueryable());
        db.AssetFiles.Returns(System.Array.Empty<CCE.Domain.Content.AssetFile>().AsQueryable());
        return db;
    }
}
