using CCE.Application.InteractiveCity;
using CCE.Application.InteractiveCity.Public.Commands.DeleteMyScenario;
using CCE.Domain.InteractiveCity;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.InteractiveCity;

public class DeleteMyScenarioCommandHandlerTests
{
    [Fact]
    public async Task Throws_KeyNotFoundException_when_scenario_not_found()
    {
        var service = Substitute.For<ICityScenarioService>();
        service.FindAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>())
            .Returns((CityScenario?)null);

        var sut = new DeleteMyScenarioCommandHandler(service, new FakeSystemClock());

        var act = async () => await sut.Handle(
            new DeleteMyScenarioCommand(System.Guid.NewGuid(), System.Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>();
    }

    [Fact]
    public async Task Throws_KeyNotFoundException_when_scenario_owned_by_different_user()
    {
        var clock = new FakeSystemClock();
        var ownerId = System.Guid.NewGuid();
        var requesterId = System.Guid.NewGuid();

        var scenario = CityScenario.Create(ownerId, "سيناريو", "Scenario", CityType.Coastal, 2040,
            "{\"technologyIds\":[]}", clock);

        var service = Substitute.For<ICityScenarioService>();
        service.FindAsync(scenario.Id, Arg.Any<CancellationToken>()).Returns(scenario);

        var sut = new DeleteMyScenarioCommandHandler(service, clock);

        var act = async () => await sut.Handle(
            new DeleteMyScenarioCommand(scenario.Id, requesterId),
            CancellationToken.None);

        await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>();
    }

    [Fact]
    public async Task Soft_deletes_and_persists_when_owner_requests_deletion()
    {
        var clock = new FakeSystemClock();
        var userId = System.Guid.NewGuid();

        var scenario = CityScenario.Create(userId, "سيناريو", "Scenario", CityType.Coastal, 2040,
            "{\"technologyIds\":[]}", clock);

        var service = Substitute.For<ICityScenarioService>();
        service.FindAsync(scenario.Id, Arg.Any<CancellationToken>()).Returns(scenario);

        var sut = new DeleteMyScenarioCommandHandler(service, clock);

        await sut.Handle(new DeleteMyScenarioCommand(scenario.Id, userId), CancellationToken.None);

        scenario.IsDeleted.Should().BeTrue();
        await service.Received(1).UpdateAsync(scenario, Arg.Any<CancellationToken>());
    }
}
