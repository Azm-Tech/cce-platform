namespace CCE.Application.Reports.Rows;

/// <summary>One row in the community-posts report. Public properties become CSV columns in order.</summary>
public sealed class CommunityPostReportRow
{
    public System.Guid Id { get; set; }
    public System.Guid TopicId { get; set; }
    public System.Guid AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public string Locale { get; set; } = string.Empty;
    public bool IsAnswerable { get; set; }
    public bool IsDeleted { get; set; }
    public System.DateTimeOffset CreatedOn { get; set; }
}
