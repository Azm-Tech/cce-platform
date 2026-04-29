using CCE.Application.InteractiveCity;
using CCE.Application.InteractiveCity.Public.Commands.SaveScenario;
using CCE.Domain.InteractiveCity;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.InteractiveCity;

public class SaveScenarioCommandHandlerTests
{
    [Fact]
    public async Task Persists_scenario_when_inputs_valid()
    {
        var (sut, service) = BuildSut();

        await sut.Handle(BuildCmd(), CancellationToken.None);

        await service.Received(1).SaveAsync(Arg.Any<CityScenario>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_dto_with_correct_fields()
    {
        var (sut, _) = BuildSut();

        var dto = await sut.Handle(BuildCmd(), CancellationToken.None);

        dto.NameAr.Should().Be("سيناريو");
        dto.NameEn.Should().Be("Scenario");
        dto.CityType.Should().Be(CityType.Coastal);
        dto.TargetYear.Should().Be(2040);
        dto.ConfigurationJson.Should().Be("{\"technologyIds\":[]}");
    }

    private static SaveScenarioCommand BuildCmd() =>
        new(System.Guid.NewGuid(), "سيناريو", "Scenario", CityType.Coastal, 2040,
            "{\"technologyIds\":[]}");

    private static (SaveScenarioCommandHandler sut, ICityScenarioService service) BuildSut()
    {
        var service = Substitute.For<ICityScenarioService>();
        var sut = new SaveScenarioCommandHandler(service, new FakeSystemClock());
        return (sut, service);
    }
}
