using CCE.Application.Notifications;
using CCE.Application.Notifications.Commands.UpdateNotificationTemplate;
using CCE.Domain.Notifications;

namespace CCE.Application.Tests.Notifications;

public class UpdateNotificationTemplateCommandHandlerTests
{
    [Fact]
    public async Task Returns_null_when_template_not_found()
    {
        var service = Substitute.For<INotificationTemplateService>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns((NotificationTemplate?)null);
        var sut = new UpdateNotificationTemplateCommandHandler(service);

        var result = await sut.Handle(BuildCommand(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Updates_content_and_active_state_and_returns_dto()
    {
        var template = NotificationTemplate.Define(
            "OLD_CODE",
            "قديم", "Old Subject",
            "جسم قديم", "Old Body",
            NotificationChannel.Email,
            "{}");

        var service = Substitute.For<INotificationTemplateService>();
        service.FindAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);

        var sut = new UpdateNotificationTemplateCommandHandler(service);

        var cmd = new UpdateNotificationTemplateCommand(
            template.Id,
            "جديد", "New Subject",
            "جسم جديد", "New Body",
            IsActive: false);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result!.SubjectEn.Should().Be("New Subject");
        result.BodyEn.Should().Be("New Body");
        result.IsActive.Should().BeFalse();
        await service.Received(1).UpdateAsync(template, Arg.Any<CancellationToken>());
    }

    private static UpdateNotificationTemplateCommand BuildCommand(System.Guid id) =>
        new(id, "عنوان", "Subject", "جسم", "Body", true);
}
