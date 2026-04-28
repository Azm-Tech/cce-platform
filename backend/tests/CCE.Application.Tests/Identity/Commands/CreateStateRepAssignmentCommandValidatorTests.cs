using CCE.Application.Identity.Commands.CreateStateRepAssignment;

namespace CCE.Application.Tests.Identity.Commands;

public class CreateStateRepAssignmentCommandValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var sut = new CreateStateRepAssignmentCommandValidator();
        var cmd = new CreateStateRepAssignmentCommand(System.Guid.NewGuid(), System.Guid.NewGuid());

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_user_id_is_rejected()
    {
        var sut = new CreateStateRepAssignmentCommandValidator();
        var cmd = new CreateStateRepAssignmentCommand(System.Guid.Empty, System.Guid.NewGuid());

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStateRepAssignmentCommand.UserId));
    }

    [Fact]
    public void Empty_country_id_is_rejected()
    {
        var sut = new CreateStateRepAssignmentCommandValidator();
        var cmd = new CreateStateRepAssignmentCommand(System.Guid.NewGuid(), System.Guid.Empty);

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateStateRepAssignmentCommand.CountryId));
    }
}
