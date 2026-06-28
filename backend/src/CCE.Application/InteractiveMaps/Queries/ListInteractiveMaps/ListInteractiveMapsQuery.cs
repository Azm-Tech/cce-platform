using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.InteractiveMaps.Dtos;
using MediatR;

namespace CCE.Application.InteractiveMaps.Queries.ListInteractiveMaps;

public sealed record ListInteractiveMapsQuery(
    int Page = 1,
    int PageSize = 20,
    bool? IsActive = null) : IRequest<Response<PagedResult<InteractiveMapDto>>>;
