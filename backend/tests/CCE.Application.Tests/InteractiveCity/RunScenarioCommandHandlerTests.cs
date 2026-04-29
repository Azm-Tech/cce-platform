using CCE.Application.Common.Interfaces;
using CCE.Application.InteractiveCity.Public.Commands.RunScenario;
using CCE.Domain.InteractiveCity;

namespace CCE.Application.Tests.InteractiveCity;

public class RunScenarioCommandHandlerTests
{
    [Fact]
    public async Task Empty_technologyIds_array_returns_zero_totals()
    {
        var db = BuildDb(System.Array.Empty<CityTechnology>());
        var sut = new RunScenarioCommandHandler(db);

        var result = await sut.Handle(
            new RunScenarioCommand(CityType.Coastal, 2040, "{\"technologyIds\":[]}"),
            CancellationToken.None);

        result.TotalCarbonImpactKgPerYear.Should().Be(0m);
        result.TotalCostUsd.Should().Be(0m);
    }

    [Fact]
    public async Task Valid_technologyIds_returns_summed_totals()
    {
        var tech1 = CityTechnology.Create("أ", "Solar", "وصف", "desc", "طاقة", "Energy", 100m, 50m);
        var tech2 = CityTechnology.Create("ب", "Wind", "وصف", "desc", "طاقة", "Energy", 200m, 80m);

        var db = BuildDb(new[] { tech1, tech2 });
        var sut = new RunScenarioCommandHandler(db);

        var ids = $"[\"{tech1.Id}\",\"{tech2.Id}\"]";
        var result = await sut.Handle(
            new RunScenarioCommand(CityType.Mixed, 2050, $"{{\"technologyIds\":{ids}}}"),
            CancellationToken.None);

        result.TotalCarbonImpactKgPerYear.Should().Be(300m);
        result.TotalCostUsd.Should().Be(130m);
    }

    [Fact]
    public async Task Parse_failure_returns_zero_totals_and_invalid_config_message()
    {
        var db = BuildDb(System.Array.Empty<CityTechnology>());
        var sut = new RunScenarioCommandHandler(db);

        var result = await sut.Handle(
            new RunScenarioCommand(CityType.Industrial, 2060, "NOT VALID JSON {{{{"),
            CancellationToken.None);

        result.TotalCarbonImpactKgPerYear.Should().Be(0m);
        result.TotalCostUsd.Should().Be(0m);
        result.SummaryEn.Should().Be("Invalid configuration");
    }

    private static ICceDbContext BuildDb(IEnumerable<CityTechnology> techs)
    {
        var db = Substitute.For<ICceDbContext>();
        db.CityTechnologies.Returns(techs.AsQueryable());
        return db;
    }
}
