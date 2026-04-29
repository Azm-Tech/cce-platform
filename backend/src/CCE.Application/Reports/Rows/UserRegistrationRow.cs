namespace CCE.Application.Reports.Rows;

/// <summary>One row in the users-registrations report. Public properties become CSV columns in order.</summary>
public sealed class UserRegistrationRow
{
    public System.Guid Id { get; set; }
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string Roles { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string LocalePreference { get; set; } = string.Empty;
    public string? CountryId { get; set; }
}
