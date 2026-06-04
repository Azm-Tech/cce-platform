using CCE.Application.Common.Interfaces;
using CCE.Application.CountryPublic.Queries.GetPublicCountryProfile;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Content;
using CCE.Domain.Country;
using CCE.TestInfrastructure.Time;
using NSubstitute;

namespace CCE.Application.Tests.Country.Public.Queries;

public class GetPublicCountryProfileQueryHandlerTests
{
    private static readonly FakeSystemClock FakeSystemClock = new();

    private static MessageFactory BuildMessages()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return new MessageFactory(localization, Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageFactory>.Instance);
    }

    [Fact]
    public async Task Returns_not_found_when_country_not_found()
    {
        var db = BuildDb(
            System.Array.Empty<CCE.Domain.Country.Country>(),
            System.Array.Empty<CountryProfile>(),
            System.Array.Empty<CountryKapsarcSnapshot>(),
            System.Array.Empty<AssetFile>());
        var sut = new GetPublicCountryProfileQueryHandler(db, BuildMessages());

        var result = await sut.Handle(new GetPublicCountryProfileQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task Returns_dto_with_null_profile_fields_when_profile_missing()
    {
        var country = CCE.Domain.Country.Country.Register("USA", "US", "أمريكا", "United States", "أمريكا الشمالية", "North America", "https://example/flag.png");

        var db = BuildDb(
            new[] { country },
            System.Array.Empty<CountryProfile>(),
            System.Array.Empty<CountryKapsarcSnapshot>(),
            System.Array.Empty<AssetFile>());
        var sut = new GetPublicCountryProfileQueryHandler(db, BuildMessages());

        var result = await sut.Handle(new GetPublicCountryProfileQuery(country.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.CountryId.Should().Be(country.Id);
        result.Data.DescriptionAr.Should().BeNull();
        result.Data.DescriptionEn.Should().BeNull();
    }

    [Fact]
    public async Task Returns_dto_when_country_and_profile_exist()
    {
        var adminId = System.Guid.NewGuid();
        var country = CCE.Domain.Country.Country.Register("USA", "US", "أمريكا", "United States", "أمريكا الشمالية", "North America", "https://example/flag.png");
        var profile = CountryProfile.Create(
            country.Id, "ar-desc", "en-desc", "ar-init", "en-init", null, null, adminId, FakeSystemClock);

        var db = BuildDb(new[] { country }, new[] { profile }, System.Array.Empty<CountryKapsarcSnapshot>(), System.Array.Empty<AssetFile>());
        var sut = new GetPublicCountryProfileQueryHandler(db, BuildMessages());

        var result = await sut.Handle(new GetPublicCountryProfileQuery(country.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.CountryId.Should().Be(country.Id);
        result.Data.DescriptionAr.Should().Be("ar-desc");
        result.Data.DescriptionEn.Should().Be("en-desc");
        result.Data.KeyInitiativesAr.Should().Be("ar-init");
        result.Data.KeyInitiativesEn.Should().Be("en-init");
        result.Data.ContactInfoAr.Should().BeNull();
        result.Data.ContactInfoEn.Should().BeNull();
    }

    private static ICceDbContext BuildDb(
        IEnumerable<CCE.Domain.Country.Country> countries,
        IEnumerable<CountryProfile> profiles,
        IEnumerable<CountryKapsarcSnapshot> snapshots,
        IEnumerable<AssetFile> assetFiles)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Countries.Returns(countries.AsQueryable());
        db.CountryProfiles.Returns(profiles.AsQueryable());
        db.CountryKapsarcSnapshots.Returns(snapshots.AsQueryable());
        db.AssetFiles.Returns(assetFiles.AsQueryable());
        return db;
    }
}
