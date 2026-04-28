using CCE.Application.Content.Commands.ApproveCountryResourceRequest;

namespace CCE.Application.Tests.Content.Commands;

public class ApproveCountryResourceRequestCommandValidatorTests
{
    [Fact]
    public void Valid_command_passes()
    {
        var sut = new ApproveCountryResourceRequestCommandValidator();
        var cmd = new ApproveCountryResourceRequestCommand(System.Guid.NewGuid(), null, null);

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_id_is_rejected()
    {
        var sut = new ApproveCountryResourceRequestCommandValidator();
        var cmd = new ApproveCountryResourceRequestCommand(System.Guid.Empty, null, null);

        var result = sut.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ApproveCountryResourceRequestCommand.Id));
    }
}
