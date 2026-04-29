using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Queries.ListEvents;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Content.Queries;

public class ListEventsQueryHandlerTests
{
    private static readonly System.DateTimeOffset BaseTime =
        new(2026, 6, 1, 10, 0, 0, System.TimeSpan.Zero);

    [Fact]
    public async Task Returns_empty_paged_result_when_no_events_exist()
    {
        var db = BuildDb(System.Array.Empty<CCE.Domain.Content.Event>());
        var sut = new ListEventsQueryHandler(db);

        var result = await sut.Handle(new ListEventsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Returns_events_sorted_by_StartsOn_descending()
    {
        var clock = new FakeSystemClock();

        var earlier = CCE.Domain.Content.Event.Schedule(
            "أ", "Earlier Event", "وصف", "Description A",
            BaseTime, BaseTime.AddHours(2),
            null, null, null, null, clock);

        var later = CCE.Domain.Content.Event.Schedule(
            "ب", "Later Event", "وصف ب", "Description B",
            BaseTime.AddDays(1), BaseTime.AddDays(1).AddHours(2),
            null, null, null, null, clock);

        var db = BuildDb(new[] { earlier, later });
        var sut = new ListEventsQueryHandler(db);

        var result = await sut.Handle(new ListEventsQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].TitleEn.Should().Be("Later Event");
        result.Items[1].TitleEn.Should().Be("Earlier Event");
    }

    [Fact]
    public async Task Search_filter_matches_title_ar_or_title_en()
    {
        var clock = new FakeSystemClock();

        var match = CCE.Domain.Content.Event.Schedule(
            "مطابق", "matching-event", "وصف", "Description",
            BaseTime, BaseTime.AddHours(1),
            null, null, null, null, clock);

        var noMatch = CCE.Domain.Content.Event.Schedule(
            "آخر", "other-event", "وصف آخر", "Other description",
            BaseTime.AddDays(1), BaseTime.AddDays(1).AddHours(1),
            null, null, null, null, clock);

        var db = BuildDb(new[] { match, noMatch });
        var sut = new ListEventsQueryHandler(db);

        var result = await sut.Handle(new ListEventsQuery(Search: "matching"), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().TitleEn.Should().Be("matching-event");
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
