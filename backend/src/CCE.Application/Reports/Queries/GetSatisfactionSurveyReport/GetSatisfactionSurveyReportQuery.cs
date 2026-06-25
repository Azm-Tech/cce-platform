using CCE.Application.Common;
using CCE.Application.Reports.Dtos;
using MediatR;

namespace CCE.Application.Reports.Queries.GetSatisfactionSurveyReport;

public sealed record GetSatisfactionSurveyReportQuery()
    : IRequest<Response<List<SatisfactionSurveyReportDto>>>;
