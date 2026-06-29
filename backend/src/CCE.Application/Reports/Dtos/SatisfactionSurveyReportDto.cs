using CCE.Domain.Evaluation;

namespace CCE.Application.Reports.Dtos;

public sealed record SatisfactionSurveyReportDto(
    Guid Id,
    EvaluationRating OverallSatisfaction,
    EvaluationRating EaseOfUse,
    EvaluationRating ContentSuitability,
    string Feedback,
    Guid? UserId,
    DateTimeOffset SubmittedAt
);
