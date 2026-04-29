namespace CCE.Application.Reports.Rows;

public sealed class CountryProfileReportRow
{
    public System.Guid CountryId { get; set; }
    public string IsoAlpha3 { get; set; } = string.Empty;
    public string IsoAlpha2 { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string RegionEn { get; set; } = string.Empty;
    public bool CountryIsActive { get; set; }
    public bool HasProfile { get; set; }
    public System.DateTimeOffset? LastProfileUpdatedOn { get; set; }
    public System.Guid? LastProfileUpdatedById { get; set; }
}
