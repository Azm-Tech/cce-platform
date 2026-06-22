using CCE.Application.Common;
using CCE.Application.Reports.Dtos;
using MediatR;

namespace CCE.Application.Reports.Queries.GetUserPreferenceReport;

public sealed record GetUserPreferenceReportQuery()
    : IRequest<Response<List<UserPreferenceReportDto>>>;
