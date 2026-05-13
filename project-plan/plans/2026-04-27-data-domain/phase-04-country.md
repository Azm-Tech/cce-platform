# Phase 04 — Country bounded context

> Parent: [`../2026-04-27-data-domain.md`](../2026-04-27-data-domain.md) · Spec: [`../../specs/2026-04-27-data-domain-design.md`](../../specs/2026-04-27-data-domain-design.md) §4.3

**Phase goal:** Land 4 Country entities under `CCE.Domain.Country/`: `Country` (aggregate root with ISO codes + KAPSARC snapshot pointer), `CountryProfile` (1:1 admin-managed profile), `CountryResourceRequest` (state-rep submission workflow), `CountryKapsarcSnapshot` (append-only KAPSARC index history). Pure domain layer.

**Tasks in this phase:** 6
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 03 closed (`9d06f46` is HEAD).
- 204 backend tests passing.

---

## Task 4.1: `Country` aggregate + ISO code invariants

**Files:**
- Create: `backend/src/CCE.Domain/Country/Country.cs`
- Create: `backend/tests/CCE.Domain.Tests/Country/CountryTests.cs`

**Rationale:** ISO 3166-1 alpha-3 (3-letter, e.g., "SAU") is the primary external identifier. Alpha-2 (e.g., "SA") is its sibling. Both are uppercase. `LatestKapsarcSnapshotId` is updated by `CountryKapsarcSnapshot.Append` (Task 4.4).

- [ ] **Step 1: Write the entity**

`backend/src/CCE.Domain/Country/Country.cs`:

