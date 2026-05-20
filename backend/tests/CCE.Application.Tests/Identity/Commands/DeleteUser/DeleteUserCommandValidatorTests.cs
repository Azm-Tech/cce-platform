using CCE.Application.Identity.Commands.DeleteUser;

namespace CCE.Application.Tests.Identity.Commands.DeleteUser;

public class DeleteUserCommandValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var sut = new DeleteUserCommandValidator();
        var cmd = new DeleteUserCommand(System.Guid.NewGuid());

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_id_is_rejected()
    {
        var sut = new DeleteUserCommandValidator();
        var cmd = new DeleteUserCommand(System.Guid.Empty);

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DeleteUserCommand.UserId));
    }
}
