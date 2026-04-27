using CCE.Domain.Common;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Country;

public class CountryTests
{
    private static CCE.Domain.Country.Country NewCountry() => CCE.Domain.Country.Country.Register(
        "SAU", "SA", "السعودية", "Saudi Arabia", "آسيا", "Asia",
        "https://flags.example/sa.svg");

    [Fact]
    public void Register_creates_active_country()
    {
        var c = NewCountry();
        c.IsoAlpha3.Should().Be("SAU");
        c.IsoAlpha2.Should().Be("SA");
        c.IsActive.Should().BeTrue();
        c.IsDeleted.Should().BeFalse();
        c.LatestKapsarcSnapshotId.Should().BeNull();
    }

    [Theory]
    [InlineData("sau")]
    [InlineData("SA")]
    [InlineData("SAUD")]
    public void Register_with_invalid_alpha3_throws(string bad)
    {
        var act = () => CCE.Domain.Country.Country.Register(bad, "SA", "ا", "x", "ا", "x", "https://x");
        act.Should().Throw<DomainException>().WithMessage("*IsoAlpha3*");
    }

    [Theory]
    [InlineData("sa")]
    [InlineData("SAU")]
    [InlineData("S")]
    public void Register_with_invalid_alpha2_throws(string bad)
    {
        var act = () => CCE.Domain.Country.Country.Register("SAU", bad, "ا", "x", "ا", "x", "https://x");
        act.Should().Throw<DomainException>().WithMessage("*IsoAlpha2*");
    }

    [Fact]
    public void FlagUrl_must_be_https()
    {
        var act = () => CCE.Domain.Country.Country.Register("SAU", "SA", "ا", "x", "ا", "x", "http://insecure");
        act.Should().Throw<DomainException>().WithMessage("*FlagUrl*");
    }

    [Fact]
    public void UpdateLatestKapsarcSnapshot_sets_pointer()
    {
        var c = NewCountry();
        var snap = System.Guid.NewGuid();
        c.UpdateLatestKapsarcSnapshot(snap);
        c.LatestKapsarcSnapshotId.Should().Be(snap);
    }

    [Fact]
    public void Deactivate_then_Activate_toggles()
    {
        var c = NewCountry();
        c.Deactivate();
        c.IsActive.Should().BeFalse();
        c.Activate();
        c.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_marks_deleted()
    {
        var c = NewCountry();
        c.SoftDelete(System.Guid.NewGuid(), new FakeSystemClock());
        c.IsDeleted.Should().BeTrue();
    }
}
