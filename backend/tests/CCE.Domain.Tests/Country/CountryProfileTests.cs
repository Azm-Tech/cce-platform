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
        p.CreatedOn.Should().Be(clock.UtcNow);
        p.CreatedById.Should().NotBe(Guid.Empty);
        p.LastModifiedOn.Should().Be(clock.UtcNow);
        p.LastModifiedById.Should().Be(p.CreatedById);
        p.RowVersion.Should().NotBeNull();
        p.Population.Should().BeNull();
        p.AreaSqKm.Should().BeNull();
        p.GdpPerCapita.Should().BeNull();
        p.NationallyDeterminedContributionAssetId.Should().BeNull();
    }

    [Fact]
    public void Create_with_demographic_fields_stores_values()
    {
        var clock = new FakeSystemClock();
        var ndcId = System.Guid.NewGuid();
        var p = CountryProfile.Create(
            System.Guid.NewGuid(),
            "وصف", "Description",
            "مبادرات", "Initiatives",
            null, null,
            System.Guid.NewGuid(), clock,
            population: 35_000_000,
            areaSqKm: 2_149_690.0m,
            gdpPerCapita: 23_500.0m,
            nationallyDeterminedContributionAssetId: ndcId);

        p.Population.Should().Be(35_000_000);
        p.AreaSqKm.Should().Be(2_149_690.0m);
        p.GdpPerCapita.Should().Be(23_500.0m);
        p.NationallyDeterminedContributionAssetId.Should().Be(ndcId);
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
    public void Create_with_zero_population_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => CountryProfile.Create(
            System.Guid.NewGuid(), "ا", "x", "ا", "x", null, null, System.Guid.NewGuid(), clock,
            population: 0);
        act.Should().Throw<DomainException>().WithMessage("*Population*");
    }

    [Fact]
    public void Create_with_negative_area_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => CountryProfile.Create(
            System.Guid.NewGuid(), "ا", "x", "ا", "x", null, null, System.Guid.NewGuid(), clock,
            areaSqKm: -1.0m);
        act.Should().Throw<DomainException>().WithMessage("*AreaSqKm*");
    }

    [Fact]
    public void Create_with_zero_gdp_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => CountryProfile.Create(
            System.Guid.NewGuid(), "ا", "x", "ا", "x", null, null, System.Guid.NewGuid(), clock,
            gdpPerCapita: 0m);
        act.Should().Throw<DomainException>().WithMessage("*GdpPerCapita*");
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
        p.LastModifiedOn.Should().Be(clock.UtcNow);
        p.LastModifiedById.Should().Be(updater);
        p.ContactInfoAr.Should().Be("info");
    }

    [Fact]
    public void Update_with_demographic_fields_stores_values()
    {
        var clock = new FakeSystemClock();
        var p = CountryProfile.Create(
            System.Guid.NewGuid(), "ا", "x", "ا", "x", null, null, System.Guid.NewGuid(), clock);
        clock.Advance(System.TimeSpan.FromHours(1));

        p.Update("ج", "new", "ج", "new", null, null, System.Guid.NewGuid(), clock,
            population: 10_000_000, areaSqKm: 500_000m, gdpPerCapita: 15_000m);

        p.Population.Should().Be(10_000_000);
        p.AreaSqKm.Should().Be(500_000m);
        p.GdpPerCapita.Should().Be(15_000m);
    }

    [Fact]
    public void Update_clears_demographic_fields_when_null_passed()
    {
        var clock = new FakeSystemClock();
        var p = CountryProfile.Create(
            System.Guid.NewGuid(), "ا", "x", "ا", "x", null, null, System.Guid.NewGuid(), clock,
            population: 5_000_000);
        clock.Advance(System.TimeSpan.FromHours(1));

        p.Update("ا", "x", "ا", "x", null, null, System.Guid.NewGuid(), clock);

        p.Population.Should().BeNull();
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
