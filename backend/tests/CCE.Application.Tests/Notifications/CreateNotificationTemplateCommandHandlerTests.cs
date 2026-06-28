using CCE.Application.Common.Interfaces;
using CCE.Application.Notifications;
using CCE.Application.Notifications.Commands.CreateNotificationTemplate;
using CCE.Domain.Notifications;

namespace CCE.Application.Tests.Notifications;

public class CreateNotificationTemplateCommandHandlerTests
{
    [Fact]
    public async Task Persists_template_and_returns_id_when_inputs_valid()
    {
        var repo = Substitute.For<INotificationTemplateRepository>();
        var db = Substitute.For<ICceDbContext>();
        var sut = new CreateNotificationTemplateCommandHandler(repo, db, NotificationTestMessages.Create());

        var cmd = new CreateNotificationTemplateCommand(
            "WELCOME_EMAIL",
            "مرحبا", "Welcome",
            "جسم عربي", "English body",
            NotificationChannel.Email,
            "{}");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBe(System.Guid.Empty);
        await repo.Received(1).AddAsync(Arg.Any<NotificationTemplate>(), Arg.Any<CancellationToken>());
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
