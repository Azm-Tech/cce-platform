using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Queries.GetTopicById;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Queries;

public class GetTopicByIdQueryHandlerTests
{
    [Fact]
    public async Task Returns_null_when_topic_not_found()
    {
        var db = BuildDb(System.Array.Empty<Topic>());
        var sut = new GetTopicByIdQueryHandler(db);

        var result = await sut.Handle(new GetTopicByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_dto_with_all_fields_when_found()
    {
        var topic = Topic.Create("طاقة", "Energy", "وصف الطاقة", "Energy description", "energy", null, null, 5);

        var db = BuildDb(new[] { topic });
        var sut = new GetTopicByIdQueryHandler(db);

        var result = await sut.Handle(new GetTopicByIdQuery(topic.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(topic.Id);
        result.NameAr.Should().Be("طاقة");
        result.NameEn.Should().Be("Energy");
        result.DescriptionAr.Should().Be("وصف الطاقة");
        result.DescriptionEn.Should().Be("Energy description");
        result.Slug.Should().Be("energy");
        result.ParentId.Should().BeNull();
        result.OrderIndex.Should().Be(5);
        result.IsActive.Should().BeTrue();
    }

    private static ICceDbContext BuildDb(IEnumerable<Topic> topics)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Topics.Returns(topics.AsQueryable());
        return db;
    }
}
