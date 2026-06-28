using CCE.Application.Common.Interfaces;
using CCE.Application.Notifications;
using CCE.Application.Notifications.Commands.UpdateNotificationTemplate;
using CCE.Domain.Notifications;

namespace CCE.Application.Tests.Notifications;

public class UpdateNotificationTemplateCommandHandlerTests
{
    [Fact]
    public async Task Returns_not_found_response_when_template_not_found()
    {
        var repo = Substitute.For<INotificationTemplateRepository>();
        repo.GetAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns((NotificationTemplate?)null);
        var db = Substitute.For<ICceDbContext>();
        var sut = new UpdateNotificationTemplateCommandHandler(repo, db, NotificationTestMessages.Create());

        var result = await sut.Handle(BuildCommand(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Updates_content_and_active_state_and_returns_id()
    {
        var template = NotificationTemplate.Define(
            "OLD_CODE",
            "قديم", "Old Subject",
            "جسم قديم", "Old Body",
            NotificationChannel.Email,
            "{}");

        var repo = Substitute.For<INotificationTemplateRepository>();
        repo.GetAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);

        var db = Substitute.For<ICceDbContext>();
        var sut = new UpdateNotificationTemplateCommandHandler(repo, db, NotificationTestMessages.Create());

        var cmd = new UpdateNotificationTemplateCommand(
            template.Id,
            "جديد", "New Subject",
            "جسم جديد", "New Body",
            IsActive: false);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(template.Id);
        template.SubjectEn.Should().Be("New Subject");
        template.BodyEn.Should().Be("New Body");
        template.IsActive.Should().BeFalse();
        await db.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static UpdateNotificationTemplateCommand BuildCommand(System.Guid id) =>
        new(id, "عنوان", "Subject", "جسم", "Body", true);
}
