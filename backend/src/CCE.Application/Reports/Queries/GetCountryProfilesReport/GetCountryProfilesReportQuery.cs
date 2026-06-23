using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Reports.Dtos;
using MediatR;

namespace CCE.Application.Reports.Queries.GetCountryProfilesReport;

public sealed record GetCountryProfilesReportQuery(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Response<PagedResult<CountryProfilesReportDto>>>;