```csharp
using System.Text.RegularExpressions;
using CCE.Domain.Common;

namespace CCE.Domain.Country;

/// <summary>
/// Country reference entity — primary identifier is ISO 3166-1 alpha-3 (e.g., "SAU").
/// Aggregate root for the country bounded context. Soft-deletable. <see cref="IsActive"/>
/// hides a country from public dropdowns without deleting historical references.
/// </summary>
[Audited]
public sealed class Country : AggregateRoot<System.Guid>, ISoftDeletable
{
    private static readonly Regex Alpha3Pattern = new("^[A-Z]{3}$", RegexOptions.Compiled);
    private static readonly Regex Alpha2Pattern = new("^[A-Z]{2}$", RegexOptions.Compiled);

    private Country(
        System.Guid id,
        string isoAlpha3,
        string isoAlpha2,
        string nameAr,
        string nameEn,
        string regionAr,
        string regionEn,
        string flagUrl) : base(id)
    {
        IsoAlpha3 = isoAlpha3;
        IsoAlpha2 = isoAlpha2;
        NameAr = nameAr;
        NameEn = nameEn;
        RegionAr = regionAr;
        RegionEn = regionEn;
        FlagUrl = flagUrl;
        IsActive = true;
    }

    public string IsoAlpha3 { get; private set; }
    public string IsoAlpha2 { get; private set; }
    public string NameAr { get; private set; }
    public string NameEn { get; private set; }
    public string RegionAr { get; private set; }
    public string RegionEn { get; private set; }
    public string FlagUrl { get; private set; }
    public System.Guid? LatestKapsarcSnapshotId { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public System.DateTimeOffset? DeletedOn { get; private set; }
    public System.Guid? DeletedById { get; private set; }

    public static Country Register(
        string isoAlpha3,
        string isoAlpha2,
        string nameAr,
        string nameEn,
        string regionAr,
        string regionEn,
        string flagUrl)
    {
        if (string.IsNullOrWhiteSpace(isoAlpha3) || !Alpha3Pattern.IsMatch(isoAlpha3))
        {
            throw new DomainException($"IsoAlpha3 '{isoAlpha3}' must be three uppercase letters.");
        }
        if (string.IsNullOrWhiteSpace(isoAlpha2) || !Alpha2Pattern.IsMatch(isoAlpha2))
        {
            throw new DomainException($"IsoAlpha2 '{isoAlpha2}' must be two uppercase letters.");
        }
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        if (string.IsNullOrWhiteSpace(regionAr)) throw new DomainException("RegionAr is required.");
        if (string.IsNullOrWhiteSpace(regionEn)) throw new DomainException("RegionEn is required.");
        if (string.IsNullOrWhiteSpace(flagUrl)
            || !flagUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("FlagUrl must be https://.");
        }
        return new Country(System.Guid.NewGuid(),
            isoAlpha3, isoAlpha2, nameAr, nameEn, regionAr, regionEn, flagUrl);
    }

    public void UpdateLatestKapsarcSnapshot(System.Guid snapshotId)
    {
        if (snapshotId == System.Guid.Empty)
        {
            throw new DomainException("SnapshotId is required.");
        }
        LatestKapsarcSnapshotId = snapshotId;
    }

    public void UpdateNames(string nameAr, string nameEn, string regionAr, string regionEn)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        if (string.IsNullOrWhiteSpace(regionAr)) throw new DomainException("RegionAr is required.");
        if (string.IsNullOrWhiteSpace(regionEn)) throw new DomainException("RegionEn is required.");
        NameAr = nameAr;
        NameEn = nameEn;
        RegionAr = regionAr;
        RegionEn = regionEn;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void SoftDelete(System.Guid deletedById, ISystemClock clock)
    {
        if (deletedById == System.Guid.Empty) throw new DomainException("DeletedById is required.");
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedById = deletedById;
        DeletedOn = clock.UtcNow;
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Country;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Country;

public class CountryTests
{
    private static Country.Country NewCountry() => Country.Country.Register(
        "SAU", "SA", "السعودية", "Saudi Arabia", "آسيا", "Asia",
        "https://flags.example/sa.svg");

    [Fact]
    public void Register_creates_active_country()
    {
        var c = NewCountry();
        c.IsoAlpha3.Should().Be("SAU");
        c.IsoAlpha2.Should().Be("SA");
        c.IsActive.Should().BeTrue();
        c.IsDeleted.Should().BeFalse();
        c.LatestKapsarcSnapshotId.Should().BeNull();
    }

    [Theory]
    [InlineData("sau")]   // lowercase
    [InlineData("SA")]    // 2-letter where 3 expected
    [InlineData("SAUD")]  // 4-letter
    public void Register_with_invalid_alpha3_throws(string bad)
    {
        var act = () => Country.Country.Register(bad, "SA", "ا", "x", "ا", "x", "https://x");
        act.Should().Throw<DomainException>().WithMessage("*IsoAlpha3*");
    }

    [Theory]
    [InlineData("sa")]
    [InlineData("SAU")]
    [InlineData("S")]
    public void Register_with_invalid_alpha2_throws(string bad)
    {
        var act = () => Country.Country.Register("SAU", bad, "ا", "x", "ا", "x", "https://x");
        act.Should().Throw<DomainException>().WithMessage("*IsoAlpha2*");
    }

    [Fact]
    public void FlagUrl_must_be_https()
    {
        var act = () => Country.Country.Register("SAU", "SA", "ا", "x", "ا", "x", "http://insecure");
        act.Should().Throw<DomainException>().WithMessage("*FlagUrl*");
    }

    [Fact]
    public void UpdateLatestKapsarcSnapshot_sets_pointer()
    {
        var c = NewCountry();
        var snap = System.Guid.NewGuid();
        c.UpdateLatestKapsarcSnapshot(snap);
        c.LatestKapsarcSnapshotId.Should().Be(snap);
    }

    [Fact]
    public void Deactivate_then_Activate_toggles()
    {
        var c = NewCountry();
        c.Deactivate();
        c.IsActive.Should().BeFalse();
        c.Activate();
        c.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_marks_deleted()
    {
        var c = NewCountry();
        c.SoftDelete(System.Guid.NewGuid(), new FakeSystemClock());
        c.IsDeleted.Should().BeTrue();
    }
}
```

