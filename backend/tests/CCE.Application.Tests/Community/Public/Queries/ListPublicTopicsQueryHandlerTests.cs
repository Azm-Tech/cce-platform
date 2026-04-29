using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Public.Queries.ListPublicTopics;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Public.Queries;

public class ListPublicTopicsQueryHandlerTests
{
    [Fact]
    public async Task Returns_active_topics_sorted_by_order_index()
    {
        var topic1 = Topic.Create("طاقة", "Energy", "وصف الطاقة", "Energy description", "energy", null, null, 2);
        var topic2 = Topic.Create("بيئة", "Environment", "وصف البيئة", "Environment description", "environment", null, null, 1);
        var inactive = Topic.Create("مياه", "Water", "وصف المياه", "Water description", "water", null, null, 0);
        inactive.Deactivate();

        var db = BuildDb(new[] { topic1, topic2, inactive });
        var sut = new ListPublicTopicsQueryHandler(db);

        var result = await sut.Handle(new ListPublicTopicsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].OrderIndex.Should().Be(1);
        result[0].NameEn.Should().Be("Environment");
        result[1].OrderIndex.Should().Be(2);
        result[1].NameEn.Should().Be("Energy");
    }

    [Fact]
    public async Task Returns_empty_when_no_active_topics_exist()
    {
        var inactive = Topic.Create("طاقة", "Energy", "وصف", "Description", "energy", null, null, 1);
        inactive.Deactivate();

        var db = BuildDb(new[] { inactive });
        var sut = new ListPublicTopicsQueryHandler(db);

        var result = await sut.Handle(new ListPublicTopicsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    private static ICceDbContext BuildDb(IEnumerable<Topic> topics)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Topics.Returns(topics.AsQueryable());
        db.Users.Returns(System.Array.Empty<CCE.Domain.Identity.User>().AsQueryable());
        db.Roles.Returns(System.Array.Empty<CCE.Domain.Identity.Role>().AsQueryable());
        db.UserRoles.Returns(System.Array.Empty<Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>>().AsQueryable());
        db.Resources.Returns(System.Array.Empty<CCE.Domain.Content.Resource>().AsQueryable());
        return db;
    }
}
