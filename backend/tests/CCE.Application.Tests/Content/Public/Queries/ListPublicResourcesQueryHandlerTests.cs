using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.ListPublicResources;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;
using DomainCountry = CCE.Domain.Country;

namespace CCE.Application.Tests.Content.Public.Queries;

public class ListPublicResourcesQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_empty_paged_result_when_no_resources_exist()
    {
        var sut = BuildSut(Array.Empty<Resource>());

        var result = await sut.Handle(new ListPublicResourcesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.Total.Should().Be(0);
        result.Data.Page.Should().Be(1);
        result.Data.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Only_published_resources_are_returned()
    {
        var cat = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var published = Resource.Draft("عنوان", "Published", "وصف", "Description",
            ResourceType.ScientificPaper, cat, null, uploader, asset, System.Array.Empty<System.Guid>(), Clock);
        published.Publish(Clock);

        var draft = Resource.Draft("مسودة", "Draft", "وصف", "Description",
            ResourceType.ScientificPaper, cat, null, uploader, asset, System.Array.Empty<System.Guid>(), Clock);

        var sut = BuildSut([published, draft]);

        var result = await sut.Handle(new ListPublicResourcesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Data!.Total.Should().Be(1);
        result.Data.Items.Single().TitleEn.Should().Be("Published");
    }

    [Fact]
    public async Task CategoryId_filter_returns_only_matching_published_resources()
    {
        var catA = System.Guid.NewGuid();
        var catB = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var match = Resource.Draft("فئة أ", "Category A", "وصف", "Description",
            ResourceType.ScientificPaper, catA, null, uploader, asset, System.Array.Empty<System.Guid>(), Clock);
        match.Publish(Clock);

        var noMatch = Resource.Draft("فئة ب", "Category B", "وصف", "Description",
            ResourceType.ScientificPaper, catB, null, uploader, asset, System.Array.Empty<System.Guid>(), Clock);
        noMatch.Publish(Clock);

        var sut = BuildSut([match, noMatch]);

        var result = await sut.Handle(new ListPublicResourcesQuery(Page: 1, PageSize: 20, CategoryId: catA), CancellationToken.None);

        result.Data!.Total.Should().Be(1);
        result.Data.Items.Single().TitleEn.Should().Be("Category A");
        result.Data.Items.Single().CategoryId.Should().Be(catA);
    }

    [Fact]
    public async Task ResourceType_filter_returns_only_matching_published_resources()
    {
        var cat = System.Guid.NewGuid();
        var uploader = System.Guid.NewGuid();
        var asset = System.Guid.NewGuid();

        var doc = Resource.Draft("وثيقة", "Document", "وصف", "Description",
            ResourceType.ScientificPaper, cat, null, uploader, asset, System.Array.Empty<System.Guid>(), Clock);
        doc.Publish(Clock);

        var video = Resource.Draft("فيديو", "Video", "وصف", "Description",
            ResourceType.Article, cat, null, uploader, asset, System.Array.Empty<System.Guid>(), Clock);
        video.Publish(Clock);

        var sut = BuildSut([doc, video]);

        var result = await sut.Handle(new ListPublicResourcesQuery(Page: 1, PageSize: 20, ResourceType: ResourceType.Article), CancellationToken.None);

        result.Data!.Total.Should().Be(1);
        result.Data.Items.Single().TitleEn.Should().Be("Video");
    }

    private static ListPublicResourcesQueryHandler BuildSut(IEnumerable<Resource> resources)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Resources.Returns(resources.AsQueryable());
        db.ResourceCategories.Returns(Array.Empty<ResourceCategory>().AsQueryable());
        db.AssetFiles.Returns(Array.Empty<AssetFile>().AsQueryable());
        db.Countries.Returns(Array.Empty<DomainCountry.Country>().AsQueryable());
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return new ListPublicResourcesQueryHandler(db, new MessageFactory(localization, Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageFactory>.Instance));
    }
}
