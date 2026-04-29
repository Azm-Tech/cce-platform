using CCE.Application.Identity.Public.Commands.SubmitExpertRequest;

namespace CCE.Application.Tests.Identity.Public.Commands;

public class SubmitExpertRequestCommandValidatorTests
{
    private static SubmitExpertRequestCommand ValidCommand() => new(
        System.Guid.NewGuid(),
        "سيرة ذاتية",
        "English bio",
        new[] { "Hydrogen" });

    [Fact]
    public void Valid_command_passes()
    {
        var sut = new SubmitExpertRequestCommandValidator();
        var result = sut.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_bios_are_rejected()
    {
        var sut = new SubmitExpertRequestCommandValidator();
        var cmd = ValidCommand() with { RequestedBioAr = "", RequestedBioEn = "" };
        var result = sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Empty_requester_id_is_rejected()
    {
        var sut = new SubmitExpertRequestCommandValidator();
        var cmd = ValidCommand() with { RequesterId = System.Guid.Empty };
        var result = sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SubmitExpertRequestCommand.RequesterId));
    }
}
