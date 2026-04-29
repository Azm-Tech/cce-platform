using CCE.Application.Notifications.Commands.UpdateNotificationTemplate;

namespace CCE.Application.Tests.Notifications;

public class UpdateNotificationTemplateCommandValidatorTests
{
    private static UpdateNotificationTemplateCommand ValidCmd() => new(
        System.Guid.NewGuid(),
        "مرحبا", "Welcome",
        "جسم عربي", "English body",
        IsActive: true);

    [Fact]
    public void Valid_command_passes()
    {
        var sut = new UpdateNotificationTemplateCommandValidator();

        var result = sut.Validate(ValidCmd());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Id_is_rejected()
    {
        var sut = new UpdateNotificationTemplateCommandValidator();
        var cmd = ValidCmd() with { Id = System.Guid.Empty };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateNotificationTemplateCommand.Id));
    }
}
