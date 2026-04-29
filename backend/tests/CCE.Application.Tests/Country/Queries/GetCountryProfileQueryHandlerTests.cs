using CCE.Application.Country;
using CCE.Application.Country.Queries.GetCountryProfile;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Country.Queries;

public class GetCountryProfileQueryHandlerTests
{
    private static readonly FakeSystemClock Clock = new();

    [Fact]
    public async Task Returns_null_when_no_profile_exists()
    {
        var service = Substitute.For<ICountryProfileService>();
        service.FindByCountryIdAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns((CCE.Domain.Country.CountryProfile?)null);
        var sut = new GetCountryProfileQueryHandler(service);

        var result = await sut.Handle(new GetCountryProfileQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_dto_with_all_fields_when_profile_exists()
    {
        var countryId = System.Guid.NewGuid();
        var adminId = System.Guid.NewGuid();
        var profile = CCE.Domain.Country.CountryProfile.Create(
            countryId, "ar-desc", "en-desc", "ar-init", "en-init", null, null, adminId, Clock);

        var service = Substitute.For<ICountryProfileService>();
        service.FindByCountryIdAsync(countryId, Arg.Any<CancellationToken>()).Returns(profile);
        var sut = new GetCountryProfileQueryHandler(service);

        var result = await sut.Handle(new GetCountryProfileQuery(countryId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.CountryId.Should().Be(countryId);
        result.DescriptionAr.Should().Be("ar-desc");
        result.DescriptionEn.Should().Be("en-desc");
        result.KeyInitiativesAr.Should().Be("ar-init");
        result.KeyInitiativesEn.Should().Be("en-init");
        result.ContactInfoAr.Should().BeNull();
        result.ContactInfoEn.Should().BeNull();
        result.LastUpdatedById.Should().Be(adminId);
    }
}
