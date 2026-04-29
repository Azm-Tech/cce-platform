using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Public.Queries.ListPublicEvents;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Public.Queries;

public class ListPublicEventsQueryHandlerTests
{
    private static readonly System.DateTimeOffset BaseTime =
        new(2026, 6, 1, 10, 0, 0, System.TimeSpan.Zero);

    [Fact]
    public async Task Returns_empty_paged_result_when_no_events_exist()
    {
        var db = BuildDb(System.Array.Empty<CCE.Domain.Content.Event>());
        var sut = new ListPublicEventsQueryHandler(db);

        var from = BaseTime;
        var to = BaseTime.AddDays(30);
        var result = await sut.Handle(new ListPublicEventsQuery(Page: 1, PageSize: 20, From: from, To: to), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Returns_events_sorted_by_StartsOn_ascending()
    {
        var clock = new FakeSystemClock();

        var later = CCE.Domain.Content.Event.Schedule(
            "ب", "Later Event", "وصف ب", "Description B",
            BaseTime.AddDays(1), BaseTime.AddDays(1).AddHours(2),
            null, null, null, null, clock);

        var earlier = CCE.Domain.Content.Event.Schedule(
            "أ", "Earlier Event", "وصف", "Description A",
            BaseTime, BaseTime.AddHours(2),
            null, null, null, null, clock);

        var db = BuildDb(new[] { later, earlier });
        var sut = new ListPublicEventsQueryHandler(db);

        var from = BaseTime.AddMinutes(-1);
        var to = BaseTime.AddDays(2);
        var result = await sut.Handle(new ListPublicEventsQuery(Page: 1, PageSize: 20, From: from, To: to), CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].TitleEn.Should().Be("Earlier Event");
        result.Items[1].TitleEn.Should().Be("Later Event");
    }

    [Fact]
    public async Task From_to_range_filter_returns_only_events_in_range()
    {
        var clock = new FakeSystemClock();

        var inRange = CCE.Domain.Content.Event.Schedule(
            "داخل النطاق", "In Range", "وصف", "Description",
            BaseTime.AddDays(5), BaseTime.AddDays(5).AddHours(1),
            null, null, null, null, clock);

        var outOfRange = CCE.Domain.Content.Event.Schedule(
            "خارج النطاق", "Out Of Range", "وصف", "Description",
            BaseTime.AddDays(20), BaseTime.AddDays(20).AddHours(1),
            null, null, null, null, clock);

        var db = BuildDb(new[] { inRange, outOfRange });
        var sut = new ListPublicEventsQueryHandler(db);

        var from = BaseTime;
        var to = BaseTime.AddDays(10);
        var result = await sut.Handle(new ListPublicEventsQuery(Page: 1, PageSize: 20, From: from, To: to), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("In Range");
    }

    private static ICceDbContext BuildDb(IEnumerable<CCE.Domain.Content.Event> events)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Events.Returns(events.AsQueryable());
        db.Users.Returns(System.Array.Empty<CCE.Domain.Identity.User>().AsQueryable());
        db.Roles.Returns(System.Array.Empty<CCE.Domain.Identity.Role>().AsQueryable());
        db.UserRoles.Returns(System.Array.Empty<Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>>().AsQueryable());
        db.Resources.Returns(System.Array.Empty<CCE.Domain.Content.Resource>().AsQueryable());
        return db;
    }
}
