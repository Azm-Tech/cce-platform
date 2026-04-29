using CCE.Application.CountryPublic.Dtos;
using MediatR;

namespace CCE.Application.CountryPublic.Queries.ListPublicCountries;

public sealed record ListPublicCountriesQuery(string? Search = null) : IRequest<IReadOnlyList<PublicCountryDto>>;