- [ ] **Step 3: Run + commit**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~Country.CountryTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:    13` (1 register + 3 alpha3 theory + 3 alpha2 theory + 1 flag + 1 snapshot + 1 toggle + 1 delete = 11; FluentAssertions Theory rows count individually so check actual).

```bash
git add backend/src/CCE.Domain/Country/Country.cs backend/tests/CCE.Domain.Tests/Country/CountryTests.cs
git -c commit.gpgsign=false commit -m "feat(country): Country aggregate with ISO 3166 invariants + KAPSARC snapshot pointer"
```

---

## Task 4.2: `CountryProfile` entity (1:1 with Country)

**Files:**
- Create: `backend/src/CCE.Domain/Country/CountryProfile.cs`
- Create: `backend/tests/CCE.Domain.Tests/Country/CountryProfileTests.cs`

- [ ] **Step 1: Entity**

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Country;

/// <summary>
/// Admin-managed bilingual profile content for a <see cref="Country"/>. 1:1 — enforced by
/// unique index on <see cref="CountryId"/> in Phase 08. <see cref="RowVersion"/> for
/// optimistic concurrency on edit.
/// </summary>
[Audited]
public sealed class CountryProfile : Entity<System.Guid>
{
    private CountryProfile(
        System.Guid id,
        System.Guid countryId,
        string descriptionAr,
        string descriptionEn,
        string keyInitiativesAr,
        string keyInitiativesEn,
        string? contactInfoAr,
        string? contactInfoEn,
        System.Guid lastUpdatedById,
        System.DateTimeOffset lastUpdatedOn) : base(id)
    {
        CountryId = countryId;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        KeyInitiativesAr = keyInitiativesAr;
        KeyInitiativesEn = keyInitiativesEn;
        ContactInfoAr = contactInfoAr;
        ContactInfoEn = contactInfoEn;
        LastUpdatedById = lastUpdatedById;
        LastUpdatedOn = lastUpdatedOn;
    }

    public System.Guid CountryId { get; private set; }
    public string DescriptionAr { get; private set; }
    public string DescriptionEn { get; private set; }
    public string KeyInitiativesAr { get; private set; }
    public string KeyInitiativesEn { get; private set; }
    public string? ContactInfoAr { get; private set; }
    public string? ContactInfoEn { get; private set; }
    public System.Guid LastUpdatedById { get; private set; }
    public System.DateTimeOffset LastUpdatedOn { get; private set; }
    public byte[] RowVersion { get; private set; } = System.Array.Empty<byte>();

    public static CountryProfile Create(
        System.Guid countryId,
        string descriptionAr,
        string descriptionEn,
        string keyInitiativesAr,
        string keyInitiativesEn,
        string? contactInfoAr,
        string? contactInfoEn,
        System.Guid createdById,
        ISystemClock clock)
    {
        if (countryId == System.Guid.Empty) throw new DomainException("CountryId is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (string.IsNullOrWhiteSpace(keyInitiativesAr)) throw new DomainException("KeyInitiativesAr is required.");
        if (string.IsNullOrWhiteSpace(keyInitiativesEn)) throw new DomainException("KeyInitiativesEn is required.");
        if (createdById == System.Guid.Empty) throw new DomainException("CreatedById is required.");
        return new CountryProfile(
            id: System.Guid.NewGuid(),
            countryId: countryId,
            descriptionAr: descriptionAr,
            descriptionEn: descriptionEn,
            keyInitiativesAr: keyInitiativesAr,
            keyInitiativesEn: keyInitiativesEn,
            contactInfoAr: contactInfoAr,
            contactInfoEn: contactInfoEn,
            lastUpdatedById: createdById,
            lastUpdatedOn: clock.UtcNow);
    }

    public void Update(
        string descriptionAr,
        string descriptionEn,
        string keyInitiativesAr,
        string keyInitiativesEn,
        string? contactInfoAr,
        string? contactInfoEn,
        System.Guid updatedById,
        ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (string.IsNullOrWhiteSpace(keyInitiativesAr)) throw new DomainException("KeyInitiativesAr is required.");
        if (string.IsNullOrWhiteSpace(keyInitiativesEn)) throw new DomainException("KeyInitiativesEn is required.");
        if (updatedById == System.Guid.Empty) throw new DomainException("UpdatedById is required.");
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        KeyInitiativesAr = keyInitiativesAr;
        KeyInitiativesEn = keyInitiativesEn;
        ContactInfoAr = contactInfoAr;
        ContactInfoEn = contactInfoEn;
        LastUpdatedById = updatedById;
        LastUpdatedOn = clock.UtcNow;
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Country;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Country;

public class CountryProfileTests
{
    [Fact]
    public void Create_builds_profile()
    {
        var clock = new FakeSystemClock();
        var p = CountryProfile.Create(
            System.Guid.NewGuid(),
            "وصف", "Description",
            "مبادرات", "Initiatives",
            null, null,
            System.Guid.NewGuid(), clock);

        p.DescriptionAr.Should().Be("وصف");
        p.LastUpdatedOn.Should().Be(clock.UtcNow);
        p.RowVersion.Should().NotBeNull();
    }

    [Fact]
    public void Create_with_empty_countryId_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => CountryProfile.Create(
            System.Guid.Empty, "ا", "x", "ا", "x", null, null, System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>().WithMessage("*CountryId*");
    }

    [Fact]
    public void Update_advances_LastUpdatedOn()
    {
        var clock = new FakeSystemClock();
        var p = CountryProfile.Create(
            System.Guid.NewGuid(), "ا", "x", "ا", "x", null, null, System.Guid.NewGuid(), clock);
        clock.Advance(System.TimeSpan.FromHours(1));
        var updater = System.Guid.NewGuid();

        p.Update("ج", "new", "ج", "new", "info", "info-en", updater, clock);

        p.DescriptionAr.Should().Be("ج");
        p.LastUpdatedOn.Should().Be(clock.UtcNow);
        p.LastUpdatedById.Should().Be(updater);
        p.ContactInfoAr.Should().Be("info");
    }

    [Fact]
    public void Update_with_empty_required_throws()
    {
        var clock = new FakeSystemClock();
        var p = CountryProfile.Create(
            System.Guid.NewGuid(), "ا", "x", "ا", "x", null, null, System.Guid.NewGuid(), clock);

        var act = () => p.Update("", "x", "ا", "x", null, null, System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>();
    }
}
```

