namespace CCE.Application.Reports.Rows;

/// <summary>One row in the satisfaction-survey report. Public properties become CSV columns in order.</summary>
public sealed class SatisfactionSurveyReportRow
{
    public System.Guid Id { get; set; }
    public System.Guid? UserId { get; set; }
    public int Rating { get; set; }
    public string? CommentAr { get; set; }
    public string? CommentEn { get; set; }
    public string Page { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
    public System.DateTimeOffset SubmittedOn { get; set; }
}
