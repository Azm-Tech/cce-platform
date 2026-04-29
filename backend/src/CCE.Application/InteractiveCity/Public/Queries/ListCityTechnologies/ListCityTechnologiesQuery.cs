using CCE.Application.InteractiveCity.Public.Dtos;
using MediatR;

namespace CCE.Application.InteractiveCity.Public.Queries.ListCityTechnologies;

public sealed record ListCityTechnologiesQuery : IRequest<System.Collections.Generic.IReadOnlyList<CityTechnologyDto>>;
