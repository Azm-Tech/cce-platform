using CCE.Application.Notifications;
using CCE.Application.Notifications.Commands.CreateNotificationTemplate;
using CCE.Domain.Notifications;

namespace CCE.Application.Tests.Notifications;

public class CreateNotificationTemplateCommandHandlerTests
{
    [Fact]
    public async Task Persists_template_and_returns_dto_when_inputs_valid()
    {
        var service = Substitute.For<INotificationTemplateService>();
        var sut = new CreateNotificationTemplateCommandHandler(service);

        var cmd = new CreateNotificationTemplateCommand(
            "WELCOME_EMAIL",
            "مرحبا", "Welcome",
            "جسم عربي", "English body",
            NotificationChannel.Email,
            "{}");

        var dto = await sut.Handle(cmd, CancellationToken.None);

        dto.Code.Should().Be("WELCOME_EMAIL");
        dto.SubjectEn.Should().Be("Welcome");
        dto.Channel.Should().Be(NotificationChannel.Email);
        dto.IsActive.Should().BeTrue();
        await service.Received(1).SaveAsync(Arg.Any<NotificationTemplate>(), Arg.Any<CancellationToken>());
    }
}
