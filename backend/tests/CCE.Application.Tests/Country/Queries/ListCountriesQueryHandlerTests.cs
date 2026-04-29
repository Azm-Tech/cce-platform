using CCE.Application.Common.Interfaces;
using CCE.Application.Country.Queries.ListCountries;
using CCE.Domain.Country;

namespace CCE.Application.Tests.Country.Queries;

public class ListCountriesQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_paged_result_when_no_countries_exist()
    {
        var db = BuildDb(System.Array.Empty<CCE.Domain.Country.Country>());
        var sut = new ListCountriesQueryHandler(db);

        var result = await sut.Handle(new ListCountriesQuery(Page: 1, PageSize: 20), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task IsActive_filter_returns_only_active_countries()
    {
        var active = CCE.Domain.Country.Country.Register("USA", "US", "أمريكا", "United States", "أمريكا الشمالية", "North America", "https://example/flag.png");
        var inactive = CCE.Domain.Country.Country.Register("GBR", "GB", "بريطانيا", "United Kingdom", "أوروبا", "Europe", "https://example/flag-gb.png");
        inactive.Deactivate();

        var db = BuildDb(new[] { active, inactive });
        var sut = new ListCountriesQueryHandler(db);

        var result = await sut.Handle(new ListCountriesQuery(IsActive: true), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().NameEn.Should().Be("United States");
    }

    [Fact]
    public async Task Search_filter_returns_countries_matching_IsoAlpha3()
    {
        var usa = CCE.Domain.Country.Country.Register("USA", "US", "أمريكا", "United States", "أمريكا الشمالية", "North America", "https://example/flag.png");
        var gbr = CCE.Domain.Country.Country.Register("GBR", "GB", "بريطانيا", "United Kingdom", "أوروبا", "Europe", "https://example/flag-gb.png");

        var db = BuildDb(new[] { usa, gbr });
        var sut = new ListCountriesQueryHandler(db);

        var result = await sut.Handle(new ListCountriesQuery(Search: "USA"), CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Single().IsoAlpha3.Should().Be("USA");
    }

    private static ICceDbContext BuildDb(IEnumerable<CCE.Domain.Country.Country> countries)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Countries.Returns(countries.AsQueryable());
        return db;
    }
}
