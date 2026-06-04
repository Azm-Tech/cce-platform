using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicEvents;

public sealed record ListPublicEventsQuery(
    int Page = 1,
    int PageSize = 20,
    System.DateTimeOffset? From = null,
    System.DateTimeOffset? To = null,
    System.Guid? TopicId = null,
    System.Collections.Generic.IReadOnlyList<System.Guid>? TagIds = null) : IRequest<Response<PagedResult<PublicEventDto>>>;
