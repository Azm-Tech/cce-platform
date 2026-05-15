using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.GetPublicResourceById;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Public.Queries;

public class GetPublicResourceByIdQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_dto_when_resource_is_published()
    {
        var cat = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var resource = Resource.Draft("عنوان", "Published Resource", "وصف", "Description",
            ResourceType.Document, cat, null, uploader, asset, Clock);
        resource.Publish(Clock);

        var db = BuildDb([resource]);
        var sut = new GetPublicResourceByIdQueryHandler(db);

        var result = await sut.Handle(new GetPublicResourceByIdQuery(resource.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(resource.Id);
        result.TitleEn.Should().Be("Published Resource");
    }

    [Fact]
    public async Task Returns_null_when_resource_not_found()
    {
        var db = BuildDb(Array.Empty<Resource>());
        var sut = new GetPublicResourceByIdQueryHandler(db);

        var result = await sut.Handle(new GetPublicResourceByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_null_when_resource_exists_but_is_not_published()
    {
        var cat = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var resource = Resource.Draft("مسودة", "Draft Resource", "وصف", "Description",
            ResourceType.Document, cat, null, uploader, asset, Clock);

        var db = BuildDb([resource]);
        var sut = new GetPublicResourceByIdQueryHandler(db);

        var result = await sut.Handle(new GetPublicResourceByIdQuery(resource.Id), CancellationToken.None);

        result.Should().BeNull();
    }

    private static ICceDbContext BuildDb(IEnumerable<Resource> resources)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Resources.Returns(resources.AsQueryable());
        return db;
    }
}
