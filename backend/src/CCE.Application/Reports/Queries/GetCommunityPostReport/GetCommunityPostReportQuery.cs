using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Reports.Dtos;
using MediatR;

namespace CCE.Application.Reports.Queries.GetCommunityPostReport;

public sealed record GetCommunityPostReportQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Response<PagedResult<CommunityPostReportDto>>>;
