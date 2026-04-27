using CCE.Domain.Common;
using CCE.Domain.Country;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Country;

public class CountryProfileTests
{
    [Fact]
    public void Create_builds_profile()
    {
        var clock = new FakeSystemClock();
        var p = CountryProfile.Create(
            System.Guid.NewGuid(),
            "وصف", "Description",
            "مبادرات", "Initiatives",
            null, null,
            System.Guid.NewGuid(), clock);

        p.DescriptionAr.Should().Be("وصف");
        p.LastUpdatedOn.Should().Be(clock.UtcNow);
        p.RowVersion.Should().NotBeNull();
    }

    [Fact]
    public void Create_with_empty_countryId_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => CountryProfile.Create(
            System.Guid.Empty, "ا", "x", "ا", "x", null, null, System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>().WithMessage("*CountryId*");
    }

    [Fact]
    public void Update_advances_LastUpdatedOn()
    {
        var clock = new FakeSystemClock();
        var p = CountryProfile.Create(
            System.Guid.NewGuid(), "ا", "x", "ا", "x", null, null, System.Guid.NewGuid(), clock);
        clock.Advance(System.TimeSpan.FromHours(1));
        var updater = System.Guid.NewGuid();

        p.Update("ج", "new", "ج", "new", "info", "info-en", updater, clock);

        p.DescriptionAr.Should().Be("ج");
        p.LastUpdatedOn.Should().Be(clock.UtcNow);
        p.LastUpdatedById.Should().Be(updater);
        p.ContactInfoAr.Should().Be("info");
    }

    [Fact]
    public void Update_with_empty_required_throws()
    {
        var clock = new FakeSystemClock();
        var p = CountryProfile.Create(
            System.Guid.NewGuid(), "ا", "x", "ا", "x", null, null, System.Guid.NewGuid(), clock);

        var act = () => p.Update("", "x", "ا", "x", null, null, System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>();
    }
}
