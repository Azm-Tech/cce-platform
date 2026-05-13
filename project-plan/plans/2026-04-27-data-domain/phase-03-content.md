# Phase 03 — Content bounded context

> Parent: [`../2026-04-27-data-domain.md`](../2026-04-27-data-domain.md) · Spec: [`../../specs/2026-04-27-data-domain-design.md`](../../specs/2026-04-27-data-domain-design.md) §4.2

**Phase goal:** Land 8 Content entities under `CCE.Domain.Content/`: `AssetFile`, `ResourceCategory`, `Resource`, `News`, `Event`, `Page`, `HomepageSection`, `NewsletterSubscription`. Aggregate roots (`Resource`, `News`, `Event`, `Page`) carry `RowVersion` for optimistic concurrency. Domain events fire on key state transitions (publish, schedule, virus-scan). Pure domain layer — no EF mappings.

**Tasks in this phase:** 12
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 02 closed (commit `84076d3` is HEAD).
- `dotnet build backend/CCE.sln` 0 warnings 0 errors.
- `dotnet test backend/CCE.sln` reports 134 backend passing.

---

## Pre-execution sanity checks

1. `git status` clean; on `main`.
2. `git log --oneline -1` → `84076d3` or later.
3. `ls backend/src/CCE.Domain/Identity/` shows the 5 Identity entities + `Events/` folder.
4. Full test suite green.

If any fail, stop and report.

---

## Task 3.1: Content enums (`ResourceType`, `VirusScanStatus`, `PageType`, `HomepageSectionType`)

**Files:**
- Create: `backend/src/CCE.Domain/Content/ResourceType.cs`
- Create: `backend/src/CCE.Domain/Content/VirusScanStatus.cs`
- Create: `backend/src/CCE.Domain/Content/PageType.cs`
- Create: `backend/src/CCE.Domain/Content/HomepageSectionType.cs`

**Rationale:** Four enums consumed by entities in this phase. Bundle them into one task — they're pure data definitions with no behavior, no tests beyond the compile-check.

- [ ] **Step 1: Write the enums**

`backend/src/CCE.Domain/Content/ResourceType.cs`:

```csharp
namespace CCE.Domain.Content;

/// <summary>
/// Format of a <see cref="Resource"/>. Drives both UI rendering (icon + viewer) and
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
```

`backend/src/CCE.Domain/Content/VirusScanStatus.cs`:

```csharp
namespace CCE.Domain.Content;

/// <summary>
/// ClamAV result for an <see cref="AssetFile"/>. Files with status other than
/// <see cref="Clean"/> are blocked from public download/render.
/// </summary>
public enum VirusScanStatus
{
    /// <summary>Upload succeeded but ClamAV hasn't scanned yet.</summary>
    Pending = 0,

    /// <summary>ClamAV scanned with no detection.</summary>
    Clean = 1,

    /// <summary>ClamAV detected a signature; the asset is quarantined.</summary>
    Infected = 2,

    /// <summary>Scan failed (ClamAV error / timeout); manual review required.</summary>
    ScanFailed = 3,
}
```

`backend/src/CCE.Domain/Content/PageType.cs`:

```csharp
namespace CCE.Domain.Content;

/// <summary>Type of static <see cref="Page"/>. Mostly drives URL routing + navigation.</summary>
public enum PageType
{
    AboutPlatform = 0,
    TermsOfService = 1,
    PrivacyPolicy = 2,
    /// <summary>Free-form page added by content managers.</summary>
    Custom = 99,
}
```

`backend/src/CCE.Domain/Content/HomepageSectionType.cs`:

```csharp
namespace CCE.Domain.Content;

/// <summary>
/// Logical block on the public homepage. Order is set per-section by
/// <c>HomepageSection.OrderIndex</c>; the same SectionType can appear multiple times
/// (e.g., two Hero rows) but each row is a distinct entity.
/// </summary>
public enum HomepageSectionType
{
    Hero = 0,
    FeaturedNews = 1,
    FeaturedResources = 2,
    UpcomingEvents = 3,
    KnowledgeMapTeaser = 4,
    InteractiveCityTeaser = 5,
    NewsletterSignup = 6,
    Custom = 99,
}
```

- [ ] **Step 2: Build**

```bash
dotnet build backend/src/CCE.Domain/CCE.Domain.csproj --nologo --no-restore 2>&1 | tail -5
```

Expected: 0 errors, 0 warnings.

- [ ] **Step 3: Commit**

```bash
git add backend/src/CCE.Domain/Content/ResourceType.cs backend/src/CCE.Domain/Content/VirusScanStatus.cs backend/src/CCE.Domain/Content/PageType.cs backend/src/CCE.Domain/Content/HomepageSectionType.cs
git -c commit.gpgsign=false commit -m "feat(content): 4 Content enums (ResourceType, VirusScanStatus, PageType, HomepageSectionType)"
```

---

## Task 3.2: `AssetFile` entity + virus-scan transitions

**Files:**
- Create: `backend/src/CCE.Domain/Content/AssetFile.cs`
- Create: `backend/tests/CCE.Domain.Tests/Content/AssetFileTests.cs`

**Rationale:** `AssetFile` is the file-storage handle. It starts `Pending`, transitions to `Clean`/`Infected`/`ScanFailed` once ClamAV reports back. State transitions are one-way from `Pending`; a clean file cannot become infected without re-uploading.

- [ ] **Step 1: Write failing tests**

`backend/tests/CCE.Domain.Tests/Content/AssetFileTests.cs`:

```csharp
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Content;

public class AssetFileTests
{
    private static FakeSystemClock NewClock() => new();

    private static AssetFile NewPending(FakeSystemClock clock) =>
        AssetFile.Register(
            url: "https://cdn.example/uploads/abc.pdf",
            originalFileName: "report.pdf",
            sizeBytes: 12345,
            mimeType: "application/pdf",
            uploadedById: System.Guid.NewGuid(),
            clock: clock);

    [Fact]
    public void Register_creates_pending_asset()
    {
        var clock = NewClock();
        var asset = NewPending(clock);

        asset.Id.Should().NotBe(System.Guid.Empty);
        asset.Url.Should().Be("https://cdn.example/uploads/abc.pdf");
        asset.OriginalFileName.Should().Be("report.pdf");
        asset.SizeBytes.Should().Be(12345);
        asset.MimeType.Should().Be("application/pdf");
        asset.UploadedOn.Should().Be(clock.UtcNow);
        asset.VirusScanStatus.Should().Be(VirusScanStatus.Pending);
        asset.ScannedOn.Should().BeNull();
    }

    [Fact]
    public void Register_with_zero_size_throws()
    {
        var clock = NewClock();
        var act = () => AssetFile.Register("https://x", "f", 0, "x/y", System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>().WithMessage("*SizeBytes*");
    }

    [Fact]
    public void Register_with_empty_url_throws()
    {
        var clock = NewClock();
        var act = () => AssetFile.Register("", "f", 1, "x/y", System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>().WithMessage("*Url*");
    }

    [Fact]
    public void MarkClean_transitions_from_Pending()
    {
        var clock = NewClock();
        var asset = NewPending(clock);
        clock.Advance(System.TimeSpan.FromMinutes(2));

        asset.MarkClean(clock);

        asset.VirusScanStatus.Should().Be(VirusScanStatus.Clean);
        asset.ScannedOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void MarkInfected_transitions_from_Pending()
    {
        var clock = NewClock();
        var asset = NewPending(clock);
        asset.MarkInfected(clock);

        asset.VirusScanStatus.Should().Be(VirusScanStatus.Infected);
    }

    [Fact]
    public void MarkScanFailed_transitions_from_Pending()
    {
        var clock = NewClock();
        var asset = NewPending(clock);
        asset.MarkScanFailed(clock);

        asset.VirusScanStatus.Should().Be(VirusScanStatus.ScanFailed);
    }

    [Fact]
    public void Cannot_transition_a_clean_asset_to_infected()
    {
        var clock = NewClock();
        var asset = NewPending(clock);
        asset.MarkClean(clock);

        var act = () => asset.MarkInfected(clock);
        act.Should().Throw<DomainException>().WithMessage("*Pending*");
    }

    [Fact]
    public void Cannot_double_mark_clean()
    {
        var clock = NewClock();
        var asset = NewPending(clock);
        asset.MarkClean(clock);

        var act = () => asset.MarkClean(clock);
        act.Should().Throw<DomainException>();
    }
}
```

