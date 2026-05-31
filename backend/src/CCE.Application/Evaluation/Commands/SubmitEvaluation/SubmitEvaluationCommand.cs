using CCE.Application.Common;
using CCE.Domain.Evaluation;
using MediatR;

namespace CCE.Application.Evaluation.Commands.SubmitEvaluation;

public sealed record SubmitEvaluationCommand(
    EvaluationRating OverallSatisfaction,
    EvaluationRating EaseOfUse,
    EvaluationRating ContentSuitability,
    string Feedback) : IRequest<Response<VoidData>>;
