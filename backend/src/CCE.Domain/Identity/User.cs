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
    /// Sub-11: stable Entra ID Object ID (<c>oid</c> claim) for this user. Populated lazily on
    /// first sign-in by <c>EntraIdUserResolver</c>. Null until the user signs in via Entra ID
    /// for the first time post-cutover. Filtered unique index enforces no two users share
    /// the same objectId.
    /// </summary>
    public System.Guid? EntraIdObjectId { get; private set; }

    /// <summary>
    /// Sub-11: idempotent linkage of an existing CCE user to their Entra ID objectId.
    /// Throws if already linked to a different objectId.
    /// </summary>
    public void LinkEntraIdObjectId(System.Guid objectId)
    {
        if (EntraIdObjectId.HasValue && EntraIdObjectId.Value != objectId)
        {
            throw new DomainException(
                $"User {Id} is already linked to Entra ID objectId {EntraIdObjectId}; refusing to overwrite with {objectId}.");
        }
        EntraIdObjectId = objectId;
    }

    /// <summary>
    /// Sub-11: factory for stub User rows created on first sign-in by external partner-tenant
    /// users who don't have a pre-existing CCE row. Other fields default; user completes
    /// profile in CCE later. Operator/admin must confirm email + assign roles before access.
    /// </summary>
    public static User CreateStubFromEntraId(System.Guid objectId, string email, string displayName)
    {
        return new User
        {
            Id = System.Guid.NewGuid(),
            EntraIdObjectId = objectId,
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            EmailConfirmed = false,
        };
    }

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
