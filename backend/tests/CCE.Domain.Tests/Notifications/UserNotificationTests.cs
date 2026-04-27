using CCE.Domain.Common;
using CCE.Domain.Notifications;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Notifications;

public class UserNotificationTests
{
    private static UserNotification NewPending() => UserNotification.Render(
        System.Guid.NewGuid(), System.Guid.NewGuid(),
        "موضوع", "Subject", "نص", "ar", NotificationChannel.Email);

    [Fact]
    public void Render_creates_pending() {
        var n = NewPending();
        n.Status.Should().Be(NotificationStatus.Pending);
        n.SentOn.Should().BeNull();
    }

    [Fact]
    public void MarkSent_transitions_from_Pending() {
        var clock = new FakeSystemClock();
        var n = NewPending();
        n.MarkSent(clock);
        n.Status.Should().Be(NotificationStatus.Sent);
        n.SentOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void MarkRead_transitions_from_Sent() {
        var clock = new FakeSystemClock();
        var n = NewPending();
        n.MarkSent(clock);
        clock.Advance(System.TimeSpan.FromMinutes(5));
        n.MarkRead(clock);
        n.Status.Should().Be(NotificationStatus.Read);
        n.ReadOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void MarkFailed_from_Pending() {
        var clock = new FakeSystemClock();
        var n = NewPending();
        n.MarkFailed(clock);
        n.Status.Should().Be(NotificationStatus.Failed);
    }

    [Fact]
    public void Cannot_send_already_sent() {
        var clock = new FakeSystemClock();
        var n = NewPending();
        n.MarkSent(clock);
        var act = () => n.MarkSent(clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cannot_mark_pending_as_read() {
        var clock = new FakeSystemClock();
        var n = NewPending();
        var act = () => n.MarkRead(clock);
        act.Should().Throw<DomainException>().WithMessage("*Sent*");
    }

    [Fact]
    public void Invalid_locale_throws() {
        var act = () => UserNotification.Render(
            System.Guid.NewGuid(), System.Guid.NewGuid(), "ا", "x", "y", "fr", NotificationChannel.Sms);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UserNotification_is_NOT_audited() {
        typeof(UserNotification).GetCustomAttributes(typeof(AuditedAttribute), inherit: false)
            .Should().BeEmpty();
    }
}
