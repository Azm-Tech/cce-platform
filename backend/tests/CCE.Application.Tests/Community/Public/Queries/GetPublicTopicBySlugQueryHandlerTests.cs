using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Public.Queries.GetPublicTopicBySlug;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Public.Queries;

public class GetPublicTopicBySlugQueryHandlerTests
{
    [Fact]
    public async Task Returns_dto_when_active_topic_with_matching_slug_exists()
    {
        var topic = Topic.Create("طاقة", "Energy", "وصف", "Description", "energy", null, null, 1);
        var db = BuildDb(new[] { topic });
        var sut = new GetPublicTopicBySlugQueryHandler(db);

        var result = await sut.Handle(new GetPublicTopicBySlugQuery("energy"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Slug.Should().Be("energy");
        result.NameEn.Should().Be("Energy");
    }

    [Fact]
    public async Task Returns_null_when_slug_not_found()
    {
        var db = BuildDb(System.Array.Empty<Topic>());
        var sut = new GetPublicTopicBySlugQueryHandler(db);

        var result = await sut.Handle(new GetPublicTopicBySlugQuery("no-such-slug"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_null_when_topic_is_inactive()
    {
        var topic = Topic.Create("طاقة", "Energy", "وصف", "Description", "energy", null, null, 1);
        topic.Deactivate();
        var db = BuildDb(new[] { topic });
        var sut = new GetPublicTopicBySlugQueryHandler(db);

        var result = await sut.Handle(new GetPublicTopicBySlugQuery("energy"), CancellationToken.None);

        result.Should().BeNull();
    }

    private static ICceDbContext BuildDb(IEnumerable<Topic> topics)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Topics.Returns(topics.AsQueryable());
        return db;
    }
}
