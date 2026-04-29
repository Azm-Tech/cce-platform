using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicEvents;

public sealed record ListPublicEventsQuery(
    int Page = 1,
    int PageSize = 20,
    System.DateTimeOffset? From = null,
    System.DateTimeOffset? To = null) : IRequest<PagedResult<PublicEventDto>>;
