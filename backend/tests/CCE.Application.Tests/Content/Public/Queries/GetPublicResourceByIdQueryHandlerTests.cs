using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.GetPublicResourceById;
using CCE.Application.Localization;
using CCE.Application.Messages;
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
            ResourceType.ScientificPaper, cat, null, uploader, asset, System.Array.Empty<System.Guid>(), Clock);
        resource.Publish(Clock);

        var sut = BuildSut([resource]);

        var result = await sut.Handle(new GetPublicResourceByIdQuery(resource.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(resource.Id);
        result.Data.TitleEn.Should().Be("Published Resource");
    }

    [Fact]
    public async Task Returns_not_found_when_resource_missing()
    {
        var sut = BuildSut(Array.Empty<Resource>());

        var result = await sut.Handle(new GetPublicResourceByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_not_found_when_resource_exists_but_is_not_published()
    {
        var cat = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var resource = Resource.Draft("مسودة", "Draft Resource", "وصف", "Description",
            ResourceType.ScientificPaper, cat, null, uploader, asset, System.Array.Empty<System.Guid>(), Clock);

        var sut = BuildSut([resource]);

        var result = await sut.Handle(new GetPublicResourceByIdQuery(resource.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    private static GetPublicResourceByIdQueryHandler BuildSut(IEnumerable<Resource> resources)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Resources.Returns(resources.AsQueryable());
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return new GetPublicResourceByIdQueryHandler(db, new MessageFactory(localization));
    }
}
