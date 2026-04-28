using CCE.Application.Identity.Commands.RejectExpertRequest;

namespace CCE.Application.Tests.Identity.Commands;

public class RejectExpertRequestCommandValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var sut = new RejectExpertRequestCommandValidator();
        var cmd = new RejectExpertRequestCommand(System.Guid.NewGuid(), "غير مؤهل", "Insufficient evidence.");

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_id_is_rejected()
    {
        var sut = new RejectExpertRequestCommandValidator();
        var cmd = new RejectExpertRequestCommand(System.Guid.Empty, "غير مؤهل", "Insufficient evidence.");

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Empty_reasons_are_rejected()
    {
        var sut = new RejectExpertRequestCommandValidator();
        var cmd = new RejectExpertRequestCommand(System.Guid.NewGuid(), "", "");

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
