using CCE.Application.Common;
using CCE.Application.InteractiveCity.Public.Dtos;
using MediatR;

namespace CCE.Application.InteractiveCity.Public.Queries.ListCityTechnologies;

public sealed record ListCityTechnologiesQuery : IRequest<Response<System.Collections.Generic.IReadOnlyList<CityTechnologyDto>>>;
