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
    public async Task Throws_KeyNotFoundException_when_notification_not_found_or_belongs_to_different_user()
    {
        var service = Substitute.For<IUserNotificationService>();
        var clock = new FakeSystemClock();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns((UserNotification?)null);

        var sut = new MarkNotificationReadCommandHandler(service, clock);
        var cmd = new MarkNotificationReadCommand(System.Guid.NewGuid(), System.Guid.NewGuid());

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Marks_notification_as_read_and_calls_update()
    {
        var userId = System.Guid.NewGuid();
        var (notif, clock) = MakeSentNotification(userId);

        var service = Substitute.For<IUserNotificationService>();
        service.FindAsync(notif.Id, Arg.Any<CancellationToken>())
            .Returns(notif);

        var sut = new MarkNotificationReadCommandHandler(service, clock);
        var cmd = new MarkNotificationReadCommand(notif.Id, userId);

        await sut.Handle(cmd, CancellationToken.None);

        notif.Status.Should().Be(NotificationStatus.Read);
        await service.Received(1).UpdateAsync(notif, Arg.Any<CancellationToken>());
    }
}