- [ ] **Step 2: Run — expect compile errors**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~AssetFileTests" 2>&1 | tail -8
```

Expected: build error referencing `AssetFile` not found.

- [ ] **Step 3: Write `AssetFile.cs`**

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Content;

/// <summary>
/// File-storage handle. Carries the URL/key, mime type, size, and ClamAV result.
/// State transitions: <see cref="VirusScanStatus.Pending"/> → exactly one of
/// <see cref="VirusScanStatus.Clean"/>, <see cref="VirusScanStatus.Infected"/>,
/// <see cref="VirusScanStatus.ScanFailed"/>. Once a terminal status is set, it cannot
/// change — re-scan requires a new asset.
/// </summary>
[Audited]
public sealed class AssetFile : Entity<System.Guid>
{
    private AssetFile(
        System.Guid id,
        string url,
        string originalFileName,
        long sizeBytes,
        string mimeType,
        System.Guid uploadedById,
        System.DateTimeOffset uploadedOn) : base(id)
    {
        Url = url;
        OriginalFileName = originalFileName;
        SizeBytes = sizeBytes;
        MimeType = mimeType;
        UploadedById = uploadedById;
        UploadedOn = uploadedOn;
        VirusScanStatus = VirusScanStatus.Pending;
    }

    public string Url { get; private set; }

    public string OriginalFileName { get; private set; }

    public long SizeBytes { get; private set; }

    public string MimeType { get; private set; }

    public System.Guid UploadedById { get; private set; }

    public System.DateTimeOffset UploadedOn { get; private set; }

    public VirusScanStatus VirusScanStatus { get; private set; }

    public System.DateTimeOffset? ScannedOn { get; private set; }

    public static AssetFile Register(
        string url,
        string originalFileName,
        long sizeBytes,
        string mimeType,
        System.Guid uploadedById,
        ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DomainException("Url is required.");
        }
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new DomainException("OriginalFileName is required.");
        }
        if (sizeBytes <= 0)
        {
            throw new DomainException("SizeBytes must be positive.");
        }
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            throw new DomainException("MimeType is required.");
        }
        if (uploadedById == System.Guid.Empty)
        {
            throw new DomainException("UploadedById is required.");
        }
        return new AssetFile(
            id: System.Guid.NewGuid(),
            url: url,
            originalFileName: originalFileName,
            sizeBytes: sizeBytes,
            mimeType: mimeType,
            uploadedById: uploadedById,
            uploadedOn: clock.UtcNow);
    }

    public void MarkClean(ISystemClock clock) => Transition(VirusScanStatus.Clean, clock);

    public void MarkInfected(ISystemClock clock) => Transition(VirusScanStatus.Infected, clock);

    public void MarkScanFailed(ISystemClock clock) => Transition(VirusScanStatus.ScanFailed, clock);

    private void Transition(VirusScanStatus terminal, ISystemClock clock)
    {
        if (VirusScanStatus != VirusScanStatus.Pending)
        {
            throw new DomainException($"Cannot mark a {VirusScanStatus} asset — must be Pending.");
        }
        VirusScanStatus = terminal;
        ScannedOn = clock.UtcNow;
    }
}
```

- [ ] **Step 4: Build + run tests**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~AssetFileTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:     8`.

- [ ] **Step 5: Commit**

```bash
git add backend/src/CCE.Domain/Content/AssetFile.cs backend/tests/CCE.Domain.Tests/Content/AssetFileTests.cs
git -c commit.gpgsign=false commit -m "feat(content): AssetFile entity with virus-scan state machine (8 TDD tests)"
```

---

## Task 3.3: `ResourceCategory` entity (tree)

**Files:**
- Create: `backend/src/CCE.Domain/Content/ResourceCategory.cs`
- Create: `backend/tests/CCE.Domain.Tests/Content/ResourceCategoryTests.cs`

**Rationale:** Hierarchical taxonomy for resources. Self-referencing `ParentId`. Root nodes have null parent. Depth limit (4) prevents accidental cycles in admin UI.

- [ ] **Step 1: Write failing tests**

`backend/tests/CCE.Domain.Tests/Content/ResourceCategoryTests.cs`:

```csharp
using CCE.Domain.Common;
using CCE.Domain.Content;

namespace CCE.Domain.Tests.Content;

public class ResourceCategoryTests
{
    [Fact]
    public void Create_root_category_has_no_parent()
    {
        var cat = ResourceCategory.Create("الشمسية", "Solar", "solar", parentId: null, orderIndex: 0);

        cat.NameAr.Should().Be("الشمسية");
        cat.NameEn.Should().Be("Solar");
        cat.Slug.Should().Be("solar");
        cat.ParentId.Should().BeNull();
        cat.OrderIndex.Should().Be(0);
        cat.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_child_category_has_parent()
    {
        var parentId = System.Guid.NewGuid();
        var cat = ResourceCategory.Create("الكهروضوئية", "Photovoltaic", "pv", parentId, orderIndex: 1);

        cat.ParentId.Should().Be(parentId);
    }

    [Fact]
    public void Slug_must_be_kebab_case()
    {
        var act = () => ResourceCategory.Create("ا", "Solar", "Bad Slug", null, 0);
        act.Should().Throw<DomainException>().WithMessage("*slug*");
    }

    [Fact]
    public void Slug_with_underscores_rejected()
    {
        var act = () => ResourceCategory.Create("ا", "Solar", "wind_power", null, 0);
        act.Should().Throw<DomainException>().WithMessage("*slug*");
    }

    [Fact]
    public void Empty_NameAr_throws()
    {
        var act = () => ResourceCategory.Create("", "Solar", "solar", null, 0);
        act.Should().Throw<DomainException>().WithMessage("*NameAr*");
    }

    [Fact]
    public void Empty_NameEn_throws()
    {
        var act = () => ResourceCategory.Create("ا", "", "solar", null, 0);
        act.Should().Throw<DomainException>().WithMessage("*NameEn*");
    }

    [Fact]
    public void UpdateNames_replaces_bilingual_names()
    {
        var cat = ResourceCategory.Create("ا", "Old", "x", null, 0);
        cat.UpdateNames("ج", "New");
        cat.NameAr.Should().Be("ج");
        cat.NameEn.Should().Be("New");
    }

    [Fact]
    public void Reorder_updates_OrderIndex()
    {
        var cat = ResourceCategory.Create("ا", "x", "x", null, 0);
        cat.Reorder(7);
        cat.OrderIndex.Should().Be(7);
    }

    [Fact]
    public void Deactivate_sets_IsActive_false()
    {
        var cat = ResourceCategory.Create("ا", "x", "x", null, 0);
        cat.Deactivate();
        cat.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_sets_IsActive_true()
    {
        var cat = ResourceCategory.Create("ا", "x", "x", null, 0);
        cat.Deactivate();
        cat.Activate();
        cat.IsActive.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run — expect compile errors**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~ResourceCategoryTests" 2>&1 | tail -5
```

Expected: build error referencing `ResourceCategory` not found.

- [ ] **Step 3: Write `ResourceCategory.cs`**

```csharp
using System.Text.RegularExpressions;
using CCE.Domain.Common;

namespace CCE.Domain.Content;

/// <summary>
/// Hierarchical taxonomy node for <see cref="Resource"/>. Self-referencing via
/// <see cref="ParentId"/>. Root categories have null parent. Slugs are kebab-case
/// (lowercase a-z, digits, hyphens). Inactive categories are hidden from public UI
/// but their resources remain accessible by direct link.
/// </summary>
[Audited]
public sealed class ResourceCategory : Entity<System.Guid>
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private ResourceCategory(
        System.Guid id,
        string nameAr,
        string nameEn,
        string slug,
        System.Guid? parentId,
        int orderIndex) : base(id)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        Slug = slug;
        ParentId = parentId;
        OrderIndex = orderIndex;
        IsActive = true;
    }

    public string NameAr { get; private set; }

    public string NameEn { get; private set; }

    public string Slug { get; private set; }

    public System.Guid? ParentId { get; private set; }

    public int OrderIndex { get; private set; }

    public bool IsActive { get; private set; }

    public static ResourceCategory Create(
        string nameAr,
        string nameEn,
        string slug,
        System.Guid? parentId,
        int orderIndex)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
        {
            throw new DomainException("NameAr is required.");
        }
        if (string.IsNullOrWhiteSpace(nameEn))
        {
            throw new DomainException("NameEn is required.");
        }
        if (string.IsNullOrWhiteSpace(slug) || !SlugPattern.IsMatch(slug))
        {
            throw new DomainException($"slug '{slug}' must be kebab-case (a-z, 0-9, hyphens).");
        }
        return new ResourceCategory(
            id: System.Guid.NewGuid(),
            nameAr: nameAr,
            nameEn: nameEn,
            slug: slug,
            parentId: parentId,
            orderIndex: orderIndex);
    }

    public void UpdateNames(string nameAr, string nameEn)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
        {
            throw new DomainException("NameAr is required.");
        }
        if (string.IsNullOrWhiteSpace(nameEn))
        {
            throw new DomainException("NameEn is required.");
        }
        NameAr = nameAr;
        NameEn = nameEn;
    }

