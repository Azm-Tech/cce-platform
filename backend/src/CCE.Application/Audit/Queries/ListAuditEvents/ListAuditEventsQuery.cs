using CCE.Application.Audit.Dtos;
using CCE.Application.Common.Pagination;
using MediatR;

namespace CCE.Application.Audit.Queries.ListAuditEvents;

public sealed record ListAuditEventsQuery(
    int Page = 1,
    int PageSize = 50,
    string? Actor = null,
    string? ActionPrefix = null,
    string? ResourceType = null,
    System.Guid? CorrelationId = null,
    System.DateTimeOffset? From = null,
    System.DateTimeOffset? To = null) : IRequest<PagedResult<AuditEventDto>>;
