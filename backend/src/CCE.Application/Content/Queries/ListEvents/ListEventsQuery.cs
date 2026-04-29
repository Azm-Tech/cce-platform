using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.ListEvents;

public sealed record ListEventsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    System.DateTimeOffset? FromDate = null,
    System.DateTimeOffset? ToDate = null) : IRequest<PagedResult<EventDto>>;
