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
    public string LocalePreference { get; set; } = "ar";

    /// <summary>Self-declared knowledge level. Default <see cref="KnowledgeLevel.Beginner"/>.</summary>
    public KnowledgeLevel KnowledgeLevel { get; set; } = KnowledgeLevel.Beginner;

    /// <summary>User-selected topic interests (free-text PascalCase tags). EF maps as JSON column.</summary>
    public List<string> Interests { get; private set; } = new();

    /// <summary>Optional user country (FK to <c>Country</c>); only set for state-rep / community users with a profile.</summary>
    public System.Guid? CountryId { get; set; }

    /// <summary>Optional avatar URL (CDN-served).</summary>
    public string? AvatarUrl { get; set; }
}
