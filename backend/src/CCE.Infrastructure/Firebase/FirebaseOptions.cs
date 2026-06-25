namespace CCE.Infrastructure.Firebase;

public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";
    public string ProjectId { get; init; } = string.Empty;
    /// <summary>Raw service-account JSON string. Inject via env var or user-secrets — never commit to source control.</summary>
    public string ServiceAccountJson { get; init; } = string.Empty;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ProjectId)
                             && !string.IsNullOrWhiteSpace(ServiceAccountJson);
}
