using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Content.Events;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Content;

public class EventTests
{
    private static FakeSystemClock NewClock() => new();

    private static Event NewEvent(FakeSystemClock clock) =>
        Event.Schedule(
            titleAr: "حدث",
            titleEn: "Event",
            descriptionAr: "وصف",
            descriptionEn: "Description",
            startsOn: clock.UtcNow.AddDays(7),
            endsOn: clock.UtcNow.AddDays(7).AddHours(2),
            locationAr: "الرياض",
            locationEn: "Riyadh",
            onlineMeetingUrl: null,
            featuredImageUrl: null,
            clock: clock);

    [Fact]
    public void Schedule_creates_event_with_generated_ICalUid()
    {
        var clock = NewClock();
        var e = NewEvent(clock);

        e.ICalUid.Should().NotBeNullOrWhiteSpace();
        e.ICalUid.Length.Should().BeGreaterThan(10);
    }

    [Fact]
    public void Schedule_raises_EventScheduledEvent()
    {
        var clock = NewClock();
        var e = NewEvent(clock);
        e.DomainEvents.OfType<EventScheduledEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void EndsOn_must_be_after_StartsOn()
    {
        var clock = NewClock();
        var act = () => Event.Schedule("ا", "x", "ا", "x",
            clock.UtcNow.AddDays(7),
            clock.UtcNow.AddDays(7),
            null, null, null, null, clock);
        act.Should().Throw<DomainException>().WithMessage("*EndsOn*");
    }

    [Fact]
    public void EndsOn_before_StartsOn_throws()
    {
        var clock = NewClock();
        var act = () => Event.Schedule("ا", "x", "ا", "x",
            clock.UtcNow.AddDays(7),
            clock.UtcNow.AddDays(6),
            null, null, null, null, clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void OnlineMeetingUrl_must_be_https()
    {
        var clock = NewClock();
        var act = () => Event.Schedule("ا", "x", "ا", "x",
            clock.UtcNow.AddDays(7),
            clock.UtcNow.AddDays(7).AddHours(2),
            null, null, "http://insecure", null, clock);
        act.Should().Throw<DomainException>().WithMessage("*https*");
    }

    [Fact]
    public void Reschedule_updates_window_when_valid()
    {
        var clock = NewClock();
        var e = NewEvent(clock);
        var newStart = clock.UtcNow.AddDays(14);
        var newEnd = newStart.AddHours(3);

        e.Reschedule(newStart, newEnd);

        e.StartsOn.Should().Be(newStart);
        e.EndsOn.Should().Be(newEnd);
    }

    [Fact]
    public void Reschedule_with_invalid_window_throws()
    {
        var clock = NewClock();
        var e = NewEvent(clock);
        var act = () => e.Reschedule(clock.UtcNow.AddDays(14), clock.UtcNow.AddDays(13));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ICalUid_does_not_change_after_reschedule()
    {
        var clock = NewClock();
        var e = NewEvent(clock);
        var uid = e.ICalUid;

        e.Reschedule(clock.UtcNow.AddDays(14), clock.UtcNow.AddDays(14).AddHours(1));

        e.ICalUid.Should().Be(uid);
    }
}
