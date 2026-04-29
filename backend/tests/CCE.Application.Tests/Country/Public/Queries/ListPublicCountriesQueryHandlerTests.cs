using CCE.Application.Common.Interfaces;
using CCE.Application.CountryPublic.Queries.ListPublicCountries;

namespace CCE.Application.Tests.Country.Public.Queries;

public class ListPublicCountriesQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_list_when_no_countries_exist()
    {
        var db = BuildDb(System.Array.Empty<CCE.Domain.Country.Country>());
        var sut = new ListPublicCountriesQueryHandler(db);

        var result = await sut.Handle(new ListPublicCountriesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_only_active_countries()
    {
        var active = CCE.Domain.Country.Country.Register("USA", "US", "أمريكا", "United States", "أمريكا الشمالية", "North America", "https://example/flag.png");
        var inactive = CCE.Domain.Country.Country.Register("GBR", "GB", "بريطانيا", "United Kingdom", "أوروبا", "Europe", "https://example/flag-gb.png");
        inactive.Deactivate();

        var db = BuildDb(new[] { active, inactive });
        var sut = new ListPublicCountriesQueryHandler(db);

        var result = await sut.Handle(new ListPublicCountriesQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().NameEn.Should().Be("United States");
    }

    [Fact]
    public async Task Search_filter_returns_countries_matching_NameEn_substring()
    {
        var usa = CCE.Domain.Country.Country.Register("USA", "US", "أمريكا", "United States", "أمريكا الشمالية", "North America", "https://example/flag.png");
        var gbr = CCE.Domain.Country.Country.Register("GBR", "GB", "بريطانيا", "United Kingdom", "أوروبا", "Europe", "https://example/flag-gb.png");

        var db = BuildDb(new[] { usa, gbr });
        var sut = new ListPublicCountriesQueryHandler(db);

        var result = await sut.Handle(new ListPublicCountriesQuery(Search: "United States"), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().NameEn.Should().Be("United States");
    }

    private static ICceDbContext BuildDb(IEnumerable<CCE.Domain.Country.Country> countries)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Countries.Returns(countries.AsQueryable());
        return db;
    }
}
