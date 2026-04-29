using CCE.Application.Country.Commands.UpsertCountryProfile;

namespace CCE.Application.Tests.Country.Commands;

public class UpsertCountryProfileCommandValidatorTests
{
    private readonly UpsertCountryProfileCommandValidator _sut = new();

    [Fact]
    public void Valid_command_passes_validation()
    {
        var cmd = BuildCommand(System.Guid.NewGuid(), "ar-desc", "en-desc", "ar-init", "en-init");
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_DescriptionEn_fails_validation()
    {
        var cmd = BuildCommand(System.Guid.NewGuid(), "ar-desc", "", "ar-init", "en-init");
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.DescriptionEn));
    }

    [Fact]
    public void Empty_CountryId_fails_validation()
    {
        var cmd = BuildCommand(System.Guid.Empty, "ar-desc", "en-desc", "ar-init", "en-init");
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.CountryId));
    }

    private static UpsertCountryProfileCommand BuildCommand(
        System.Guid countryId,
        string descAr,
        string descEn,
        string keyInitAr,
        string keyInitEn) =>
        new(countryId, descAr, descEn, keyInitAr, keyInitEn, null, null, System.Array.Empty<byte>());
}
