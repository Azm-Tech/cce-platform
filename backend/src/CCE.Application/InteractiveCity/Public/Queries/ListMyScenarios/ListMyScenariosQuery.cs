using CCE.Application.InteractiveCity.Public.Dtos;
using MediatR;

namespace CCE.Application.InteractiveCity.Public.Queries.ListMyScenarios;

public sealed record ListMyScenariosQuery(System.Guid UserId)
    : IRequest<System.Collections.Generic.IReadOnlyList<CityScenarioDto>>;
