namespace CCE.Application.Reports.Rows;

/// <summary>One row in the news report. Public properties become CSV columns in order.</summary>
public sealed class NewsReportRow
{
    public System.Guid Id { get; set; }
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public System.Guid AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }
    public System.DateTimeOffset? PublishedOn { get; set; }
}
