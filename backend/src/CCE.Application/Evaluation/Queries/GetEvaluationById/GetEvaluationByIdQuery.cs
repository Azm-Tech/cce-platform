using CCE.Application.Evaluation.DTOs;
using MediatR;

namespace CCE.Application.Evaluation.Queries.GetEvaluationById;

public sealed record GetEvaluationByIdQuery(System.Guid Id) : IRequest<ServiceEvaluationDto?>;
