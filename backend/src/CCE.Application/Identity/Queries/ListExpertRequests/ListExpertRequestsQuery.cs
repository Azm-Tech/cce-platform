using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.Identity.Queries.ListExpertRequests;

public sealed record ListExpertRequestsQuery(
    int Page = 1,
    int PageSize = 20,
    ExpertRegistrationStatus? Status = null,
    System.Guid? RequestedById = null) : IRequest<PagedResult<ExpertRequestDto>>;
