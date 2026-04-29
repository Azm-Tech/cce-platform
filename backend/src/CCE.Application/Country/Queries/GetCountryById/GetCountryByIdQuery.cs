using CCE.Application.Country.Dtos;
using MediatR;

namespace CCE.Application.Country.Queries.GetCountryById;

public sealed record GetCountryByIdQuery(System.Guid Id) : IRequest<CountryDto?>;
