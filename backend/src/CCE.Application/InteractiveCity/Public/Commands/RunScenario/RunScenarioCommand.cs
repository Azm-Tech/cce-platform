using CCE.Application.InteractiveCity.Public.Dtos;
using CCE.Domain.InteractiveCity;
using MediatR;

namespace CCE.Application.InteractiveCity.Public.Commands.RunScenario;

public sealed record RunScenarioCommand(
    CityType CityType,
    int TargetYear,
    string ConfigurationJson) : IRequest<CityScenarioRunResultDto>;
