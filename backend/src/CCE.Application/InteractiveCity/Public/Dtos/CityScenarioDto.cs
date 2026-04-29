using CCE.Domain.InteractiveCity;

namespace CCE.Application.InteractiveCity.Public.Dtos;

public sealed record CityScenarioDto(
    System.Guid Id,
    string NameAr,
    string NameEn,
    CityType CityType,
    int TargetYear,
    string ConfigurationJson,
    System.DateTimeOffset CreatedOn,
    System.DateTimeOffset LastModifiedOn);
