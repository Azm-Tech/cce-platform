using CCE.Application.Audit.Queries.ListAuditEvents;
using CCE.Application.Common.Interfaces;
using CCE.Domain.Audit;

namespace CCE.Application.Tests.Audit.Queries;

public class ListAuditEventsQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_paged_result_when_no_events_exist()
    {
        var db = BuildDb(System.Array.Empty<AuditEvent>());
        var sut = new ListAuditEventsQueryHandler(db);

        var result = await sut.Handle(new ListAuditEventsQuery(Page: 1, PageSize: 50), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task Returns_events_sorted_descending_by_occurred_on()
    {
        var now = System.DateTimeOffset.UtcNow;
        var older = new AuditEvent(System.Guid.NewGuid(), now.AddMinutes(-10), "user:a", "News.Created", "News/1", System.Guid.NewGuid(), null);
        var newer = new AuditEvent(System.Guid.NewGuid(), now, "user:a", "News.Updated", "News/1", System.Guid.NewGuid(), null);

        var db = BuildDb(new[] { older, newer });
        var sut = new ListAuditEventsQueryHandler(db);

        var result = await sut.Handle(new ListAuditEventsQuery(Page: 1, PageSize: 50), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items[0].OccurredOn.Should().Be(newer.OccurredOn);
        result.Items[1].OccurredOn.Should().Be(older.OccurredOn);
    }

    [Fact]
    public async Task ActionPrefix_filter_restricts_to_matching_prefix()
    {
        var now = System.DateTimeOffset.UtcNow;
        var newsEvent  = new AuditEvent(System.Guid.NewGuid(), now, "user:a", "News.Created",  "News/1", System.Guid.NewGuid(), null);
        var userEvent  = new AuditEvent(System.Guid.NewGuid(), now, "user:a", "User.Created",  "User/2", System.Guid.NewGuid(), null);
        var newsUpdate = new AuditEvent(System.Guid.NewGuid(), now, "user:a", "News.Updated",  "News/1", System.Guid.NewGuid(), null);

        var db = BuildDb(new[] { newsEvent, userEvent, newsUpdate });
        var sut = new ListAuditEventsQueryHandler(db);

        var result = await sut.Handle(
            new ListAuditEventsQuery(ActionPrefix: "News"),
            CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items.Should().OnlyContain(e => e.Action.StartsWith("News."));
    }

    [Fact]
    public async Task DateRange_filter_applies_from_and_to_bounds()
    {
        var base_ = new System.DateTimeOffset(2026, 1, 1, 0, 0, 0, System.TimeSpan.Zero);
        var e1 = new AuditEvent(System.Guid.NewGuid(), base_.AddDays(-1), "user:a", "News.Created", "News/1", System.Guid.NewGuid(), null);
        var e2 = new AuditEvent(System.Guid.NewGuid(), base_,             "user:a", "News.Created", "News/2", System.Guid.NewGuid(), null);
        var e3 = new AuditEvent(System.Guid.NewGuid(), base_.AddDays(1),  "user:a", "News.Created", "News/3", System.Guid.NewGuid(), null);
        var e4 = new AuditEvent(System.Guid.NewGuid(), base_.AddDays(2),  "user:a", "News.Created", "News/4", System.Guid.NewGuid(), null);

        var db = BuildDb(new[] { e1, e2, e3, e4 });
        var sut = new ListAuditEventsQueryHandler(db);

        var result = await sut.Handle(
            new ListAuditEventsQuery(From: base_, To: base_.AddDays(1)),
            CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items.Should().OnlyContain(e => e.OccurredOn >= base_ && e.OccurredOn <= base_.AddDays(1));
    }

    private static ICceDbContext BuildDb(IEnumerable<AuditEvent> events)
    {
        var db = Substitute.For<ICceDbContext>();
        db.AuditEvents.Returns(events.AsQueryable());
        return db;
    }
}
