using CCE.Application.InteractiveCity.Public.Dtos;
using CCE.Domain.InteractiveCity;
using MediatR;

namespace CCE.Application.InteractiveCity.Public.Commands.SaveScenario;

public sealed record SaveScenarioCommand(
    System.Guid UserId,
    string NameAr,
    string NameEn,
    CityType CityType,
    int TargetYear,
    string ConfigurationJson) : IRequest<CityScenarioDto>;
