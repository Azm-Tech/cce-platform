using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Country.Dtos;
using CCE.Domain.Common;
using CCE.Domain.Country;
using MediatR;

namespace CCE.Application.Country.Queries.ListCountries;

public sealed record ListCountriesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null,
    PublicCountrySortBy SortBy = PublicCountrySortBy.NameEn,
    SortOrder SortOrder = SortOrder.Ascending,
    bool? IsCceCountry = null) : IRequest<Response<PagedResult<CountryDto>>>;