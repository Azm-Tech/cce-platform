using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.ListPublicResources;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Public.Queries;

public class ListPublicResourcesQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_paged_result_when_no_resources_exist()
    {
        var db = BuildDb(System.Array.Empty<Resource>());
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
        var clock = new FakeSystemClock();
        var categoryId = System.Guid.NewGuid();
        var uploadedById = System.Guid.NewGuid();
        var assetFileId = System.Guid.NewGuid();

        var published = Resource.Draft("عنوان", "Published", "وصف", "Description",
            ResourceType.Document, categoryId, null, uploadedById, assetFileId, clock);
        var draft = Resource.Draft("مسودة", "Draft", "وصف", "Description",
            ResourceType.Document, categoryId, null, uploadedById, assetFileId, clock);
        published.Publish(clock);

        var db = BuildDb(new[] { published, draft });
        var sut = new ListPublicResourcesQueryHandler(db);

        var result = await sut.Handle(new ListPublicResourcesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("Published");
    }

    [Fact]
    public async Task CategoryId_filter_returns_only_matching_published_resources()
    {
        var clock = new FakeSystemClock();
        var categoryA = System.Guid.NewGuid();
        var categoryB = System.Guid.NewGuid();
        var uploadedById = System.Guid.NewGuid();
        var assetFileId = System.Guid.NewGuid();

        var inCategoryA = Resource.Draft("فئة أ", "Category A", "وصف", "Description",
            ResourceType.Document, categoryA, null, uploadedById, assetFileId, clock);
        var inCategoryB = Resource.Draft("فئة ب", "Category B", "وصف", "Description",
            ResourceType.Document, categoryB, null, uploadedById, assetFileId, clock);
        inCategoryA.Publish(clock);
        inCategoryB.Publish(clock);

        var db = BuildDb(new[] { inCategoryA, inCategoryB });
        var sut = new ListPublicResourcesQueryHandler(db);

        var result = await sut.Handle(new ListPublicResourcesQuery(Page: 1, PageSize: 20, CategoryId: categoryA), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("Category A");
        result.Items.Single().CategoryId.Should().Be(categoryA);
    }

    private static ICceDbContext BuildDb(IEnumerable<Resource> resources)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Resources.Returns(resources.AsQueryable());
        db.Users.Returns(System.Array.Empty<CCE.Domain.Identity.User>().AsQueryable());
        db.Roles.Returns(System.Array.Empty<CCE.Domain.Identity.Role>().AsQueryable());
        db.UserRoles.Returns(System.Array.Empty<Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>>().AsQueryable());
        db.News.Returns(System.Array.Empty<CCE.Domain.Content.News>().AsQueryable());
        return db;
    }
}
