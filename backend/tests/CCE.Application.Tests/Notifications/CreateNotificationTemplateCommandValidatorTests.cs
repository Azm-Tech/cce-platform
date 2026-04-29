using CCE.Application.Notifications.Commands.CreateNotificationTemplate;
using CCE.Domain.Notifications;

namespace CCE.Application.Tests.Notifications;

public class CreateNotificationTemplateCommandValidatorTests
{
    private static CreateNotificationTemplateCommand ValidCmd() => new(
        "WELCOME_EMAIL",
        "مرحبا", "Welcome",
        "جسم عربي", "English body",
        NotificationChannel.Email,
        "{}");

    [Fact]
    public void Valid_command_passes()
    {
        var sut = new CreateNotificationTemplateCommandValidator();

        var result = sut.Validate(ValidCmd());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("welcome")]          // lowercase — fails regex
    [InlineData("1INVALID")]         // starts with digit
    [InlineData("INVALID CODE")]     // space not allowed
    public void Invalid_code_format_is_rejected(string code)
    {
        var sut = new CreateNotificationTemplateCommandValidator();
        var cmd = ValidCmd() with { Code = code };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateNotificationTemplateCommand.Code));
    }

    [Theory]
    [InlineData("", "Welcome", "جسم عربي", "English body")]
    [InlineData("مرحبا", "", "جسم عربي", "English body")]
    [InlineData("مرحبا", "Welcome", "", "English body")]
    [InlineData("مرحبا", "Welcome", "جسم عربي", "")]
    public void Empty_subject_or_body_fields_are_rejected(
        string subjectAr, string subjectEn, string bodyAr, string bodyEn)
    {
        var sut = new CreateNotificationTemplateCommandValidator();
        var cmd = ValidCmd() with
        {
            SubjectAr = subjectAr,
            SubjectEn = subjectEn,
            BodyAr = bodyAr,
            BodyEn = bodyEn
        };

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }
}
