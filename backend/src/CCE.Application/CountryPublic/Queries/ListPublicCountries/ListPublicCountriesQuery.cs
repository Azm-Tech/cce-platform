using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.CountryPublic.Dtos;
using CCE.Domain.Common;
using CCE.Domain.Country;
using MediatR;

namespace CCE.Application.CountryPublic.Queries.ListPublicCountries;

public sealed record ListPublicCountriesQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 20,
    PublicCountrySortBy SortBy = PublicCountrySortBy.TotalIndex,
    SortOrder SortOrder = SortOrder.Descending) : IRequest<Response<PagedResult<PublicCountryDto>>>;
