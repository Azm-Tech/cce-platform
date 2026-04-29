using CCE.Application.Identity.Public.Commands.UpdateMyProfile;
using CCE.Domain.Identity;

namespace CCE.Application.Tests.Identity.Public.Commands;

public class UpdateMyProfileCommandValidatorTests
{
    private static UpdateMyProfileCommand ValidCommand() => new(
        System.Guid.NewGuid(), "ar", KnowledgeLevel.Beginner,
        System.Array.Empty<string>(), null, null);

    [Fact]
    public void Valid_command_passes()
    {
        var sut = new UpdateMyProfileCommandValidator();
        var result = sut.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_locale_is_rejected()
    {
        var sut = new UpdateMyProfileCommandValidator();
        var cmd = ValidCommand() with { LocalePreference = "fr" };
        var result = sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateMyProfileCommand.LocalePreference));
    }

    [Fact]
    public void Non_https_avatar_url_is_rejected()
    {
        var sut = new UpdateMyProfileCommandValidator();
        var cmd = ValidCommand() with { AvatarUrl = "http://example.com/avatar.png" };
        var result = sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateMyProfileCommand.AvatarUrl));
    }
}