- [ ] **Step 3: Run + commit**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~CountryProfileTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:     4`.

```bash
git add backend/src/CCE.Domain/Country/CountryProfile.cs backend/tests/CCE.Domain.Tests/Country/CountryProfileTests.cs
git -c commit.gpgsign=false commit -m "feat(country): CountryProfile entity (1:1 with Country) with bilingual content + RowVersion (4 TDD tests)"
```

---

## Task 4.3: `CountryResourceRequest` aggregate + state machine

**Files:**
- Create: `backend/src/CCE.Domain/Country/CountryResourceRequestStatus.cs`
- Create: `backend/src/CCE.Domain/Country/Events/CountryResourceRequestApprovedEvent.cs`
- Create: `backend/src/CCE.Domain/Country/Events/CountryResourceRequestRejectedEvent.cs`
- Create: `backend/src/CCE.Domain/Country/CountryResourceRequest.cs`
- Create: `backend/tests/CCE.Domain.Tests/Country/CountryResourceRequestTests.cs`

- [ ] **Step 1: Status enum**

```csharp
namespace CCE.Domain.Country;

public enum CountryResourceRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}
```

- [ ] **Step 2: Domain events**

`Events/CountryResourceRequestApprovedEvent.cs`:

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Country.Events;

public sealed record CountryResourceRequestApprovedEvent(
    System.Guid RequestId,
    System.Guid CountryId,
    System.Guid RequestedById,
    System.Guid ApprovedById,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
```

`Events/CountryResourceRequestRejectedEvent.cs`:

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Country.Events;

