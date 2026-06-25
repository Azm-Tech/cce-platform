using CCE.Application.Common;
using CCE.Application.Reports.Dtos;
using MediatR;

namespace CCE.Application.Reports.Queries.GetExpertReport;

public sealed record GetExpertReportQuery()
    : IRequest<Response<List<ExpertReportDto>>>;
