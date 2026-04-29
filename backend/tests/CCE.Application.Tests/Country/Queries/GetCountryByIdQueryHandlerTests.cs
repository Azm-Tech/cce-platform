using CCE.Application.Common.Interfaces;
using CCE.Application.Country.Queries.GetCountryById;

namespace CCE.Application.Tests.Country.Queries;

public class GetCountryByIdQueryHandlerTests
{
    [Fact]
    public async Task Returns_null_when_country_not_found()
    {
        var db = BuildDb(System.Array.Empty<CCE.Domain.Country.Country>());
        var sut = new GetCountryByIdQueryHandler(db);

        var result = await sut.Handle(new GetCountryByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_dto_with_all_fields_when_found()
    {
        var country = CCE.Domain.Country.Country.Register("USA", "US", "أمريكا", "United States", "أمريكا الشمالية", "North America", "https://example/flag.png");

        var db = BuildDb(new[] { country });
        var sut = new GetCountryByIdQueryHandler(db);

        var result = await sut.Handle(new GetCountryByIdQuery(country.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(country.Id);
        result.IsoAlpha3.Should().Be("USA");
        result.IsoAlpha2.Should().Be("US");
        result.NameAr.Should().Be("أمريكا");
        result.NameEn.Should().Be("United States");
        result.RegionAr.Should().Be("أمريكا الشمالية");
        result.RegionEn.Should().Be("North America");
        result.FlagUrl.Should().Be("https://example/flag.png");
        result.IsActive.Should().BeTrue();
    }

    private static ICceDbContext BuildDb(IEnumerable<CCE.Domain.Country.Country> countries)
    {
        var db = Substitute.For<ICceDbContext>();
        db.Countries.Returns(countries.AsQueryable());
        return db;
    }
}
