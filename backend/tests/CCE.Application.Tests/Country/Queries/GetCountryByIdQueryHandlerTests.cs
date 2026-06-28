using CCE.Application.Common.Interfaces;
using CCE.Application.Country.Queries.GetCountryById;
using CCE.Application.Localization;
using CCE.Application.Messages;

namespace CCE.Application.Tests.Country.Queries;

public class GetCountryByIdQueryHandlerTests
{
    [Fact]
    public async Task Returns_not_found_when_country_not_found()
    {
        var db = BuildDb(System.Array.Empty<CCE.Domain.Country.Country>());
        var sut = new GetCountryByIdQueryHandler(db, BuildMessages());

        var result = await sut.Handle(new GetCountryByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task Returns_dto_with_all_fields_when_found()
    {
        var country = CCE.Domain.Country.Country.Register("USA", "US", "أمريكا", "United States", "أمريكا الشمالية", "North America", "https://example/flag.png");

        var db = BuildDb(new[] { country });
        var sut = new GetCountryByIdQueryHandler(db, BuildMessages());

        var result = await sut.Handle(new GetCountryByIdQuery(country.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(country.Id);
        result.Data.IsoAlpha3.Should().Be("USA");
        result.Data.IsoAlpha2.Should().Be("US");
        result.Data.NameAr.Should().Be("أمريكا");
        result.Data.NameEn.Should().Be("United States");
        result.Data.RegionAr.Should().Be("أمريكا الشمالية");
        result.Data.RegionEn.Should().Be("North America");
        result.Data.FlagUrl.Should().Be("https://example/flag.png");
        result.Data.IsActive.Should().BeTrue();
    }

    private static MessageFactory BuildMessages()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return new MessageFactory(localization, Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageFactory>.Instance);
    }

    private static ICceDbContext BuildDb(IEnumerable<CCE.Domain.Country.Country> countries)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Countries.Returns(countries.AsQueryable());
        return db;
    }
}