public sealed record CountryResourceRequestRejectedEvent(
    System.Guid RequestId,
    System.Guid CountryId,
    System.Guid RequestedById,
    System.Guid RejectedById,
    string AdminNotesAr,
    string AdminNotesEn,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
```

- [ ] **Step 3: Aggregate**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Country.Events;

namespace CCE.Domain.Country;

/// <summary>
/// State-rep submission asking the center to publish a country-scoped resource. State machine:
/// <c>Pending → Approved</c> or <c>Pending → Rejected</c> (terminal). Approving raises
/// <see cref="CountryResourceRequestApprovedEvent"/> which Phase 07 routes to a handler that
/// creates the actual <c>Resource</c>.
/// </summary>
[Audited]
public sealed class CountryResourceRequest : AggregateRoot<System.Guid>, ISoftDeletable
{
    private CountryResourceRequest(
        System.Guid id,
        System.Guid countryId,
        System.Guid requestedById,
        string proposedTitleAr,
        string proposedTitleEn,
        string proposedDescriptionAr,
        string proposedDescriptionEn,
        ResourceType proposedResourceType,
        System.Guid proposedAssetFileId,
        System.DateTimeOffset submittedOn) : base(id)
    {
        CountryId = countryId;
        RequestedById = requestedById;
        ProposedTitleAr = proposedTitleAr;
        ProposedTitleEn = proposedTitleEn;
        ProposedDescriptionAr = proposedDescriptionAr;
        ProposedDescriptionEn = proposedDescriptionEn;
        ProposedResourceType = proposedResourceType;
        ProposedAssetFileId = proposedAssetFileId;
        SubmittedOn = submittedOn;
        Status = CountryResourceRequestStatus.Pending;
    }

    public System.Guid CountryId { get; private set; }
    public System.Guid RequestedById { get; private set; }
    public CountryResourceRequestStatus Status { get; private set; }
    public string ProposedTitleAr { get; private set; }
    public string ProposedTitleEn { get; private set; }
    public string ProposedDescriptionAr { get; private set; }
    public string ProposedDescriptionEn { get; private set; }
    public ResourceType ProposedResourceType { get; private set; }
    public System.Guid ProposedAssetFileId { get; private set; }
    public System.DateTimeOffset SubmittedOn { get; private set; }
    public string? AdminNotesAr { get; private set; }
    public string? AdminNotesEn { get; private set; }
    public System.Guid? ProcessedById { get; private set; }
    public System.DateTimeOffset? ProcessedOn { get; private set; }
    public bool IsDeleted { get; private set; }
    public System.DateTimeOffset? DeletedOn { get; private set; }
    public System.Guid? DeletedById { get; private set; }

    public static CountryResourceRequest Submit(
        System.Guid countryId,
        System.Guid requestedById,
        string titleAr,
        string titleEn,
        string descriptionAr,
        string descriptionEn,
        ResourceType resourceType,
        System.Guid assetFileId,
        ISystemClock clock)
    {
        if (countryId == System.Guid.Empty) throw new DomainException("CountryId is required.");
        if (requestedById == System.Guid.Empty) throw new DomainException("RequestedById is required.");
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (assetFileId == System.Guid.Empty) throw new DomainException("AssetFileId is required.");
        return new CountryResourceRequest(
            System.Guid.NewGuid(), countryId, requestedById,
            titleAr, titleEn, descriptionAr, descriptionEn,
            resourceType, assetFileId, clock.UtcNow);
    }

    public void Approve(System.Guid approvedById, string? notesAr, string? notesEn, ISystemClock clock)
    {
        if (Status != CountryResourceRequestStatus.Pending)
        {
            throw new DomainException($"Cannot approve a {Status} request — only Pending allowed.");
        }
        if (approvedById == System.Guid.Empty) throw new DomainException("ApprovedById is required.");
        var now = clock.UtcNow;
        Status = CountryResourceRequestStatus.Approved;
        ProcessedById = approvedById;
        ProcessedOn = now;
        AdminNotesAr = notesAr;
        AdminNotesEn = notesEn;
        RaiseDomainEvent(new CountryResourceRequestApprovedEvent(
            Id, CountryId, RequestedById, approvedById, now));
    }

    public void Reject(System.Guid rejectedById, string notesAr, string notesEn, ISystemClock clock)
    {
        if (Status != CountryResourceRequestStatus.Pending)
        {
            throw new DomainException($"Cannot reject a {Status} request — only Pending allowed.");
        }
        if (rejectedById == System.Guid.Empty) throw new DomainException("RejectedById is required.");
        if (string.IsNullOrWhiteSpace(notesAr)) throw new DomainException("Arabic admin notes are required to reject.");
        if (string.IsNullOrWhiteSpace(notesEn)) throw new DomainException("English admin notes are required to reject.");
        var now = clock.UtcNow;
        Status = CountryResourceRequestStatus.Rejected;
        ProcessedById = rejectedById;
        ProcessedOn = now;
        AdminNotesAr = notesAr;
        AdminNotesEn = notesEn;
        RaiseDomainEvent(new CountryResourceRequestRejectedEvent(
            Id, CountryId, RequestedById, rejectedById, notesAr, notesEn, now));
    }
}
```

- [ ] **Step 4: Tests**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Country;
using CCE.Domain.Country.Events;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Country;

public class CountryResourceRequestTests
{
    private static FakeSystemClock NewClock() => new();

    private static CountryResourceRequest NewPending(FakeSystemClock clock) =>
        CountryResourceRequest.Submit(
            countryId: System.Guid.NewGuid(),
            requestedById: System.Guid.NewGuid(),
            titleAr: "عنوان", titleEn: "Title",
            descriptionAr: "وصف", descriptionEn: "Description",
            resourceType: ResourceType.Pdf,
            assetFileId: System.Guid.NewGuid(),
            clock: clock);

    [Fact]
    public void Submit_creates_pending_request()
    {
        var clock = NewClock();
        var r = NewPending(clock);

        r.Status.Should().Be(CountryResourceRequestStatus.Pending);
        r.SubmittedOn.Should().Be(clock.UtcNow);
        r.AdminNotesAr.Should().BeNull();
        r.ProcessedOn.Should().BeNull();
    }

    [Fact]
    public void Approve_transitions_to_Approved_and_raises_event()
    {
        var clock = NewClock();
        var r = NewPending(clock);
        var admin = System.Guid.NewGuid();
        clock.Advance(System.TimeSpan.FromHours(1));

        r.Approve(admin, "ملاحظة", "Note", clock);

        r.Status.Should().Be(CountryResourceRequestStatus.Approved);
        r.ProcessedById.Should().Be(admin);
        r.ProcessedOn.Should().Be(clock.UtcNow);
        r.AdminNotesAr.Should().Be("ملاحظة");
        r.DomainEvents.OfType<CountryResourceRequestApprovedEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void Reject_requires_admin_notes_in_both_locales()
    {
        var clock = NewClock();
        var r = NewPending(clock);
        var act = () => r.Reject(System.Guid.NewGuid(), "", "Note", clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reject_transitions_to_Rejected_and_raises_event()
    {
        var clock = NewClock();
        var r = NewPending(clock);
        var admin = System.Guid.NewGuid();

        r.Reject(admin, "سبب", "Reason", clock);

        r.Status.Should().Be(CountryResourceRequestStatus.Rejected);
        r.AdminNotesAr.Should().Be("سبب");
        r.DomainEvents.OfType<CountryResourceRequestRejectedEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void Approving_already_processed_throws()
    {
        var clock = NewClock();
        var r = NewPending(clock);
        r.Approve(System.Guid.NewGuid(), null, null, clock);
        var act = () => r.Approve(System.Guid.NewGuid(), null, null, clock);
        act.Should().Throw<DomainException>().WithMessage("*Pending*");
    }

    [Fact]
    public void Rejecting_after_approval_throws()
    {
        var clock = NewClock();
        var r = NewPending(clock);
        r.Approve(System.Guid.NewGuid(), null, null, clock);
        var act = () => r.Reject(System.Guid.NewGuid(), "ا", "a", clock);
        act.Should().Throw<DomainException>();
    }
}
```

- [ ] **Step 5: Run + commit**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~CountryResourceRequestTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:     6`.

```bash
git add backend/src/CCE.Domain/Country/CountryResourceRequestStatus.cs backend/src/CCE.Domain/Country/Events/ backend/src/CCE.Domain/Country/CountryResourceRequest.cs backend/tests/CCE.Domain.Tests/Country/CountryResourceRequestTests.cs
git -c commit.gpgsign=false commit -m "feat(country): CountryResourceRequest aggregate + state machine + 2 events (6 TDD tests)"
```

---

## Task 4.4: `CountryKapsarcSnapshot` (append-only)

**Files:**
- Create: `backend/src/CCE.Domain/Country/CountryKapsarcSnapshot.cs`
- Create: `backend/tests/CCE.Domain.Tests/Country/CountryKapsarcSnapshotTests.cs`

**Rationale:** Append-only KAPSARC index reading. Each snapshot is immutable once captured; the `Country.LatestKapsarcSnapshotId` pointer caches the most recent. NOT marked `[Audited]` per spec §4.11 (high-volume time-series).

- [ ] **Step 1: Entity**

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Country;

/// <summary>
/// One reading of the KAPSARC Circular Carbon Economy index for a country at a point in time.
/// Append-only by convention — once captured, fields are immutable. The pointer
/// <c>Country.LatestKapsarcSnapshotId</c> caches the most recent snapshot per country.
/// NOT audited (high-volume time-series; spec §4.11).
/// </summary>
public sealed class CountryKapsarcSnapshot : Entity<System.Guid>
{
    private CountryKapsarcSnapshot(
        System.Guid id,
        System.Guid countryId,
        string classification,
        decimal performanceScore,
        decimal totalIndex,
        System.DateTimeOffset snapshotTakenOn,
        string? sourceVersion) : base(id)
    {
        CountryId = countryId;
        Classification = classification;
        PerformanceScore = performanceScore;
        TotalIndex = totalIndex;
        SnapshotTakenOn = snapshotTakenOn;
        SourceVersion = sourceVersion;
    }

    public System.Guid CountryId { get; private set; }
    public string Classification { get; private set; }
    public decimal PerformanceScore { get; private set; }
    public decimal TotalIndex { get; private set; }
    public System.DateTimeOffset SnapshotTakenOn { get; private set; }
    public string? SourceVersion { get; private set; }

    public static CountryKapsarcSnapshot Capture(
        System.Guid countryId,
        string classification,
        decimal performanceScore,
        decimal totalIndex,
        ISystemClock clock,
        string? sourceVersion = null)
    {
        if (countryId == System.Guid.Empty) throw new DomainException("CountryId is required.");
        if (string.IsNullOrWhiteSpace(classification)) throw new DomainException("Classification is required.");
        if (performanceScore < 0 || performanceScore > 100)
        {
            throw new DomainException("PerformanceScore must be between 0 and 100.");
        }
        if (totalIndex < 0 || totalIndex > 100)
        {
            throw new DomainException("TotalIndex must be between 0 and 100.");
        }
        return new CountryKapsarcSnapshot(
            System.Guid.NewGuid(), countryId, classification,
            performanceScore, totalIndex, clock.UtcNow, sourceVersion);
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Country;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Country;

public class CountryKapsarcSnapshotTests
{
    [Fact]
    public void Capture_creates_snapshot()
    {
        var clock = new FakeSystemClock();
        var s = CountryKapsarcSnapshot.Capture(
            System.Guid.NewGuid(), "Leader", 87.5m, 92.0m, clock, "v2.4");

        s.Classification.Should().Be("Leader");
        s.PerformanceScore.Should().Be(87.5m);
        s.TotalIndex.Should().Be(92.0m);
        s.SnapshotTakenOn.Should().Be(clock.UtcNow);
        s.SourceVersion.Should().Be("v2.4");
    }

    [Fact]
    public void Capture_with_negative_score_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => CountryKapsarcSnapshot.Capture(
            System.Guid.NewGuid(), "x", -1m, 50m, clock);
        act.Should().Throw<DomainException>().WithMessage("*PerformanceScore*");
    }

    [Fact]
    public void Capture_with_score_above_100_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => CountryKapsarcSnapshot.Capture(
            System.Guid.NewGuid(), "x", 100.1m, 50m, clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Capture_with_empty_classification_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => CountryKapsarcSnapshot.Capture(
            System.Guid.NewGuid(), "", 50m, 50m, clock);
        act.Should().Throw<DomainException>().WithMessage("*Classification*");
    }

    [Fact]
    public void Snapshot_is_NOT_audited()
    {
        var attrs = typeof(CountryKapsarcSnapshot).GetCustomAttributes(typeof(AuditedAttribute), inherit: false);
        attrs.Should().BeEmpty(because: "high-volume time-series — spec §4.11 excludes from audit");
    }
}
```

- [ ] **Step 3: Run + commit**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~CountryKapsarcSnapshotTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:     5`.

```bash
git add backend/src/CCE.Domain/Country/CountryKapsarcSnapshot.cs backend/tests/CCE.Domain.Tests/Country/CountryKapsarcSnapshotTests.cs
git -c commit.gpgsign=false commit -m "feat(country): CountryKapsarcSnapshot (append-only, non-audited per §4.11) (5 TDD tests)"
```

---

## Task 4.5: Country [Audited] coverage test

**Files:**
- Create: `backend/tests/CCE.Domain.Tests/Country/AuditedCoverageTests.cs`

```csharp
using CCE.Domain.Common;
using CCE.Domain.Country;

namespace CCE.Domain.Tests.Country;

public class AuditedCoverageTests
{
    [Theory]
    [InlineData(typeof(Country.Country))]
    [InlineData(typeof(CountryProfile))]
    [InlineData(typeof(CountryResourceRequest))]
    public void Country_aggregate_or_profile_carries_AuditedAttribute(System.Type type)
    {
        var attrs = type.GetCustomAttributes(typeof(AuditedAttribute), inherit: false);
        attrs.Should().HaveCount(1, because: $"{type.Name} must be marked [Audited] (spec §4.11)");
    }
}
```

- [ ] **Step 1: Run + commit**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~Country.AuditedCoverageTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:     3`.

```bash
git add backend/tests/CCE.Domain.Tests/Country/AuditedCoverageTests.cs
git -c commit.gpgsign=false commit -m "test(country): [Audited] coverage on Country/CountryProfile/CountryResourceRequest (3 tests)"
```

---

## Task 4.6: Phase 04 close

- [ ] **Step 1: Full backend run**

```bash
dotnet test backend/CCE.sln --nologo --no-restore --logger "console;verbosity=minimal" 2>&1 | grep -E "(Passed!|Failed!)"
```

- [ ] **Step 2: Update progress doc**

Mark Phase 04 ✅ Done. Use the actual numbers reported.

- [ ] **Step 3: Commit**

```bash
git add docs/subprojects/02-data-domain-progress.md
git -c commit.gpgsign=false commit -m "docs(sub-2): mark Phase 04 done; Country bounded context shipped"
```

---

## Phase 04 — completion checklist

- [ ] 4 Country entities exist under `backend/src/CCE.Domain/Country/`.
- [ ] `Country` ISO alpha-3/alpha-2 invariants.
- [ ] `CountryProfile` with `RowVersion`.
- [ ] `CountryResourceRequest` state machine (Pending→Approved/Rejected) + 2 events.
- [ ] `CountryKapsarcSnapshot` append-only, NOT audited.
- [ ] All Phase 03 regression tests still pass.
- [ ] 6 new commits.

**If all boxes ticked, Phase 04 is complete. Proceed to Phase 05 (Community — 7 entities).**
