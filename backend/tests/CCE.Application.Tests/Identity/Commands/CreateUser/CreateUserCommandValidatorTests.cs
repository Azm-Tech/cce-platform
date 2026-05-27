using CCE.Application.Identity.Commands.CreateUser;

namespace CCE.Application.Tests.Identity.Commands.CreateUser;

public class CreateUserCommandValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var sut = new CreateUserCommandValidator();
        var cmd = new CreateUserCommand("Ali", "Ahmed", "a@b.c", "1234567890", null, null, "cce-admin");

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Missing_first_name_is_rejected()
    {
        var sut = new CreateUserCommandValidator();
        var cmd = new CreateUserCommand("", "B", "a@b.c", "123", null, null, "cce-admin");

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.FirstName));
    }

    [Fact]
    public void First_name_with_numbers_is_rejected()
    {
        var sut = new CreateUserCommandValidator();
        var cmd = new CreateUserCommand("Ali123", "B", "a@b.c", "123", null, null, "cce-admin");

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.FirstName));
    }

    [Fact]
    public void Invalid_email_is_rejected()
    {
        var sut = new CreateUserCommandValidator();
        var cmd = new CreateUserCommand("Ali", "Ahmed", "not-an-email", "123", null, null, "cce-admin");

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Email));
    }

    [Fact]
    public void Unknown_role_is_rejected()
    {
        var sut = new CreateUserCommandValidator();
        var cmd = new CreateUserCommand("Ali", "Ahmed", "a@b.c", "123", null, null, "cce-user");

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Role));
    }

    [Fact]
    public void Empty_role_is_rejected()
    {
        var sut = new CreateUserCommandValidator();
        var cmd = new CreateUserCommand("Ali", "Ahmed", "a@b.c", "123", null, null, "");

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateUserCommand.Role));
    }
}
