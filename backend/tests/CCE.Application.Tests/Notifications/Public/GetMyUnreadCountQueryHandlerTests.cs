using CCE.Application.Common.Interfaces;
using CCE.Application.Notifications.Public.Queries.GetMyUnreadCount;
using CCE.Domain.Notifications;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Notifications.Public;

public class GetMyUnreadCountQueryHandlerTests
{
    [Fact]
    public async Task Returns_count_of_Sent_notifications_only()
    {
        var userId = System.Guid.NewGuid();
        var clock = new FakeSystemClock();

        var sent1 = UserNotification.Render(userId, System.Guid.NewGuid(),
            "موضوع", "Subject", "نص", "ar", NotificationChannel.Email);
        sent1.MarkSent(clock);

        var sent2 = UserNotification.Render(userId, System.Guid.NewGuid(),
            "موضوع", "Subject", "نص", "ar", NotificationChannel.Email);
        sent2.MarkSent(clock);

        var read = UserNotification.Render(userId, System.Guid.NewGuid(),
            "موضوع", "Subject", "نص", "ar", NotificationChannel.Email);
        read.MarkSent(clock);
        read.MarkRead(clock);

        var pending = UserNotification.Render(userId, System.Guid.NewGuid(),
            "موضوع", "Subject", "نص", "ar", NotificationChannel.Email);

        var db = Substitute.For<ICceDbContext>();
        db.UserNotifications.Returns(new[] { sent1, sent2, read, pending }.AsQueryable());
        var sut = new GetMyUnreadCountQueryHandler(db);

        var count = await sut.Handle(new GetMyUnreadCountQuery(userId), CancellationToken.None);

        count.Should().Be(2);
    }
}
