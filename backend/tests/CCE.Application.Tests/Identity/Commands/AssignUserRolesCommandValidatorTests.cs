using CCE.Application.Identity.Commands.AssignUserRoles;

namespace CCE.Application.Tests.Identity.Commands;

public class AssignUserRolesCommandValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var sut = new AssignUserRolesCommandValidator();
        var cmd = new AssignUserRolesCommand(System.Guid.NewGuid(), new[] { "SuperAdmin", "ContentManager" });

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_roles_list_is_allowed()
    {
        var sut = new AssignUserRolesCommandValidator();
        var cmd = new AssignUserRolesCommand(System.Guid.NewGuid(), System.Array.Empty<string>());

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeTrue("empty list explicitly clears all role assignments");
    }

    [Fact]
    public void Empty_id_is_rejected()
    {
        var sut = new AssignUserRolesCommandValidator();
        var cmd = new AssignUserRolesCommand(System.Guid.Empty, new[] { "SuperAdmin" });

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AssignUserRolesCommand.Id));
    }

    [Fact]
    public void Unknown_role_is_rejected()
    {
        var sut = new AssignUserRolesCommandValidator();
        var cmd = new AssignUserRolesCommand(System.Guid.NewGuid(), new[] { "NotARole" });

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("NotARole"));
    }

    [Fact]
    public void Duplicate_role_is_rejected()
    {
        var sut = new AssignUserRolesCommandValidator();
        var cmd = new AssignUserRolesCommand(System.Guid.NewGuid(), new[] { "SuperAdmin", "SuperAdmin" });

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("duplicate", System.StringComparison.OrdinalIgnoreCase));
    }
}
