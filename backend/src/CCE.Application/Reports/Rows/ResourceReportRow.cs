namespace CCE.Application.Reports.Rows;

/// <summary>One row in the resources report. Public properties become CSV columns in order.</summary>
public sealed class ResourceReportRow
{
    public System.Guid Id { get; set; }
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public System.Guid CategoryId { get; set; }
    public System.Guid? CountryId { get; set; }
    public bool IsCenterManaged { get; set; }
    public bool IsPublished { get; set; }
    public System.DateTimeOffset? PublishedOn { get; set; }
    public long ViewCount { get; set; }
    public bool IsDeleted { get; set; }
}
