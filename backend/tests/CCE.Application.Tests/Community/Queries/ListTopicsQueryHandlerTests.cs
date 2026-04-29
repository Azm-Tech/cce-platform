using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Queries.ListTopics;
using CCE.Domain.Community;

namespace CCE.Application.Tests.Community.Queries;

public class ListTopicsQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_paged_result_when_no_topics_exist()
    {
        var db = BuildDb(System.Array.Empty<Topic>());
        var sut = new ListTopicsQueryHandler(db);

        var result = await sut.Handle(new ListTopicsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task IsActive_filter_returns_only_active_topics()
    {
        var active = Topic.Create("طاقة", "Energy", "وصف", "Description", "energy", null, null, 1);
        var inactive = Topic.Create("بيئة", "Environment", "وصف", "Description", "environment", null, null, 2);
        inactive.Deactivate();

        var db = BuildDb(new[] { active, inactive });
        var sut = new ListTopicsQueryHandler(db);

        var result = await sut.Handle(new ListTopicsQuery(IsActive: true), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().NameEn.Should().Be("Energy");
    }

    [Fact]
    public async Task Search_filter_returns_topics_matching_NameEn()
    {
        var topic1 = Topic.Create("طاقة", "Energy", "وصف", "Description", "energy", null, null, 1);
        var topic2 = Topic.Create("بيئة", "Environment", "وصف", "Description", "environment", null, null, 2);

        var db = BuildDb(new[] { topic1, topic2 });
        var sut = new ListTopicsQueryHandler(db);

        var result = await sut.Handle(new ListTopicsQuery(Search: "Energy"), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().NameEn.Should().Be("Energy");
    }

    private static ICceDbContext BuildDb(IEnumerable<Topic> topics)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Topics.Returns(topics.AsQueryable());
        return db;
    }
}
