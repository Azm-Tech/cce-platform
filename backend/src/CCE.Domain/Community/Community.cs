using System.Text.RegularExpressions;
using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>
/// A subreddit-like container that owns posts (D1). Public communities are world-readable;
/// private communities are members-only and joinable by request. <see cref="MemberCount"/> is
/// denormalized; presentation-only theming lives in <see cref="PresentationJson"/> (opaque blob, §3).
/// </summary>
[Audited]
public sealed class Community : AggregateRoot<System.Guid>
{
    public const int MaxNameLength = 150;
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private Community(
        System.Guid id, string nameAr, string nameEn,
        string descriptionAr, string descriptionEn,
        string slug, CommunityVisibility visibility, string? presentationJson) : base(id)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        Slug = slug;
        Visibility = visibility;
        PresentationJson = presentationJson;
        IsActive = true;
    }

    public string NameAr { get; private set; }
    public string NameEn { get; private set; }
    public string DescriptionAr { get; private set; }
    public string DescriptionEn { get; private set; }
    public string Slug { get; private set; }
    public CommunityVisibility Visibility { get; private set; }
    public string? PresentationJson { get; private set; }
    public int MemberCount { get; private set; }
    public int PostCount { get; private set; }
    public int FollowerCount { get; private set; }
    public bool IsActive { get; private set; }

    public bool IsPublic => Visibility == CommunityVisibility.Public;

    public static Community Create(
        string nameAr, string nameEn,
        string descriptionAr, string descriptionEn,
        string slug, CommunityVisibility visibility, string? presentationJson = null)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        if (nameAr.Length > MaxNameLength || nameEn.Length > MaxNameLength)
            throw new DomainException($"Name exceeds {MaxNameLength} chars.");
        if (string.IsNullOrWhiteSpace(slug) || !SlugPattern.IsMatch(slug))
            throw new DomainException($"slug '{slug}' must be kebab-case.");
        return new Community(System.Guid.NewGuid(), nameAr, nameEn,
            descriptionAr ?? string.Empty, descriptionEn ?? string.Empty, slug, visibility, presentationJson);
    }

    public void UpdateContent(string nameAr, string nameEn, string descriptionAr, string descriptionEn, string? presentationJson)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        if (nameAr.Length > MaxNameLength || nameEn.Length > MaxNameLength)
            throw new DomainException($"Name exceeds {MaxNameLength} chars.");
        NameAr = nameAr;
        NameEn = nameEn;
        DescriptionAr = descriptionAr ?? string.Empty;
        DescriptionEn = descriptionEn ?? string.Empty;
        PresentationJson = presentationJson;
    }

    public void ChangeVisibility(CommunityVisibility visibility) => Visibility = visibility;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void IncrementMembers() => MemberCount++;

    public void DecrementMembers()
    {
        if (MemberCount > 0) MemberCount--;
    }

    public void IncrementPosts() => PostCount++;
    public void DecrementPosts() { if (PostCount > 0) PostCount--; }
    public void IncrementFollowers() => FollowerCount++;
    public void DecrementFollowers() { if (FollowerCount > 0) FollowerCount--; }

    /// <summary>
    /// Records that a user submitted a join request to this (private) community by raising
    /// <see cref="Events.CommunityJoinRequestedEvent"/>. The join-request entity is persisted by its
    /// repository; this emits the domain event so a bridge handler relays it to the Worker for
    /// moderator notifications. Pass the real persisted <paramref name="requestId"/>.
    /// </summary>
    public void RegisterJoinRequest(System.Guid requestId, System.Guid userId, ISystemClock clock)
    {
        RaiseDomainEvent(new Events.CommunityJoinRequestedEvent(
            requestId, Id, userId, clock.UtcNow));
    }
}