    public void Reorder(int orderIndex) => OrderIndex = orderIndex;

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~ResourceCategoryTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:    10`.

- [ ] **Step 5: Commit**

```bash
git add backend/src/CCE.Domain/Content/ResourceCategory.cs backend/tests/CCE.Domain.Tests/Content/ResourceCategoryTests.cs
git -c commit.gpgsign=false commit -m "feat(content): ResourceCategory tree entity with kebab-case slug invariant (10 TDD tests)"
```

---

## Task 3.4: `Resource` aggregate root + RowVersion + Center/Country discriminator

**Files:**
- Create: `backend/src/CCE.Domain/Content/Events/ResourcePublishedEvent.cs`
- Create: `backend/src/CCE.Domain/Content/Resource.cs`
- Create: `backend/tests/CCE.Domain.Tests/Content/ResourceTests.cs`

**Rationale:** The flagship content entity. `CountryId == null` discriminates a "center-managed" resource (uploaded by SuperAdmin/ContentManager); non-null = country-uploaded (state rep). Soft-deletable, audited, has `RowVersion` for optimistic concurrency. Publishing fires a domain event.

- [ ] **Step 1: Write the publish event**

`backend/src/CCE.Domain/Content/Events/ResourcePublishedEvent.cs`:

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Content.Events;

/// <summary>
/// Raised when a <see cref="Resource"/> is first published (transitions from draft to public).
/// Phase 07 dispatches this to the search-index updater + recommendation cache invalidator.
/// </summary>
public sealed record ResourcePublishedEvent(
    System.Guid ResourceId,
    System.Guid? CountryId,
    System.Guid CategoryId,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
```

- [ ] **Step 2: Write failing tests**

`backend/tests/CCE.Domain.Tests/Content/ResourceTests.cs`:

```csharp
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Content.Events;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Content;

public class ResourceTests
{
    private static FakeSystemClock NewClock() => new();

    private static Resource NewDraft(FakeSystemClock clock, System.Guid? countryId = null) =>
        Resource.Draft(
            titleAr: "مورد",
            titleEn: "Resource",
            descriptionAr: "وصف",
            descriptionEn: "Description",
            resourceType: ResourceType.Pdf,
            categoryId: System.Guid.NewGuid(),
            countryId: countryId,
            uploadedById: System.Guid.NewGuid(),
            assetFileId: System.Guid.NewGuid(),
            clock: clock);

    [Fact]
    public void Draft_factory_creates_unpublished_resource()
    {
        var clock = NewClock();
        var r = NewDraft(clock);

        r.PublishedOn.Should().BeNull();
        r.ViewCount.Should().Be(0);
        r.IsDeleted.Should().BeFalse();
        r.RowVersion.Should().NotBeNull();
    }

    [Fact]
    public void Draft_with_null_country_marks_center_managed()
    {
        var clock = NewClock();
        var r = NewDraft(clock, countryId: null);
        r.IsCenterManaged.Should().BeTrue();
        r.CountryId.Should().BeNull();
    }

    [Fact]
    public void Draft_with_country_marks_country_managed()
    {
        var clock = NewClock();
        var country = System.Guid.NewGuid();
        var r = NewDraft(clock, countryId: country);
        r.IsCenterManaged.Should().BeFalse();
        r.CountryId.Should().Be(country);
    }

    [Fact]
    public void Draft_with_empty_titleAr_throws()
    {
        var clock = NewClock();
        var act = () => Resource.Draft("", "x", "x", "x", ResourceType.Pdf,
            System.Guid.NewGuid(), null, System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>().WithMessage("*TitleAr*");
    }

    [Fact]
    public void Publish_sets_PublishedOn_and_raises_event()
    {
        var clock = NewClock();
        var r = NewDraft(clock);
        clock.Advance(System.TimeSpan.FromMinutes(10));

        r.Publish(clock);

        r.PublishedOn.Should().Be(clock.UtcNow);
        r.IsPublished.Should().BeTrue();
        r.DomainEvents.OfType<ResourcePublishedEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void Publishing_already_published_resource_is_noop()
    {
        var clock = NewClock();
        var r = NewDraft(clock);
        r.Publish(clock);
        var firstPublishedOn = r.PublishedOn;
        r.ClearDomainEvents();
        clock.Advance(System.TimeSpan.FromHours(1));

        r.Publish(clock);

        r.PublishedOn.Should().Be(firstPublishedOn);
        r.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void IncrementViewCount_increases_by_one()
    {
        var clock = NewClock();
        var r = NewDraft(clock);
        r.IncrementViewCount();
        r.IncrementViewCount();
        r.ViewCount.Should().Be(2);
    }

    [Fact]
    public void SoftDelete_marks_deleted()
    {
        var clock = NewClock();
        var r = NewDraft(clock);
        var deleter = System.Guid.NewGuid();
        r.SoftDelete(deleter, clock);

        r.IsDeleted.Should().BeTrue();
        r.DeletedById.Should().Be(deleter);
        r.DeletedOn.Should().Be(clock.UtcNow);
    }
}
```

- [ ] **Step 3: Run — expect compile errors**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~ResourceTests" 2>&1 | tail -5
```

Expected: build error referencing `Resource` not found.

- [ ] **Step 4: Write `Resource.cs`**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Content.Events;

namespace CCE.Domain.Content;

/// <summary>
/// Knowledge-center resource (PDF, video, image, link, document). Aggregate root.
/// <see cref="CountryId"/> discriminates: <c>null</c> means center-managed (uploaded by
/// admin/content-manager), non-null means country-uploaded (by state rep, scoped to that
/// country). Soft-deletable. <see cref="RowVersion"/> is set by EF on update via
/// <c>[Timestamp]</c> mapping in Phase 07.
/// </summary>
[Audited]
public sealed class Resource : AggregateRoot<System.Guid>, ISoftDeletable
{
    private Resource(
        System.Guid id,
        string titleAr,
        string titleEn,
        string descriptionAr,
        string descriptionEn,
        ResourceType resourceType,
        System.Guid categoryId,
        System.Guid? countryId,
        System.Guid uploadedById,
        System.Guid assetFileId) : base(id)
    {
        TitleAr = titleAr;
        TitleEn = titleEn;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        ResourceType = resourceType;
        CategoryId = categoryId;
        CountryId = countryId;
        UploadedById = uploadedById;
        AssetFileId = assetFileId;
    }

    public string TitleAr { get; private set; }
    public string TitleEn { get; private set; }
    public string DescriptionAr { get; private set; }
    public string DescriptionEn { get; private set; }
    public ResourceType ResourceType { get; private set; }
    public System.Guid CategoryId { get; private set; }
    public System.Guid? CountryId { get; private set; }
    public System.Guid UploadedById { get; private set; }
    public System.Guid AssetFileId { get; private set; }
    public System.DateTimeOffset? PublishedOn { get; private set; }
    public long ViewCount { get; private set; }

    /// <summary>EF-managed concurrency token (rowversion).</summary>
    public byte[] RowVersion { get; private set; } = System.Array.Empty<byte>();

    public bool IsDeleted { get; private set; }
    public System.DateTimeOffset? DeletedOn { get; private set; }
    public System.Guid? DeletedById { get; private set; }

    /// <summary>True when no country owns this resource (center-managed).</summary>
    public bool IsCenterManaged => CountryId is null;

    /// <summary>True when the resource has been published at least once.</summary>
    public bool IsPublished => PublishedOn is not null;

    public static Resource Draft(
        string titleAr,
        string titleEn,
        string descriptionAr,
        string descriptionEn,
        ResourceType resourceType,
        System.Guid categoryId,
        System.Guid? countryId,
        System.Guid uploadedById,
        System.Guid assetFileId,
        ISystemClock clock)
    {
        _ = clock; // Reserved for future use (e.g., DraftedOn timestamp); keeps the signature stable.
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (categoryId == System.Guid.Empty) throw new DomainException("CategoryId is required.");
        if (uploadedById == System.Guid.Empty) throw new DomainException("UploadedById is required.");
        if (assetFileId == System.Guid.Empty) throw new DomainException("AssetFileId is required.");
        return new Resource(
            id: System.Guid.NewGuid(),
            titleAr: titleAr,
            titleEn: titleEn,
            descriptionAr: descriptionAr,
            descriptionEn: descriptionEn,
            resourceType: resourceType,
            categoryId: categoryId,
            countryId: countryId,
            uploadedById: uploadedById,
            assetFileId: assetFileId);
    }

    public void Publish(ISystemClock clock)
    {
        if (IsPublished)
        {
            return;
        }
        PublishedOn = clock.UtcNow;
        RaiseDomainEvent(new ResourcePublishedEvent(
            ResourceId: Id,
            CountryId: CountryId,
            CategoryId: CategoryId,
            OccurredOn: PublishedOn.Value));
    }

    public void IncrementViewCount() => ViewCount++;

    public void SoftDelete(System.Guid deletedById, ISystemClock clock)
    {
        if (deletedById == System.Guid.Empty)
        {
            throw new DomainException("DeletedById is required.");
        }
        if (IsDeleted)
        {
            return;
        }
        IsDeleted = true;
        DeletedById = deletedById;
        DeletedOn = clock.UtcNow;
    }
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~ResourceTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:     8`.

- [ ] **Step 6: Commit**

```bash
git add backend/src/CCE.Domain/Content/Events/ResourcePublishedEvent.cs backend/src/CCE.Domain/Content/Resource.cs backend/tests/CCE.Domain.Tests/Content/ResourceTests.cs
git -c commit.gpgsign=false commit -m "feat(content): Resource aggregate with publish event + Center/Country discriminator + RowVersion (8 TDD tests)"
```

---

## Task 3.5: `News` aggregate + slug + publish event

**Files:**
- Create: `backend/src/CCE.Domain/Content/Events/NewsPublishedEvent.cs`
- Create: `backend/src/CCE.Domain/Content/News.cs`
- Create: `backend/tests/CCE.Domain.Tests/Content/NewsTests.cs`

**Rationale:** Publishable news article. Slug is unique (DB index in Phase 08). Featured image is optional. Publishing fires `NewsPublishedEvent`.

- [ ] **Step 1: Write the publish event**

`backend/src/CCE.Domain/Content/Events/NewsPublishedEvent.cs`:

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Content.Events;

public sealed record NewsPublishedEvent(
    System.Guid NewsId,
    string Slug,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
```

- [ ] **Step 2: Write failing tests**

`backend/tests/CCE.Domain.Tests/Content/NewsTests.cs`:

```csharp
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Content.Events;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Content;

public class NewsTests
{
    private static FakeSystemClock NewClock() => new();

    private static News NewDraft(FakeSystemClock clock) =>
        News.Draft(
            titleAr: "خبر",
            titleEn: "News",
            contentAr: "محتوى",
            contentEn: "Content",
            slug: "first-post",
            authorId: System.Guid.NewGuid(),
            featuredImageUrl: null,
            clock: clock);

    [Fact]
    public void Draft_creates_unpublished_news()
    {
        var clock = NewClock();
        var n = NewDraft(clock);

        n.PublishedOn.Should().BeNull();
        n.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public void Slug_must_be_kebab_case()
    {
        var clock = NewClock();
        var act = () => News.Draft("ا", "x", "ا", "x", "Bad Slug", System.Guid.NewGuid(), null, clock);
        act.Should().Throw<DomainException>().WithMessage("*slug*");
    }

    [Fact]
    public void FeaturedImageUrl_must_be_https()
    {
        var clock = NewClock();
        var act = () => News.Draft("ا", "x", "ا", "x", "x", System.Guid.NewGuid(), "http://insecure", clock);
        act.Should().Throw<DomainException>().WithMessage("*https*");
    }

    [Fact]
    public void Publish_sets_PublishedOn_and_raises_event()
    {
        var clock = NewClock();
        var n = NewDraft(clock);
        clock.Advance(System.TimeSpan.FromMinutes(5));

        n.Publish(clock);

        n.PublishedOn.Should().Be(clock.UtcNow);
        n.DomainEvents.OfType<NewsPublishedEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void MarkFeatured_sets_IsFeatured_true()
    {
        var clock = NewClock();
        var n = NewDraft(clock);
        n.MarkFeatured();
        n.IsFeatured.Should().BeTrue();
    }

    [Fact]
    public void UnmarkFeatured_clears_flag()
    {
        var clock = NewClock();
        var n = NewDraft(clock);
        n.MarkFeatured();
        n.UnmarkFeatured();
        n.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public void SoftDelete_marks_deleted()
    {
        var clock = NewClock();
        var n = NewDraft(clock);
        n.SoftDelete(System.Guid.NewGuid(), clock);
        n.IsDeleted.Should().BeTrue();
    }
}
```

- [ ] **Step 3: Run — expect compile errors**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~NewsTests" 2>&1 | tail -5
```

- [ ] **Step 4: Write `News.cs`**

```csharp
using System.Text.RegularExpressions;
using CCE.Domain.Common;
using CCE.Domain.Content.Events;

namespace CCE.Domain.Content;

/// <summary>
/// News article — bilingual title + rich-text content + optional featured image.
/// Slug is unique (enforced in Phase 08 DB unique index). Soft-deletable, audited.
/// </summary>
[Audited]
public sealed class News : AggregateRoot<System.Guid>, ISoftDeletable
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private News(
        System.Guid id,
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn,
        string slug,
        System.Guid authorId,
        string? featuredImageUrl) : base(id)
    {
        TitleAr = titleAr;
        TitleEn = titleEn;
        ContentAr = contentAr;
        ContentEn = contentEn;
        Slug = slug;
        AuthorId = authorId;
        FeaturedImageUrl = featuredImageUrl;
    }

    public string TitleAr { get; private set; }
    public string TitleEn { get; private set; }
    public string ContentAr { get; private set; }
    public string ContentEn { get; private set; }
    public string Slug { get; private set; }
    public System.Guid AuthorId { get; private set; }
    public string? FeaturedImageUrl { get; private set; }
    public System.DateTimeOffset? PublishedOn { get; private set; }
    public bool IsFeatured { get; private set; }
    public byte[] RowVersion { get; private set; } = System.Array.Empty<byte>();
    public bool IsDeleted { get; private set; }
    public System.DateTimeOffset? DeletedOn { get; private set; }
    public System.Guid? DeletedById { get; private set; }

    public bool IsPublished => PublishedOn is not null;

    public static News Draft(
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn,
        string slug,
        System.Guid authorId,
        string? featuredImageUrl,
        ISystemClock clock)
    {
        _ = clock;
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(contentAr)) throw new DomainException("ContentAr is required.");
        if (string.IsNullOrWhiteSpace(contentEn)) throw new DomainException("ContentEn is required.");
        if (string.IsNullOrWhiteSpace(slug) || !SlugPattern.IsMatch(slug))
        {
            throw new DomainException($"slug '{slug}' must be kebab-case.");
        }
        if (authorId == System.Guid.Empty) throw new DomainException("AuthorId is required.");
        if (featuredImageUrl is not null
            && !featuredImageUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("FeaturedImageUrl must use https://.");
        }
        return new News(
            id: System.Guid.NewGuid(),
            titleAr: titleAr,
            titleEn: titleEn,
            contentAr: contentAr,
            contentEn: contentEn,
            slug: slug,
            authorId: authorId,
            featuredImageUrl: featuredImageUrl);
    }

    public void Publish(ISystemClock clock)
    {
        if (IsPublished) return;
        PublishedOn = clock.UtcNow;
        RaiseDomainEvent(new NewsPublishedEvent(Id, Slug, PublishedOn.Value));
    }

    public void MarkFeatured() => IsFeatured = true;

    public void UnmarkFeatured() => IsFeatured = false;

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

- [ ] **Step 5: Run tests**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~NewsTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:     7`.

- [ ] **Step 6: Commit**

```bash
git add backend/src/CCE.Domain/Content/Events/NewsPublishedEvent.cs backend/src/CCE.Domain/Content/News.cs backend/tests/CCE.Domain.Tests/Content/NewsTests.cs
git -c commit.gpgsign=false commit -m "feat(content): News aggregate with slug invariant + publish event (7 TDD tests)"
```

---

## Task 3.6: `Event` aggregate + EndsOn>StartsOn invariant + ICalUid

**Files:**
- Create: `backend/src/CCE.Domain/Content/Events/EventScheduledEvent.cs`
- Create: `backend/src/CCE.Domain/Content/Event.cs`
- Create: `backend/tests/CCE.Domain.Tests/Content/EventTests.cs`

**Rationale:** Calendar event. Hard invariant `EndsOn > StartsOn`. `ICalUid` is generated once at creation (stable for `.ics` regeneration). Online meeting URL optional.

- [ ] **Step 1: Write the schedule event**

`backend/src/CCE.Domain/Content/Events/EventScheduledEvent.cs`:

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Content.Events;

public sealed record EventScheduledEvent(
    System.Guid EventId,
    System.DateTimeOffset StartsOn,
    System.DateTimeOffset EndsOn,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
```

- [ ] **Step 2: Write failing tests**

`backend/tests/CCE.Domain.Tests/Content/EventTests.cs`:

```csharp
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Content.Events;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Content;

public class EventTests
{
    private static FakeSystemClock NewClock() => new();

    private static Event NewEvent(FakeSystemClock clock) =>
        Event.Schedule(
            titleAr: "حدث",
            titleEn: "Event",
            descriptionAr: "وصف",
            descriptionEn: "Description",
            startsOn: clock.UtcNow.AddDays(7),
            endsOn: clock.UtcNow.AddDays(7).AddHours(2),
            locationAr: "الرياض",
            locationEn: "Riyadh",
            onlineMeetingUrl: null,
            featuredImageUrl: null,
            clock: clock);

    [Fact]
    public void Schedule_creates_event_with_generated_ICalUid()
    {
        var clock = NewClock();
        var e = NewEvent(clock);

        e.ICalUid.Should().NotBeNullOrWhiteSpace();
        e.ICalUid.Length.Should().BeGreaterThan(10);
    }

    [Fact]
    public void Schedule_raises_EventScheduledEvent()
    {
        var clock = NewClock();
        var e = NewEvent(clock);
        e.DomainEvents.OfType<EventScheduledEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void EndsOn_must_be_after_StartsOn()
    {
        var clock = NewClock();
        var act = () => Event.Schedule("ا", "x", "ا", "x",
            clock.UtcNow.AddDays(7),
            clock.UtcNow.AddDays(7),
            null, null, null, null, clock);
        act.Should().Throw<DomainException>().WithMessage("*EndsOn*");
    }

    [Fact]
    public void EndsOn_before_StartsOn_throws()
    {
        var clock = NewClock();
        var act = () => Event.Schedule("ا", "x", "ا", "x",
            clock.UtcNow.AddDays(7),
            clock.UtcNow.AddDays(6),
            null, null, null, null, clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void OnlineMeetingUrl_must_be_https()
    {
        var clock = NewClock();
        var act = () => Event.Schedule("ا", "x", "ا", "x",
            clock.UtcNow.AddDays(7),
            clock.UtcNow.AddDays(7).AddHours(2),
            null, null, "http://insecure", null, clock);
        act.Should().Throw<DomainException>().WithMessage("*https*");
    }

    [Fact]
    public void Reschedule_updates_window_when_valid()
    {
        var clock = NewClock();
        var e = NewEvent(clock);
        var newStart = clock.UtcNow.AddDays(14);
        var newEnd = newStart.AddHours(3);

        e.Reschedule(newStart, newEnd);

        e.StartsOn.Should().Be(newStart);
        e.EndsOn.Should().Be(newEnd);
    }

    [Fact]
    public void Reschedule_with_invalid_window_throws()
    {
        var clock = NewClock();
        var e = NewEvent(clock);
        var act = () => e.Reschedule(clock.UtcNow.AddDays(14), clock.UtcNow.AddDays(13));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ICalUid_does_not_change_after_reschedule()
    {
        var clock = NewClock();
        var e = NewEvent(clock);
        var uid = e.ICalUid;

        e.Reschedule(clock.UtcNow.AddDays(14), clock.UtcNow.AddDays(14).AddHours(1));

        e.ICalUid.Should().Be(uid);
    }
}
```

- [ ] **Step 3: Write `Event.cs`**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Content.Events;

namespace CCE.Domain.Content;

/// <summary>
/// Calendar event. Bilingual title/description/location, optional online meeting URL.
/// <see cref="ICalUid"/> is generated once at creation and never changes — keeping it
/// stable lets external calendar clients (.ics consumers) deduplicate updates by UID.
/// </summary>
[Audited]
public sealed class Event : AggregateRoot<System.Guid>, ISoftDeletable
{
    private Event(
        System.Guid id,
        string titleAr,
        string titleEn,
        string descriptionAr,
        string descriptionEn,
        System.DateTimeOffset startsOn,
        System.DateTimeOffset endsOn,
        string? locationAr,
        string? locationEn,
        string? onlineMeetingUrl,
        string? featuredImageUrl,
        string iCalUid) : base(id)
    {
        TitleAr = titleAr;
        TitleEn = titleEn;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        StartsOn = startsOn;
        EndsOn = endsOn;
        LocationAr = locationAr;
        LocationEn = locationEn;
        OnlineMeetingUrl = onlineMeetingUrl;
        FeaturedImageUrl = featuredImageUrl;
        ICalUid = iCalUid;
    }

    public string TitleAr { get; private set; }
    public string TitleEn { get; private set; }
    public string DescriptionAr { get; private set; }
    public string DescriptionEn { get; private set; }
    public System.DateTimeOffset StartsOn { get; private set; }
    public System.DateTimeOffset EndsOn { get; private set; }
    public string? LocationAr { get; private set; }
    public string? LocationEn { get; private set; }
    public string? OnlineMeetingUrl { get; private set; }
    public string? FeaturedImageUrl { get; private set; }

    /// <summary>Stable iCalendar UID (set at creation). Never changes.</summary>
    public string ICalUid { get; private set; }

    public byte[] RowVersion { get; private set; } = System.Array.Empty<byte>();
    public bool IsDeleted { get; private set; }
    public System.DateTimeOffset? DeletedOn { get; private set; }
    public System.Guid? DeletedById { get; private set; }

    public static Event Schedule(
        string titleAr,
        string titleEn,
        string descriptionAr,
        string descriptionEn,
        System.DateTimeOffset startsOn,
        System.DateTimeOffset endsOn,
        string? locationAr,
        string? locationEn,
        string? onlineMeetingUrl,
        string? featuredImageUrl,
        ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (endsOn <= startsOn)
        {
            throw new DomainException("EndsOn must be strictly after StartsOn.");
        }
        if (onlineMeetingUrl is not null
            && !onlineMeetingUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("OnlineMeetingUrl must use https://.");
        }
        if (featuredImageUrl is not null
            && !featuredImageUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("FeaturedImageUrl must use https://.");
        }
        var id = System.Guid.NewGuid();
        var iCalUid = $"{id:N}@cce.moenergy.gov.sa";
        var ev = new Event(id, titleAr, titleEn, descriptionAr, descriptionEn,
            startsOn, endsOn, locationAr, locationEn, onlineMeetingUrl, featuredImageUrl, iCalUid);
        ev.RaiseDomainEvent(new EventScheduledEvent(id, startsOn, endsOn, clock.UtcNow));
        return ev;
    }

    public void Reschedule(System.DateTimeOffset startsOn, System.DateTimeOffset endsOn)
    {
        if (endsOn <= startsOn)
        {
            throw new DomainException("EndsOn must be strictly after StartsOn.");
        }
        StartsOn = startsOn;
        EndsOn = endsOn;
    }

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

- [ ] **Step 4: Run tests**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~EventTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:     8`.

- [ ] **Step 5: Commit**

```bash
git add backend/src/CCE.Domain/Content/Events/EventScheduledEvent.cs backend/src/CCE.Domain/Content/Event.cs backend/tests/CCE.Domain.Tests/Content/EventTests.cs
git -c commit.gpgsign=false commit -m "feat(content): Event aggregate with EndsOn>StartsOn invariant + stable ICalUid (8 TDD tests)"
```

---

## Task 3.7: `Page` aggregate (static pages)

**Files:**
- Create: `backend/src/CCE.Domain/Content/Page.cs`
- Create: `backend/tests/CCE.Domain.Tests/Content/PageTests.cs`

**Rationale:** Static admin-managed pages (about/terms/privacy/custom). Slug unique per `PageType`. No publish workflow — pages are always visible once created.

- [ ] **Step 1: Write tests**

`backend/tests/CCE.Domain.Tests/Content/PageTests.cs`:

```csharp
using CCE.Domain.Common;
using CCE.Domain.Content;

namespace CCE.Domain.Tests.Content;

public class PageTests
{
    [Fact]
    public void Create_builds_a_page()
    {
        var p = Page.Create("about", PageType.AboutPlatform,
            "عن المنصة", "About the platform",
            "محتوى", "content");

        p.Slug.Should().Be("about");
        p.PageType.Should().Be(PageType.AboutPlatform);
        p.TitleAr.Should().Be("عن المنصة");
        p.RowVersion.Should().NotBeNull();
    }

    [Fact]
    public void Slug_must_be_kebab_case()
    {
        var act = () => Page.Create("Bad Slug", PageType.Custom, "ا", "x", "ا", "x");
        act.Should().Throw<DomainException>().WithMessage("*slug*");
    }

    [Fact]
    public void UpdateContent_replaces_bilingual_content()
    {
        var p = Page.Create("about", PageType.AboutPlatform, "ا", "x", "ا", "x");
        p.UpdateContent("ج", "new", "ج", "new");
        p.TitleAr.Should().Be("ج");
        p.ContentEn.Should().Be("new");
    }

    [Fact]
    public void SoftDelete_marks_deleted()
    {
        var p = Page.Create("about", PageType.AboutPlatform, "ا", "x", "ا", "x");
        p.SoftDelete(System.Guid.NewGuid(), new CCE.TestInfrastructure.Time.FakeSystemClock());
        p.IsDeleted.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Write `Page.cs`**

```csharp
using System.Text.RegularExpressions;
using CCE.Domain.Common;

namespace CCE.Domain.Content;

/// <summary>
/// Static page. Slug is unique within (<see cref="PageType"/>) — enforced by Phase 08
/// composite unique index. Content is rich-text bilingual.
/// </summary>
[Audited]
public sealed class Page : AggregateRoot<System.Guid>, ISoftDeletable
{
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    private Page(
        System.Guid id,
        string slug,
        PageType pageType,
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn) : base(id)
    {
        Slug = slug;
        PageType = pageType;
        TitleAr = titleAr;
        TitleEn = titleEn;
        ContentAr = contentAr;
        ContentEn = contentEn;
    }

    public string Slug { get; private set; }
    public PageType PageType { get; private set; }
    public string TitleAr { get; private set; }
    public string TitleEn { get; private set; }
    public string ContentAr { get; private set; }
    public string ContentEn { get; private set; }
    public byte[] RowVersion { get; private set; } = System.Array.Empty<byte>();
    public bool IsDeleted { get; private set; }
    public System.DateTimeOffset? DeletedOn { get; private set; }
    public System.Guid? DeletedById { get; private set; }

    public static Page Create(
        string slug,
        PageType pageType,
        string titleAr,
        string titleEn,
        string contentAr,
        string contentEn)
    {
        if (string.IsNullOrWhiteSpace(slug) || !SlugPattern.IsMatch(slug))
        {
            throw new DomainException($"slug '{slug}' must be kebab-case.");
        }
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(contentAr)) throw new DomainException("ContentAr is required.");
        if (string.IsNullOrWhiteSpace(contentEn)) throw new DomainException("ContentEn is required.");
        return new Page(System.Guid.NewGuid(), slug, pageType, titleAr, titleEn, contentAr, contentEn);
    }

    public void UpdateContent(string titleAr, string titleEn, string contentAr, string contentEn)
    {
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(contentAr)) throw new DomainException("ContentAr is required.");
        if (string.IsNullOrWhiteSpace(contentEn)) throw new DomainException("ContentEn is required.");
        TitleAr = titleAr;
        TitleEn = titleEn;
        ContentAr = contentAr;
        ContentEn = contentEn;
    }

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

- [ ] **Step 3: Run tests**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~PageTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:     4`.

- [ ] **Step 4: Commit**

```bash
git add backend/src/CCE.Domain/Content/Page.cs backend/tests/CCE.Domain.Tests/Content/PageTests.cs
git -c commit.gpgsign=false commit -m "feat(content): Page aggregate (static pages, slug-per-type) (4 TDD tests)"
```

---

## Task 3.8: `HomepageSection` entity (admin-reorderable blocks)

**Files:**
- Create: `backend/src/CCE.Domain/Content/HomepageSection.cs`
- Create: `backend/tests/CCE.Domain.Tests/Content/HomepageSectionTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Content;

public class HomepageSectionTests
{
    [Fact]
    public void Create_builds_active_section()
    {
        var s = HomepageSection.Create(HomepageSectionType.Hero, 0, "{}", "{}");
        s.SectionType.Should().Be(HomepageSectionType.Hero);
        s.OrderIndex.Should().Be(0);
        s.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Reorder_updates_OrderIndex()
    {
        var s = HomepageSection.Create(HomepageSectionType.Hero, 0, "{}", "{}");
        s.Reorder(5);
        s.OrderIndex.Should().Be(5);
    }

    [Fact]
    public void UpdateContent_replaces_bilingual_content()
    {
        var s = HomepageSection.Create(HomepageSectionType.Hero, 0, "{}", "{}");
        s.UpdateContent("{ \"a\": 1 }", "{ \"b\": 2 }");
        s.ContentAr.Should().Be("{ \"a\": 1 }");
        s.ContentEn.Should().Be("{ \"b\": 2 }");
    }

    [Fact]
    public void Deactivate_and_Activate_toggle_IsActive()
    {
        var s = HomepageSection.Create(HomepageSectionType.Hero, 0, "{}", "{}");
        s.Deactivate();
        s.IsActive.Should().BeFalse();
        s.Activate();
        s.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_marks_deleted()
    {
        var s = HomepageSection.Create(HomepageSectionType.Hero, 0, "{}", "{}");
        s.SoftDelete(System.Guid.NewGuid(), new FakeSystemClock());
        s.IsDeleted.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Write `HomepageSection.cs`**

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Content;

/// <summary>
/// One block on the public homepage. Admins reorder via <see cref="OrderIndex"/>; the
/// rendering layer queries <c>WHERE IsActive = true ORDER BY OrderIndex</c>.
/// </summary>
[Audited]
public sealed class HomepageSection : Entity<System.Guid>, ISoftDeletable
{
    private HomepageSection(
        System.Guid id,
        HomepageSectionType sectionType,
        int orderIndex,
        string contentAr,
        string contentEn) : base(id)
    {
        SectionType = sectionType;
        OrderIndex = orderIndex;
        ContentAr = contentAr;
        ContentEn = contentEn;
        IsActive = true;
    }

    public HomepageSectionType SectionType { get; private set; }
    public int OrderIndex { get; private set; }
    public string ContentAr { get; private set; }
    public string ContentEn { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public System.DateTimeOffset? DeletedOn { get; private set; }
    public System.Guid? DeletedById { get; private set; }

    public static HomepageSection Create(HomepageSectionType type, int orderIndex, string contentAr, string contentEn)
    {
        return new HomepageSection(System.Guid.NewGuid(), type, orderIndex,
            contentAr ?? string.Empty, contentEn ?? string.Empty);
    }

    public void Reorder(int orderIndex) => OrderIndex = orderIndex;

    public void UpdateContent(string contentAr, string contentEn)
    {
        ContentAr = contentAr ?? string.Empty;
        ContentEn = contentEn ?? string.Empty;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

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

- [ ] **Step 3: Run tests**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~HomepageSectionTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:     5`.

- [ ] **Step 4: Commit**

```bash
git add backend/src/CCE.Domain/Content/HomepageSection.cs backend/tests/CCE.Domain.Tests/Content/HomepageSectionTests.cs
git -c commit.gpgsign=false commit -m "feat(content): HomepageSection entity with reorder + activation (5 TDD tests)"
```

---

## Task 3.9: `NewsletterSubscription` entity + double opt-in

**Files:**
- Create: `backend/src/CCE.Domain/Content/Events/NewsletterConfirmedEvent.cs`
- Create: `backend/src/CCE.Domain/Content/NewsletterSubscription.cs`
- Create: `backend/tests/CCE.Domain.Tests/Content/NewsletterSubscriptionTests.cs`

**Rationale:** Email-list subscription. Double opt-in: subscribe creates a row with `IsConfirmed=false` + a confirmation token; clicking the email link calls `Confirm(token)`. Resubscribing (after unsubscribe) reissues a fresh token.

- [ ] **Step 1: Confirmed event**

`backend/src/CCE.Domain/Content/Events/NewsletterConfirmedEvent.cs`:

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Content.Events;

public sealed record NewsletterConfirmedEvent(
    System.Guid SubscriptionId,
    string Email,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Content.Events;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Content;

public class NewsletterSubscriptionTests
{
    private static FakeSystemClock NewClock() => new();

    [Fact]
    public void Subscribe_creates_unconfirmed_record_with_token()
    {
        var clock = NewClock();
        var s = NewsletterSubscription.Subscribe("a@b.com", "ar", clock);

        s.Email.Should().Be("a@b.com");
        s.LocalePreference.Should().Be("ar");
        s.IsConfirmed.Should().BeFalse();
        s.ConfirmationToken.Should().NotBeNullOrWhiteSpace();
        s.ConfirmedOn.Should().BeNull();
    }

    [Fact]
    public void Subscribe_with_invalid_email_throws()
    {
        var clock = NewClock();
        var act = () => NewsletterSubscription.Subscribe("not-an-email", "ar", clock);
        act.Should().Throw<DomainException>().WithMessage("*email*");
    }

    [Fact]
    public void Subscribe_with_invalid_locale_throws()
    {
        var clock = NewClock();
        var act = () => NewsletterSubscription.Subscribe("a@b.com", "fr", clock);
        act.Should().Throw<DomainException>().WithMessage("*locale*");
    }

    [Fact]
    public void Confirm_with_correct_token_sets_IsConfirmed_and_raises_event()
    {
        var clock = NewClock();
        var s = NewsletterSubscription.Subscribe("a@b.com", "ar", clock);
        var token = s.ConfirmationToken;
        clock.Advance(System.TimeSpan.FromMinutes(2));

        s.Confirm(token, clock);

        s.IsConfirmed.Should().BeTrue();
        s.ConfirmedOn.Should().Be(clock.UtcNow);
        s.DomainEvents.OfType<NewsletterConfirmedEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void Confirm_with_wrong_token_throws()
    {
        var clock = NewClock();
        var s = NewsletterSubscription.Subscribe("a@b.com", "ar", clock);

        var act = () => s.Confirm("wrong-token", clock);
        act.Should().Throw<DomainException>().WithMessage("*token*");
    }

    [Fact]
    public void Cannot_confirm_twice()
    {
        var clock = NewClock();
        var s = NewsletterSubscription.Subscribe("a@b.com", "ar", clock);
        var token = s.ConfirmationToken;
        s.Confirm(token, clock);

        var act = () => s.Confirm(token, clock);
        act.Should().Throw<DomainException>().WithMessage("*already*");
    }

    [Fact]
    public void Unsubscribe_after_confirm_records_unsubscribedOn()
    {
        var clock = NewClock();
        var s = NewsletterSubscription.Subscribe("a@b.com", "ar", clock);
        s.Confirm(s.ConfirmationToken, clock);
        clock.Advance(System.TimeSpan.FromDays(7));

        s.Unsubscribe(clock);

        s.UnsubscribedOn.Should().Be(clock.UtcNow);
    }
}
```

- [ ] **Step 3: Write `NewsletterSubscription.cs`**

```csharp
using System.Text.RegularExpressions;
using CCE.Domain.Common;
using CCE.Domain.Content.Events;

namespace CCE.Domain.Content;

/// <summary>
/// Email-list subscription with double opt-in. Subscribing creates a record with a
/// fresh confirmation token; confirming consumes the token and marks the subscription
/// active. Unsubscribing keeps the row but stamps <see cref="UnsubscribedOn"/>.
/// </summary>
[Audited]
public sealed class NewsletterSubscription : Entity<System.Guid>
{
    private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

    private NewsletterSubscription(
        System.Guid id,
        string email,
        string localePreference,
        string confirmationToken) : base(id)
    {
        Email = email;
        LocalePreference = localePreference;
        ConfirmationToken = confirmationToken;
    }

    public string Email { get; private set; }
    public string LocalePreference { get; private set; }
    public bool IsConfirmed { get; private set; }
    public string ConfirmationToken { get; private set; }
    public System.DateTimeOffset? ConfirmedOn { get; private set; }
    public System.DateTimeOffset? UnsubscribedOn { get; private set; }

    public static NewsletterSubscription Subscribe(string email, string localePreference, ISystemClock clock)
    {
        _ = clock;
        if (string.IsNullOrWhiteSpace(email) || !EmailPattern.IsMatch(email))
        {
            throw new DomainException($"email '{email}' is invalid.");
        }
        if (localePreference != "ar" && localePreference != "en")
        {
            throw new DomainException("locale must be 'ar' or 'en'.");
        }
        return new NewsletterSubscription(
            id: System.Guid.NewGuid(),
            email: email,
            localePreference: localePreference,
            confirmationToken: System.Guid.NewGuid().ToString("N"));
    }

    public void Confirm(string token, ISystemClock clock)
    {
        if (IsConfirmed)
        {
            throw new DomainException("Subscription already confirmed.");
        }
        if (string.IsNullOrWhiteSpace(token) || token != ConfirmationToken)
        {
            throw new DomainException("Invalid confirmation token.");
        }
        IsConfirmed = true;
        ConfirmedOn = clock.UtcNow;
        RaiseDomainEvent(new NewsletterConfirmedEvent(Id, Email, ConfirmedOn.Value));
    }

    public void Unsubscribe(ISystemClock clock)
    {
        UnsubscribedOn = clock.UtcNow;
    }
}
```

Note: this entity raises a domain event but extends `Entity<Guid>` not `AggregateRoot<Guid>` per spec. `Entity<TId>` already provides `RaiseDomainEvent` (Foundation Phase 04 design).

- [ ] **Step 4: Run tests**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~NewsletterSubscriptionTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:     7`.

- [ ] **Step 5: Commit**

```bash
git add backend/src/CCE.Domain/Content/Events/NewsletterConfirmedEvent.cs backend/src/CCE.Domain/Content/NewsletterSubscription.cs backend/tests/CCE.Domain.Tests/Content/NewsletterSubscriptionTests.cs
git -c commit.gpgsign=false commit -m "feat(content): NewsletterSubscription entity with double opt-in + confirmed event (7 TDD tests)"
```

---

## Task 3.10: `RowVersion` cross-cutting test (Resource/News/Event/Page)

**Files:**
- Create: `backend/tests/CCE.Domain.Tests/Content/RowVersionContractTests.cs`

**Rationale:** Spec §5.1 requires a `RowVersion` byte[] on every aggregate root that needs optimistic concurrency. Phase 08 wires it via `[Timestamp]`/`HasRowVersion()`. Phase 03 just exposes the property; this test asserts the contract is in place across Resource/News/Event/Page.

- [ ] **Step 1: Test**

```csharp
using CCE.Domain.Content;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Content;

public class RowVersionContractTests
{
    [Theory]
    [InlineData(typeof(Resource))]
    [InlineData(typeof(News))]
    [InlineData(typeof(Event))]
    [InlineData(typeof(Page))]
    public void Aggregate_root_exposes_byte_array_RowVersion(System.Type type)
    {
        var prop = type.GetProperty("RowVersion",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        prop.Should().NotBeNull(because: $"{type.Name} should expose a RowVersion property");
        prop!.PropertyType.Should().Be(typeof(byte[]),
            because: $"{type.Name}.RowVersion must be byte[] for SQL Server rowversion mapping");
    }

    [Fact]
    public void Resource_RowVersion_initialised_to_empty_array()
    {
        var clock = new FakeSystemClock();
        var r = Resource.Draft("ا", "x", "ا", "x", ResourceType.Pdf,
            System.Guid.NewGuid(), null, System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        r.RowVersion.Should().NotBeNull();
        r.RowVersion.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Run tests**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~RowVersionContractTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:     5` (4 theory rows + 1 fact).

- [ ] **Step 3: Commit**

```bash
git add backend/tests/CCE.Domain.Tests/Content/RowVersionContractTests.cs
git -c commit.gpgsign=false commit -m "test(content): cross-aggregate RowVersion contract (5 tests)"
```

---

## Task 3.11: Audited-attribute coverage test (Content)

**Files:**
- Create: `backend/tests/CCE.Domain.Tests/Content/AuditedCoverageTests.cs`

**Rationale:** Spec §4.11 mandates `[Audited]` on every aggregate root. We test it for the 4 aggregates (`Resource`, `News`, `Event`, `Page`) plus the entities that the spec marks audited (`AssetFile`, `ResourceCategory`, `HomepageSection`, `NewsletterSubscription`). High-volume entities omitted from the spec's audit list are not part of this phase.

- [ ] **Step 1: Test**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Content;

namespace CCE.Domain.Tests.Content;

public class AuditedCoverageTests
{
    [Theory]
    [InlineData(typeof(Resource))]
    [InlineData(typeof(News))]
    [InlineData(typeof(Event))]
    [InlineData(typeof(Page))]
    [InlineData(typeof(AssetFile))]
    [InlineData(typeof(ResourceCategory))]
    [InlineData(typeof(HomepageSection))]
    [InlineData(typeof(NewsletterSubscription))]
    public void Content_entity_carries_AuditedAttribute(System.Type type)
    {
        var attrs = type.GetCustomAttributes(typeof(AuditedAttribute), inherit: false);
        attrs.Should().HaveCount(1, because: $"{type.Name} must be marked [Audited] (spec §4.11)");
    }
}
```

- [ ] **Step 2: Run tests**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~AuditedCoverageTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:     8`.

- [ ] **Step 3: Commit**

```bash
git add backend/tests/CCE.Domain.Tests/Content/AuditedCoverageTests.cs
git -c commit.gpgsign=false commit -m "test(content): [Audited] coverage on 8 Content entities (8 tests)"
```

---

## Task 3.12: Phase 03 close

**Files:**
- Modify: `docs/subprojects/02-data-domain-progress.md`

- [ ] **Step 1: Full backend run**

```bash
dotnet test backend/CCE.sln --nologo --no-restore --logger "console;verbosity=minimal" 2>&1 | grep -E "(Passed!|Failed!)"
```

Expected: 5 lines, all `Passed!`. Compute Domain.Tests count = 78 (Phase 02) + 8 + 10 + 8 + 7 + 8 + 4 + 5 + 7 + 5 + 8 = 148. Backend total ≈ 148 + 12 + 28 + 6 + 10 = 204. Use the actual numbers reported.

- [ ] **Step 2: Update progress doc**

Replace:
```markdown
| 03 | Content | ⏳ Pending |
```
with:
```markdown
| 03 | Content | ✅ Done |
```

Update test totals row to use actual numbers from Step 1 (Domain ~148, Cumulative ~204).

- [ ] **Step 3: Commit**

```bash
git add docs/subprojects/02-data-domain-progress.md
git -c commit.gpgsign=false commit -m "docs(sub-2): mark Phase 03 done; test totals updated (Content bounded context shipped)"
```

---

## Phase 03 — completion checklist

- [ ] 8 Content entities exist under `backend/src/CCE.Domain/Content/`.
- [ ] 4 enums (`ResourceType`, `VirusScanStatus`, `PageType`, `HomepageSectionType`).
- [ ] 4 domain events (`ResourcePublishedEvent`, `NewsPublishedEvent`, `EventScheduledEvent`, `NewsletterConfirmedEvent`).
- [ ] `AssetFile` virus-scan state machine (Pending → terminal).
- [ ] `Resource` Center/Country discriminator + Publish event + RowVersion.
- [ ] `News` slug uniqueness invariant + Publish event.
- [ ] `Event` `EndsOn > StartsOn` + stable ICalUid.
- [ ] `Page` slug-per-PageType.
- [ ] `HomepageSection` reorder.
- [ ] `NewsletterSubscription` double opt-in (subscribe → confirm with token).
- [ ] All 4 aggregates expose `byte[] RowVersion`.
- [ ] All 8 entities carry `[Audited]`.
- [ ] `dotnet build backend/CCE.sln` 0 errors / 0 warnings.
- [ ] All Phase 02 regression tests still pass.
- [ ] `git status` clean.
- [ ] 12 new commits with the messages shown above.

**If all boxes ticked, Phase 03 is complete. Proceed to Phase 04 (Country bounded context — 4 entities).**
