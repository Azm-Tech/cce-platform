using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using MediatR;

namespace CCE.Application.Community.Queries.GetModerationQueue;

public sealed record GetModerationQueueQuery(
    string? Status,
    string? ContentType,
    int Page,
    int PageSize) : IRequest<Response<PagedResult<ModerationQueueItemDto>>>;
