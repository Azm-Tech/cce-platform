using CCE.Application.Common;
using CCE.Application.InteractiveCity.Public.Dtos;
using MediatR;

namespace CCE.Application.InteractiveCity.Public.Queries.ListMyScenarios;

public sealed record ListMyScenariosQuery(System.Guid UserId)
    : IRequest<Response<System.Collections.Generic.IReadOnlyList<CityScenarioDto>>>;
