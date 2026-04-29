using CCE.Application.Common.Interfaces;
using CCE.Application.InteractiveCity.Public.Queries.ListCityTechnologies;
using CCE.Domain.InteractiveCity;

namespace CCE.Application.Tests.InteractiveCity;

public class ListCityTechnologiesQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_list_when_no_active_technologies()
    {
        var db = BuildDb(System.Array.Empty<CityTechnology>());
        var sut = new ListCityTechnologiesQueryHandler(db);

        var result = await sut.Handle(new ListCityTechnologiesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_active_technologies_sorted_by_category_then_name()
    {
        var tech1 = CityTechnology.Create("ب", "B Solar", "وصف", "desc", "طاقة", "Renewable", 100m, 50m);
        var tech2 = CityTechnology.Create("أ", "A Wind", "وصف", "desc", "طاقة", "Renewable", 200m, 80m);
        var tech3 = CityTechnology.Create("ج", "C Grid", "وصف", "desc", "شبكة", "Grid", 150m, 60m);

        var db = BuildDb(new[] { tech1, tech2, tech3 });
        var sut = new ListCityTechnologiesQueryHandler(db);

        var result = await sut.Handle(new ListCityTechnologiesQuery(), CancellationToken.None);

        result.Should().HaveCount(3);
        // Sorted by CategoryEn then NameEn: Grid/C Grid, Renewable/A Wind, Renewable/B Solar
        result[0].NameEn.Should().Be("C Grid");
        result[1].NameEn.Should().Be("A Wind");
        result[2].NameEn.Should().Be("B Solar");
    }

    private static ICceDbContext BuildDb(IEnumerable<CityTechnology> techs)
    {
        var db = Substitute.For<ICceDbContext>();
        db.CityTechnologies.Returns(techs.AsQueryable());
        return db;
    }
}
