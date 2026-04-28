using CCE.Application.Content.Commands.RejectCountryResourceRequest;

namespace CCE.Application.Tests.Content.Commands;

public class RejectCountryResourceRequestCommandValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var sut = new RejectCountryResourceRequestCommandValidator();
        var cmd = new RejectCountryResourceRequestCommand(System.Guid.NewGuid(), "غير مؤهل", "Insufficient evidence.");

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_id_is_rejected()
    {
        var sut = new RejectCountryResourceRequestCommandValidator();
        var cmd = new RejectCountryResourceRequestCommand(System.Guid.Empty, "غير مؤهل", "Insufficient evidence.");

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RejectCountryResourceRequestCommand.Id));
    }

    [Fact]
    public void Empty_notes_are_rejected()
    {
        var sut = new RejectCountryResourceRequestCommandValidator();
        var cmd = new RejectCountryResourceRequestCommand(System.Guid.NewGuid(), "", "");

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
