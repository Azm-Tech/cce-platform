using CCE.Domain.Common;

namespace CCE.Domain.Identity;

/// <summary>
/// Approved community-expert profile. Created exclusively via
/// <see cref="CreateFromApprovedRequest"/> from an approved
/// <see cref="ExpertRegistrationRequest"/>. The 1:1 link to <c>User</c> is
/// captured by <see cref="UserId"/> and enforced by a unique index in Phase 08.
/// </summary>
[Audited]
public sealed class ExpertProfile : Entity<System.Guid>, ISoftDeletable
{
    private ExpertProfile(
        System.Guid id,
        System.Guid userId,
        string bioAr,
        string bioEn,
        IList<string> expertiseTags,
        string academicTitleAr,
        string academicTitleEn,
        System.DateTimeOffset approvedOn,
        System.Guid approvedById) : base(id)
    {
        UserId = userId;
        BioAr = bioAr;
        BioEn = bioEn;
        ExpertiseTags = expertiseTags;
        AcademicTitleAr = academicTitleAr;
        AcademicTitleEn = academicTitleEn;
        ApprovedOn = approvedOn;
        ApprovedById = approvedById;
    }

    public System.Guid UserId { get; private set; }

    public string BioAr { get; private set; } = string.Empty;

    public string BioEn { get; private set; } = string.Empty;

    public IList<string> ExpertiseTags { get; private set; } = new List<string>();

    public string AcademicTitleAr { get; private set; } = string.Empty;

    public string AcademicTitleEn { get; private set; } = string.Empty;

    public System.DateTimeOffset ApprovedOn { get; private set; }

    public System.Guid ApprovedById { get; private set; }

    public bool IsDeleted { get; private set; }

    public System.DateTimeOffset? DeletedOn { get; private set; }

    public System.Guid? DeletedById { get; private set; }

    /// <summary>
    /// Factory: build an <see cref="ExpertProfile"/> from an
    /// <see cref="ExpertRegistrationRequest"/> that is in
    /// <see cref="ExpertRegistrationStatus.Approved"/>. Throws otherwise.
    /// </summary>
    public static ExpertProfile CreateFromApprovedRequest(
        ExpertRegistrationRequest request,
        string academicTitleAr,
        string academicTitleEn,
        ISystemClock clock)
    {
        if (request is null)
        {
            throw new DomainException("Request is required.");
        }
        if (request.Status != ExpertRegistrationStatus.Approved)
        {
            throw new DomainException($"Cannot create profile from a {request.Status} request — must be Approved.");
        }
        if (request.ProcessedById is null || request.ProcessedOn is null)
        {
            throw new DomainException("Approved request is missing processor metadata.");
        }
        return new ExpertProfile(
            id: System.Guid.NewGuid(),
            userId: request.RequestedById,
            bioAr: request.RequestedBioAr,
            bioEn: request.RequestedBioEn,
            expertiseTags: request.RequestedTags,
            academicTitleAr: academicTitleAr,
            academicTitleEn: academicTitleEn,
            approvedOn: request.ProcessedOn.Value,
            approvedById: request.ProcessedById.Value);
    }

    public void UpdateBio(string bioAr, string bioEn)
    {
        if (string.IsNullOrWhiteSpace(bioAr))
        {
            throw new DomainException("Arabic bio is required.");
        }
        if (string.IsNullOrWhiteSpace(bioEn))
        {
            throw new DomainException("English bio is required.");
        }
        BioAr = bioAr;
        BioEn = bioEn;
    }

    public void UpdateExpertiseTags(IEnumerable<string> tags)
    {
        if (tags is null)
        {
            throw new DomainException("Tags collection is required.");
        }
        ExpertiseTags = tags
            .Select(static s => s?.Trim() ?? string.Empty)
            .Where(static s => s.Length > 0)
            .Distinct()
            .ToList();
    }

    public void UpdateAcademicTitle(string titleAr, string titleEn)
    {
        AcademicTitleAr = titleAr ?? string.Empty;
        AcademicTitleEn = titleEn ?? string.Empty;
    }
}
