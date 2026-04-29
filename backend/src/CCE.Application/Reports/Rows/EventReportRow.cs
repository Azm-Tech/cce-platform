namespace CCE.Application.Reports.Rows;

/// <summary>One row in the events report. Public properties become CSV columns in order.</summary>
public sealed class EventReportRow
{
    public System.Guid Id { get; set; }
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public System.DateTimeOffset StartsOn { get; set; }
    public System.DateTimeOffset EndsOn { get; set; }
    public string? LocationEn { get; set; }
    public string? OnlineMeetingUrl { get; set; }
    public string ICalUid { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
