using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.GetPublicResourceById;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Public.Queries;

public class GetPublicResourceByIdQueryHandlerTests
{
    [Fact]
    public async Task Returns_dto_when_resource_is_published()
    {
        var clock = new FakeSystemClock();
        var categoryId = System.Guid.NewGuid();
        var uploadedById = System.Guid.NewGuid();
        var assetFileId = System.Guid.NewGuid();

        var resource = Resource.Draft("عنوان", "Published Resource", "وصف", "Description",
            ResourceType.Document, categoryId, null, uploadedById, assetFileId, clock);
        resource.Publish(clock);

        var db = BuildDb(new[] { resource });
        var sut = new GetPublicResourceByIdQueryHandler(db);

        var result = await sut.Handle(new GetPublicResourceByIdQuery(resource.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(resource.Id);
        result.TitleEn.Should().Be("Published Resource");
        result.PublishedOn.Should().Be(resource.PublishedOn!.Value);
    }

    [Fact]
    public async Task Returns_null_when_resource_not_found()
    {
        var db = BuildDb(System.Array.Empty<Resource>());
        var sut = new GetPublicResourceByIdQueryHandler(db);

        var result = await sut.Handle(new GetPublicResourceByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_null_when_resource_exists_but_is_not_published()
    {
        var clock = new FakeSystemClock();
        var categoryId = System.Guid.NewGuid();
        var uploadedById = System.Guid.NewGuid();
        var assetFileId = System.Guid.NewGuid();

        var draft = Resource.Draft("مسودة", "Draft Resource", "وصف", "Description",
            ResourceType.Document, categoryId, null, uploadedById, assetFileId, clock);
        // intentionally NOT calling draft.Publish(clock)

        var db = BuildDb(new[] { draft });
        var sut = new GetPublicResourceByIdQueryHandler(db);

        var result = await sut.Handle(new GetPublicResourceByIdQuery(draft.Id), CancellationToken.None);

        result.Should().BeNull();
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
