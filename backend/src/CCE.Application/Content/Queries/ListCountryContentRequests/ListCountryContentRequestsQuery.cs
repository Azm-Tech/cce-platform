using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Domain.Country;
using MediatR;

namespace CCE.Application.Content.Queries.ListCountryContentRequests;

public sealed record ListCountryContentRequestsQuery(
    int Page = 1,
    int PageSize = 20,
    CountryContentRequestStatus? Status = null,
    ContentKind? Kind = null,
    System.Guid? CountryId = null) : IRequest<Response<PagedResult<CountryContentRequestDto>>>;
