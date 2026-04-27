namespace CCE.Domain.Content;

/// <summary>
/// Format of a <c>Resource</c>. Drives both UI rendering (icon + viewer) and
/// validation rules (e.g., Video resources may require an associated transcript file).
/// </summary>
public enum ResourceType
{
    Pdf = 0,
    Video = 1,
    Image = 2,
    Link = 3,
    Document = 4,
}
