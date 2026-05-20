using CCE.Application.Identity.Commands.ChangeUserStatus;

namespace CCE.Application.Tests.Identity.Commands.ChangeUserStatus;

public class ChangeUserStatusCommandValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var sut = new ChangeUserStatusCommandValidator();
        var cmd = new ChangeUserStatusCommand(System.Guid.NewGuid(), true);

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_command_passes()
    {
        var sut = new ChangeUserStatusCommandValidator();
        var cmd = new ChangeUserStatusCommand(System.Guid.NewGuid(), false);

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_id_is_rejected()
    {
        var sut = new ChangeUserStatusCommandValidator();
        var cmd = new ChangeUserStatusCommand(System.Guid.Empty, true);

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangeUserStatusCommand.UserId));
    }
}
