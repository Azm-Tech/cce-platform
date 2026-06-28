using CCE.Domain.Evaluation;

namespace CCE.Application.Evaluation.DTOs;

public sealed record ServiceEvaluationDto(
    System.Guid Id,
    EvaluationRating OverallSatisfaction,
    EvaluationRating EaseOfUse,
    EvaluationRating ContentSuitability,
    string Feedback,
    System.Guid? UserId,
    System.DateTimeOffset CreatedOn,
    System.Guid CreatedById);
