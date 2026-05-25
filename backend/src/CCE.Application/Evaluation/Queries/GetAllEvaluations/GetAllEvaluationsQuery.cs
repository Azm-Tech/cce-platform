using CCE.Application.Common;
using CCE.Application.Evaluation.DTOs;
using MediatR;

namespace CCE.Application.Evaluation.Queries.GetAllEvaluations;

public sealed record GetAllEvaluationsQuery : IRequest<Response<List<ServiceEvaluationDto>>>;
