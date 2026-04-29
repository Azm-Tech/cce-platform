namespace CCE.Application.Reports.Rows;

/// <summary>One row in the experts report. Public properties become CSV columns in order.</summary>
public sealed class ExpertReportRow
{
    public System.Guid Id { get; set; }
    public System.Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string AcademicTitleEn { get; set; } = string.Empty;
    public string AcademicTitleAr { get; set; } = string.Empty;
    public string ExpertiseTags { get; set; } = string.Empty;
    public System.DateTimeOffset ApprovedOn { get; set; }
}
