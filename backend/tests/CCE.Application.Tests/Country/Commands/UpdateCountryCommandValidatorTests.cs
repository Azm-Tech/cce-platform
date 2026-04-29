using CCE.Application.Country.Commands.UpdateCountry;

namespace CCE.Application.Tests.Country.Commands;

public class UpdateCountryCommandValidatorTests
{
    private readonly UpdateCountryCommandValidator _sut = new();

    [Fact]
    public void Valid_command_passes_validation()
    {
        var cmd = new UpdateCountryCommand(System.Guid.NewGuid(), "أمريكا", "United States", "أمريكا الشمالية", "North America", true);
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_NameEn_fails_validation()
    {
        var cmd = new UpdateCountryCommand(System.Guid.NewGuid(), "أمريكا", "", "أمريكا الشمالية", "North America", true);
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.NameEn));
    }
}
