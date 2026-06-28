namespace CCE.Application.Reports.Dtos;

public sealed record SatisfactionSurveyReportDto(
    Guid Id,
    int OverallSatisfaction,
    int EaseOfUse,
    int ContentSuitability,
    string Feedback,
    Guid? UserId,
    DateTimeOffset SubmittedAt
);
