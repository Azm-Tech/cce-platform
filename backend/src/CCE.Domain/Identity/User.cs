using CCE.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace CCE.Domain.Identity;

/// <summary>
/// CCE user account — extends ASP.NET Identity's <see cref="IdentityUser{TKey}"/> with
/// CCE-specific profile fields: locale preference, knowledge level, interests, country,
/// avatar. Identity columns (Email, UserName, PasswordHash, etc.) are inherited.
/// </summary>
[Audited]
public class User : IdentityUser<System.Guid>
{
    /// <summary>UI locale preference. Allowed values: <c>"ar"</c>, <c>"en"</c>. Default <c>"ar"</c>.</summary>
    public string LocalePreference { get; private set; } = "ar";

    /// <summary>Self-declared knowledge level. Default <see cref="KnowledgeLevel.Beginner"/>.</summary>
    public KnowledgeLevel KnowledgeLevel { get; private set; } = KnowledgeLevel.Beginner;

    /// <summary>User-selected topic interests (free-text PascalCase tags). EF maps as JSON column.</summary>
    public List<string> Interests { get; private set; } = new();

    /// <summary>Optional user country (FK to <c>Country</c>); only set for state-rep / community users with a profile.</summary>
    public System.Guid? CountryId { get; set; }

    /// <summary>Optional avatar URL (CDN-served).</summary>
    public string? AvatarUrl { get; private set; }

    /// <summary>
    /// Updates the locale preference. Only <c>"ar"</c> and <c>"en"</c> are accepted.
    /// </summary>
    public void SetLocalePreference(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            throw new DomainException("locale must be 'ar' or 'en'.");
        }
        if (locale != "ar" && locale != "en")
        {
            throw new DomainException($"locale '{locale}' is not supported (must be 'ar' or 'en').");
        }
        LocalePreference = locale;
    }

    public void SetKnowledgeLevel(KnowledgeLevel level) => KnowledgeLevel = level;

    /// <summary>
    /// Replaces the interests list. Trims whitespace, deduplicates, and removes empty entries.
    /// </summary>
    public void UpdateInterests(IEnumerable<string> interests)
    {
        if (interests is null)
        {
            throw new DomainException("interests collection cannot be null.");
        }
        Interests = interests
            .Select(static s => s?.Trim() ?? string.Empty)
            .Where(static s => s.Length > 0)
            .Distinct()
            .ToList();
    }

    public void AssignCountry(System.Guid countryId) => CountryId = countryId;

    public void ClearCountry() => CountryId = null;

    /// <summary>
    /// Sets the avatar URL. Must be HTTPS or null. Pass null to clear.
    /// </summary>
    public void SetAvatarUrl(string? url)
    {
        if (url is null)
        {
            AvatarUrl = null;
            return;
        }
        if (!url.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException($"avatar URL must use https:// (got '{url}').");
        }
        AvatarUrl = url;
    }
}
