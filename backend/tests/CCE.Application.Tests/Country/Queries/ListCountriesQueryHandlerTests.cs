using CCE.Application.Common.Interfaces;
using CCE.Application.Country.Queries.ListCountries;
using CCE.Application.Localization;
using CCE.Application.Messages;
using CCE.Domain.Country;
using NSubstitute;

namespace CCE.Application.Tests.Country.Queries;

public class ListCountriesQueryHandlerTests
{
    private static MessageFactory BuildMessages()
    {
        var localization = Substitute.For<ILocalizationService>();
        localization.GetString(Arg.Any<string>(), Arg.Any<string?>()).Returns(call => call.ArgAt<string>(0));
        return new MessageFactory(localization, Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageFactory>.Instance);
    }

    [Fact]
    public async Task Returns_empty_paged_result_when_no_countries_exist()
    {
        var db = BuildDb(
            System.Array.Empty<CCE.Domain.Country.Country>(),
            System.Array.Empty<CountryKapsarcSnapshot>());
        var sut = new ListCountriesQueryHandler(db, BuildMessages());

        var result = await sut.Handle(new ListCountriesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.Total.Should().Be(0);
        result.Data.Page.Should().Be(1);
        result.Data.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task IsActive_filter_returns_only_active_countries()
    {
        var active = CCE.Domain.Country.Country.Register("USA", "US", "أمريكا", "United States", "أمريكا الشمالية", "North America", "https://example/flag.png");
        var inactive = CCE.Domain.Country.Country.Register("GBR", "GB", "بريطانيا", "United Kingdom", "أوروبا", "Europe", "https://example/flag-gb.png");
        inactive.Deactivate();

        var db = BuildDb(new[] { active, inactive }, System.Array.Empty<CountryKapsarcSnapshot>());
        var sut = new ListCountriesQueryHandler(db, BuildMessages());

        var result = await sut.Handle(new ListCountriesQuery(IsActive: true), CancellationToken.None);

        result.Data!.Total.Should().Be(1);
        result.Data.Items.Single().NameEn.Should().Be("United States");
    }

    [Fact]
    public async Task Search_filter_returns_countries_matching_IsoAlpha3()
    {
        var usa = CCE.Domain.Country.Country.Register("USA", "US", "أمريكا", "United States", "أمريكا الشمالية", "North America", "https://example/flag.png");
        var gbr = CCE.Domain.Country.Country.Register("GBR", "GB", "بريطانيا", "United Kingdom", "أوروبا", "Europe", "https://example/flag-gb.png");

        var db = BuildDb(new[] { usa, gbr }, System.Array.Empty<CountryKapsarcSnapshot>());
        var sut = new ListCountriesQueryHandler(db, BuildMessages());

        var result = await sut.Handle(new ListCountriesQuery(Search: "USA"), CancellationToken.None);

        result.Data!.Total.Should().Be(1);
        result.Data.Items.Single().IsoAlpha3.Should().Be("USA");
    }

    private static ICceDbContext BuildDb(
        IEnumerable<CCE.Domain.Country.Country> countries,
        IEnumerable<CountryKapsarcSnapshot> snapshots)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Countries.Returns(countries.AsQueryable());
        db.CountryKapsarcSnapshots.Returns(snapshots.AsQueryable());
        return db;
    }
}