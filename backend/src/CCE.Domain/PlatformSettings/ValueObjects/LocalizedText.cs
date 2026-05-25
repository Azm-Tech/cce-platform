using CCE.Domain.Common;

namespace CCE.Domain.PlatformSettings.ValueObjects;

/// <summary>
/// Immutable bilingual text value object. Used throughout PlatformSettings
/// to replace paired Ar/En properties with a single cohesive concept.
/// </summary>
public sealed class LocalizedText
{
    public string Ar { get; private init; } = string.Empty;
    public string En { get; private init; } = string.Empty;

    private LocalizedText() { } // EF Core materialization

    private LocalizedText(string ar, string en)
    {
        Ar = ar;
        En = en;
    }

    /// <summary>Creates a <see cref="LocalizedText"/> with validation (both required).</summary>
    public static LocalizedText Create(string ar, string en)
    {
        if (string.IsNullOrWhiteSpace(ar)) throw new DomainException("Arabic text is required.");
        if (string.IsNullOrWhiteSpace(en)) throw new DomainException("English text is required.");
        return new LocalizedText(ar, en);
    }

    /// <summary>Creates a <see cref="LocalizedText"/> without validation (allows empty strings).</summary>
    public static LocalizedText From(string ar, string en)
    {
        return new LocalizedText(ar ?? string.Empty, en ?? string.Empty);
    }
}
