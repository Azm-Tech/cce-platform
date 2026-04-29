using CCE.Application.Common.Interfaces;
using CCE.Application.CountryPublic.Queries.GetPublicCountryProfile;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Country.Public.Queries;

public class GetPublicCountryProfileQueryHandlerTests
{
    private static readonly FakeSystemClock FakeSystemClock = new();

    [Fact]
    public async Task Returns_null_when_country_not_found()
    {
        var db = BuildDb(
            System.Array.Empty<CCE.Domain.Country.Country>(),
            System.Array.Empty<CCE.Domain.Country.CountryProfile>());
        var sut = new GetPublicCountryProfileQueryHandler(db);

        var result = await sut.Handle(new GetPublicCountryProfileQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_null_when_profile_missing()
    {
        var country = CCE.Domain.Country.Country.Register("USA", "US", "أمريكا", "United States", "أمريكا الشمالية", "North America", "https://example/flag.png");

        var db = BuildDb(
            new[] { country },
            System.Array.Empty<CCE.Domain.Country.CountryProfile>());
        var sut = new GetPublicCountryProfileQueryHandler(db);

        var result = await sut.Handle(new GetPublicCountryProfileQuery(country.Id), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_dto_when_country_and_profile_exist()
    {
        var adminId = System.Guid.NewGuid();
        var country = CCE.Domain.Country.Country.Register("USA", "US", "أمريكا", "United States", "أمريكا الشمالية", "North America", "https://example/flag.png");
        var profile = CCE.Domain.Country.CountryProfile.Create(
            country.Id, "ar-desc", "en-desc", "ar-init", "en-init", null, null, adminId, FakeSystemClock);

        var db = BuildDb(new[] { country }, new[] { profile });
        var sut = new GetPublicCountryProfileQueryHandler(db);

        var result = await sut.Handle(new GetPublicCountryProfileQuery(country.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.CountryId.Should().Be(country.Id);
        result.DescriptionAr.Should().Be("ar-desc");
        result.DescriptionEn.Should().Be("en-desc");
        result.KeyInitiativesAr.Should().Be("ar-init");
        result.KeyInitiativesEn.Should().Be("en-init");
        result.ContactInfoAr.Should().BeNull();
        result.ContactInfoEn.Should().BeNull();
    }

    private static ICceDbContext BuildDb(
        IEnumerable<CCE.Domain.Country.Country> countries,
        IEnumerable<CCE.Domain.Country.CountryProfile> profiles)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Countries.Returns(countries.AsQueryable());
        db.CountryProfiles.Returns(profiles.AsQueryable());
        return db;
    }
}
