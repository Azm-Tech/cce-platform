using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.InteractiveMaps.Dtos;
using MediatR;

namespace CCE.Application.InteractiveMaps.Queries.ListInteractiveMapNodes;

public sealed record ListInteractiveMapNodesQuery(
    System.Guid MapId,
    int Page = 1,
    int PageSize = 20,
    bool? IsActive = null) : IRequest<Response<PagedResult<InteractiveMapNodeDto>>>;
