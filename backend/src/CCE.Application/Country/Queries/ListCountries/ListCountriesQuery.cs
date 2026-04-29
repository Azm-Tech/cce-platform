using CCE.Application.Common.Pagination;
using CCE.Application.Country.Dtos;
using MediatR;

namespace CCE.Application.Country.Queries.ListCountries;

public sealed record ListCountriesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null) : IRequest<PagedResult<CountryDto>>;
