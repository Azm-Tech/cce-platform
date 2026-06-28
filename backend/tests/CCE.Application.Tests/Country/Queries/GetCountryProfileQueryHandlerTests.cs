using CCE.Application.Common.Interfaces;
using CCE.Application.Country;
using CCE.Application.Country.Queries.GetCountryProfile;
using CCE.Application.Localization;
using CCE.Application.Messages;
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
        var sut = new GetCountryProfileQueryHandler(service, BuildDb(), BuildMessages());

        var result = await sut.Handle(new GetCountryProfileQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
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
        var sut = new GetCountryProfileQueryHandler(service, BuildDb(), BuildMessages());

        var result = await sut.Handle(new GetCountryProfileQuery(countryId), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.CountryId.Should().Be(countryId);
        result.Data.DescriptionAr.Should().Be("ar-desc");
        result.Data.DescriptionEn.Should().Be("en-desc");
        result.Data.KeyInitiativesAr.Should().Be("ar-init");
        result.Data.KeyInitiativesEn.Should().Be("en-init");
        result.Data.ContactInfoAr.Should().BeNull();
        result.Data.ContactInfoEn.Should().BeNull();
        result.Data.LastUpdatedById.Should().Be(adminId);
    }

    private static MessageFactory BuildMessages()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return new MessageFactory(localization, Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageFactory>.Instance);
    }

    private static ICceDbContext BuildDb()
    {
        var db = Substitute.For<ICceDbContext>();
        db.Countries.Returns(System.Array.Empty<CCE.Domain.Country.Country>().AsQueryable());
        return db;
    }
}
