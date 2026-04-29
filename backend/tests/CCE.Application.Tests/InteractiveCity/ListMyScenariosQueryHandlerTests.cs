using CCE.Application.Common.Interfaces;
using CCE.Application.InteractiveCity.Public.Queries.ListMyScenarios;
using CCE.Domain.InteractiveCity;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.InteractiveCity;

public class ListMyScenariosQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_list_when_user_has_no_scenarios()
    {
        var db = BuildDb(System.Array.Empty<CityScenario>());
        var sut = new ListMyScenariosQueryHandler(db);

        var result = await sut.Handle(
            new ListMyScenariosQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_only_own_scenarios_sorted_by_LastModifiedOn_descending()
    {
        var clock = new FakeSystemClock();
        var userId = System.Guid.NewGuid();
        var otherId = System.Guid.NewGuid();

        var older = CityScenario.Create(userId, "قديم", "Older", CityType.Coastal, 2030,
            "{\"technologyIds\":[]}", clock);
        clock.Advance(System.TimeSpan.FromMinutes(5));
        var newer = CityScenario.Create(userId, "أحدث", "Newer", CityType.Mixed, 2040,
            "{\"technologyIds\":[]}", clock);
        var other = CityScenario.Create(otherId, "آخر", "Other", CityType.Industrial, 2050,
            "{\"technologyIds\":[]}", clock);

        var db = BuildDb(new[] { older, newer, other });
        var sut = new ListMyScenariosQueryHandler(db);

        var result = await sut.Handle(new ListMyScenariosQuery(userId), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].NameEn.Should().Be("Newer");
        result[1].NameEn.Should().Be("Older");
    }

    private static ICceDbContext BuildDb(IEnumerable<CityScenario> scenarios)
    {
        var db = Substitute.For<ICceDbContext>();
        db.CityScenarios.Returns(scenarios.AsQueryable());
        return db;
    }
}
