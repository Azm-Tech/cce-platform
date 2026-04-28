using CCE.Application.Identity.Commands.ApproveExpertRequest;

namespace CCE.Application.Tests.Identity.Commands;

public class ApproveExpertRequestCommandValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var sut = new ApproveExpertRequestCommandValidator();
        var cmd = new ApproveExpertRequestCommand(System.Guid.NewGuid(), "أستاذ", "Professor");

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_id_is_rejected()
    {
        var sut = new ApproveExpertRequestCommandValidator();
        var cmd = new ApproveExpertRequestCommand(System.Guid.Empty, "أستاذ", "Professor");

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_titles_are_rejected()
    {
        var sut = new ApproveExpertRequestCommandValidator();
        var cmd = new ApproveExpertRequestCommand(System.Guid.NewGuid(), "", "");

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
