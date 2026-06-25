using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Reports.Dtos;
using MediatR;

namespace CCE.Application.Reports.Queries.GetResourcesReport;

public sealed record GetResourcesReportQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Response<PagedResult<ResourcesReportDto>>>;
