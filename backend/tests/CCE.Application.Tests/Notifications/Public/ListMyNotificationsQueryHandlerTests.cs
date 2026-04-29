using CCE.Application.Common.Interfaces;
using CCE.Application.Notifications.Public.Queries.ListMyNotifications;
using CCE.Domain.Notifications;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Notifications.Public;

public class ListMyNotificationsQueryHandlerTests
{
    private static UserNotification MakeSent(System.Guid userId)
    {
        var clock = new FakeSystemClock();
        var n = UserNotification.Render(userId, System.Guid.NewGuid(),
            "موضوع", "Subject", "نص", "ar", NotificationChannel.Email);
        n.MarkSent(clock);
        return n;
    }

    [Fact]
    public async Task Returns_empty_when_user_has_no_notifications()
    {
        var db = BuildDb(System.Array.Empty<UserNotification>());
        var sut = new ListMyNotificationsQueryHandler(db);
        var userId = System.Guid.NewGuid();

        var result = await sut.Handle(new ListMyNotificationsQuery(userId), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task Returns_only_notifications_belonging_to_the_requesting_user()
    {
        var myId = System.Guid.NewGuid();
        var otherId = System.Guid.NewGuid();
        var mine = MakeSent(myId);
        var other = MakeSent(otherId);

        var db = BuildDb(new[] { mine, other });
        var sut = new ListMyNotificationsQueryHandler(db);

        var result = await sut.Handle(new ListMyNotificationsQuery(myId), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().Id.Should().Be(mine.Id);
    }

    [Fact]
    public async Task Filters_by_status_when_provided()
    {
        var userId = System.Guid.NewGuid();
        var clock = new FakeSystemClock();
        var sent = UserNotification.Render(userId, System.Guid.NewGuid(),
            "موضوع", "Subject", "نص", "ar", NotificationChannel.Email);
        sent.MarkSent(clock);

        var read = UserNotification.Render(userId, System.Guid.NewGuid(),
            "موضوع", "Subject", "نص", "ar", NotificationChannel.Email);
        read.MarkSent(clock);
        read.MarkRead(clock);

        var db = BuildDb(new[] { sent, read });
        var sut = new ListMyNotificationsQueryHandler(db);

        var result = await sut.Handle(
            new ListMyNotificationsQuery(userId, Status: NotificationStatus.Sent),
            CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().Status.Should().Be(NotificationStatus.Sent);
    }

    private static ICceDbContext BuildDb(IEnumerable<UserNotification> notifications)
    {
        var db = Substitute.For<ICceDbContext>();
        db.UserNotifications.Returns(notifications.AsQueryable());
        return db;
    }
}
