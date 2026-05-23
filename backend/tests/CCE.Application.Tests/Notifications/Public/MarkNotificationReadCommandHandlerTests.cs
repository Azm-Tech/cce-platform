using CCE.Application.Common.Interfaces;
using CCE.Application.Tests.Notifications;
using CCE.Application.Notifications.Public;
using CCE.Application.Notifications.Public.Commands.MarkNotificationRead;
using CCE.Domain.Common;
using CCE.Domain.Notifications;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Notifications.Public;

public class MarkNotificationReadCommandHandlerTests
{
    private static (UserNotification notification, FakeSystemClock clock) MakeSentNotification(System.Guid userId)
    {
        var clock = new FakeSystemClock();
        var n = UserNotification.Render(userId, System.Guid.NewGuid(),
            "موضوع", "Subject", "نص", "ar", NotificationChannel.Email);
        n.MarkSent(clock);
        return (n, clock);
    }

    [Fact]
    public async Task Returns_not_found_response_when_notification_not_found_or_belongs_to_different_user()
    {
        var repo = Substitute.For<IUserNotificationRepository>();
        var clock = new FakeSystemClock();
        repo.GetAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns((UserNotification?)null);

        var db = Substitute.For<ICceDbContext>();
        var sut = new MarkNotificationReadCommandHandler(repo, db, NotificationTestMessages.Create(), clock);
        var cmd = new MarkNotificationReadCommand(System.Guid.NewGuid(), System.Guid.NewGuid());

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Marks_notification_as_read_and_calls_update()
    {
        var userId = System.Guid.NewGuid();
        var (notif, clock) = MakeSentNotification(userId);

        var repo = Substitute.For<IUserNotificationRepository>();
        repo.GetAsync(notif.Id, Arg.Any<CancellationToken>())
            .Returns(notif);

        var db = Substitute.For<ICceDbContext>();
        var sut = new MarkNotificationReadCommandHandler(repo, db, NotificationTestMessages.Create(), clock);
        var cmd = new MarkNotificationReadCommand(notif.Id, userId);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Success.Should().BeTrue();
        notif.Status.Should().Be(NotificationStatus.Read);
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
