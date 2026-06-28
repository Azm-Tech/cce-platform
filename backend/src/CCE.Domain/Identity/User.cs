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
    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public string JobTitle { get; private set; } = string.Empty;

    public string OrganizationName { get; private set; } = string.Empty;

    /// <summary>UI locale preference. Allowed values: <c>"ar"</c>, <c>"en"</c>. Default <c>"ar"</c>.</summary>
    public string LocalePreference { get; private set; } = "ar";

    /// <summary>Self-declared knowledge level. Default <see cref="KnowledgeLevel.Beginner"/>.</summary>
    public KnowledgeLevel KnowledgeLevel { get; private set; } = KnowledgeLevel.Beginner;

    public ICollection<UserInterestTopic> UserInterestTopics { get; private set; } = new List<UserInterestTopic>();

    /// <summary>Optional user country (FK to <c>Country</c>); only set for state-rep / community users with a profile.</summary>
    public System.Guid? CountryId { get; set; }

    /// <summary>UTC moment this user was created.</summary>
    public DateTimeOffset CreatedOn { get; private set; }

    /// <summary>Actor that created this user.</summary>
    public Guid CreatedById { get; private set; }

    /// <summary>UTC moment this user was last modified; null if never modified.</summary>
    public DateTimeOffset? LastModifiedOn { get; private set; }

    /// <summary>Actor that last modified this user; null if never modified.</summary>
    public Guid? LastModifiedById { get; private set; }

    /// <summary>Optional avatar URL (CDN-served).</summary>
    public string? AvatarUrl { get; private set; }

    /// <summary>Admin-managed account status. Default <see cref="UserStatus.Active"/>.</summary>
    public UserStatus Status { get; private set; } = UserStatus.Active;

    /// <summary>Denormalized follower count (source of truth = UserFollow rows). Updated on follow/unfollow.</summary>
    public int FollowerCount { get; private set; }

    /// <summary>Denormalized following count (source of truth = UserFollow rows). Updated on follow/unfollow.</summary>
    public int FollowingCount { get; private set; }

    /// <summary>Denormalized published-post count (source of truth = Post rows with Status=Published). Updated on Publish/SoftDelete.</summary>
    public int PostsCount { get; private set; }

    /// <summary>Denormalized reply count (source of truth = PostReply rows). Updated on reply create/soft-delete.</summary>
    public int CommentsCount { get; private set; }

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
    public static User CreateStubFromEntraId(System.Guid objectId, string email, string displayName, ISystemClock clock)
    {
        var user = new User
        {
            Id = System.Guid.NewGuid(),
            EntraIdObjectId = objectId,
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            EmailConfirmed = false,
        };
        user.MarkAsCreated(user.Id, clock);
        return user;
    }

    /// <summary>
    /// Factory for stub User rows created on first AD login via the integration gateway.
    /// Profile fields default to empty; operator/admin should prompt for completion.
    /// </summary>
    public static User CreateStubFromAd(
        string email,
        string? firstName,
        string? lastName,
        string? displayName,
        ISystemClock clock)
    {
        var user = new User
        {
            Id = System.Guid.NewGuid(),
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = firstName ?? displayName ?? string.Empty,
            LastName = lastName ?? string.Empty,
            JobTitle = string.Empty,
            OrganizationName = string.Empty,
        };
        user.MarkAsCreated(user.Id, clock);
        return user;
    }

    public static User RegisterLocal(
        string firstName,
        string lastName,
        string email,
        string jobTitle,
        string organizationName,
        string phoneNumber,
        ISystemClock clock)
    {
        var user = new User
        {
            Id = System.Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            PhoneNumber = phoneNumber,
            EmailConfirmed = false,
        };
        user.UpdateProfile(firstName, lastName, jobTitle, organizationName);
        user.MarkAsCreated(user.Id, clock);
        return user;
    }

    public static User CreateByAdmin(string firstName, string lastName, string email, string phone, Guid by, ISystemClock clock)
    {
        var user = new User
        {
            Id = System.Guid.NewGuid(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            PhoneNumber = phone,
            EmailConfirmed = true,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            JobTitle = string.Empty,
            OrganizationName = string.Empty,
        };
        user.MarkAsCreated(by, clock);
        return user;
    }

    public void UpdateProfile(string firstName, string lastName, string jobTitle, string organizationName)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new DomainException("FirstName is required.");
        if (string.IsNullOrWhiteSpace(lastName)) throw new DomainException("LastName is required.");
        if (string.IsNullOrWhiteSpace(jobTitle)) throw new DomainException("JobTitle is required.");
        if (string.IsNullOrWhiteSpace(organizationName)) throw new DomainException("OrganizationName is required.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        JobTitle = jobTitle.Trim();
        OrganizationName = organizationName.Trim();
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

    public void UpdateInterests(IEnumerable<System.Guid> interestTopicIds)
    {
        if (interestTopicIds is null)
            throw new DomainException("interestTopicIds collection cannot be null.");
        UserInterestTopics.Clear();
        foreach (var id in interestTopicIds.Distinct())
            UserInterestTopics.Add(new UserInterestTopic { UserId = Id, InterestTopicId = id });
    }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedOn { get; private set; }

    public Guid? DeletedById { get; private set; }

    public void MarkAsCreated(Guid by, ISystemClock clock)
    {
        if (by == Guid.Empty) throw new DomainException("CreatedById is required.");
        CreatedOn = clock.UtcNow;
        CreatedById = by;
    }

    public void MarkAsModified(Guid by, ISystemClock clock)
    {
        if (by == Guid.Empty) throw new DomainException("ModifiedById is required.");
        LastModifiedOn = clock.UtcNow;
        LastModifiedById = by;
    }

    public void SoftDelete(Guid by, DateTimeOffset now)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedOn = now;
        DeletedById = by;
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

    public void UpdateEmail(string newEmail)
    {
        if (string.IsNullOrWhiteSpace(newEmail)) throw new DomainException("Email is required.");
        var trimmed = newEmail.Trim();
        Email = trimmed;
        NormalizedEmail = trimmed.ToUpperInvariant();
        UserName = trimmed;
        NormalizedUserName = trimmed.ToUpperInvariant();
        EmailConfirmed = true;
    }

    public void UpdatePhoneNumber(string newPhone)
    {
        if (string.IsNullOrWhiteSpace(newPhone)) throw new DomainException("Phone number is required.");
        PhoneNumber = NormalizePhone(newPhone);
        PhoneNumberConfirmed = true;
    }

    public static string NormalizePhone(string phone)
        => new string(System.Linq.Enumerable.Where(phone, char.IsDigit).ToArray());

    public void ChangeStatus(UserStatus newStatus) => Status = newStatus;

    public void IncrementFollowers() => FollowerCount++;
    public void DecrementFollowers() { if (FollowerCount > 0) FollowerCount--; }
    public void IncrementFollowing() => FollowingCount++;
    public void DecrementFollowing() { if (FollowingCount > 0) FollowingCount--; }
    public void IncrementPostsCount() => PostsCount++;
    public void DecrementPostsCount() { if (PostsCount > 0) PostsCount--; }
    public void IncrementCommentsCount() => CommentsCount++;
    public void DecrementCommentsCount() { if (CommentsCount > 0) CommentsCount--; }

    public void Activate() => Status = UserStatus.Active;

    public void Deactivate() => Status = UserStatus.Inactive;
}
