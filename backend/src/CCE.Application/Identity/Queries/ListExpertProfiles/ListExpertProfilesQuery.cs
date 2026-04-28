using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using MediatR;

namespace CCE.Application.Identity.Queries.ListExpertProfiles;

public sealed record ListExpertProfilesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null) : IRequest<PagedResult<ExpertProfileDto>>;
