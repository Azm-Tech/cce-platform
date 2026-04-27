# Phase 02 — Identity bounded context

> Parent: [`../2026-04-27-data-domain.md`](../2026-04-27-data-domain.md) · Spec: [`../../specs/2026-04-27-data-domain-design.md`](../../specs/2026-04-27-data-domain-design.md) §4.1

**Phase goal:** Land 5 Identity entities under `CCE.Domain.Identity/` — `User` (extends `IdentityUser<Guid>`), `Role` (extends `IdentityRole<Guid>`), `StateRepresentativeAssignment`, `ExpertProfile`, and `ExpertRegistrationRequest` (aggregate root with state machine + domain events). Pure domain layer — no EF mappings, no DbContext changes (those land in Phase 07).

**Tasks in this phase:** 8
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 01 closed (commit `90a9d36` is HEAD).
- `dotnet build backend/CCE.sln` 0 warnings 0 errors.
- `dotnet test backend/CCE.sln` reports 85 backend passing.

---

## Pre-execution sanity checks

1. `git status` clean, on `main` (apart from untracked `.claude/`).
2. `git log --oneline -1` → `0fd0cb6` or later.
3. `ls backend/src/CCE.Domain/Common/` includes `Entity.cs`, `AggregateRoot.cs`, `ISoftDeletable.cs`, `AuditedAttribute.cs`, `DomainException.cs`, `IDomainEvent.cs`, `ISystemClock.cs`.
4. `dotnet test backend/CCE.sln --no-restore --logger "console;verbosity=minimal" 2>&1 | grep -E "(Passed!|Failed!)"` → 5 lines, all `Passed!`.

If any fail, stop and report.

---

## Task 2.1: Add `Microsoft.Extensions.Identity.Stores` to CPM + reference from `CCE.Domain`

**Files:**
- Modify: `backend/Directory.Packages.props`
- Modify: `backend/src/CCE.Domain/CCE.Domain.csproj`

**Rationale:** `IdentityUser<TKey>` and `IdentityRole<TKey>` live in `Microsoft.Extensions.Identity.Stores` (transitive of `Microsoft.AspNetCore.Identity.EntityFrameworkCore`). We want them in Domain WITHOUT pulling EF Core into Domain. Pin the package in CPM and add a `<PackageReference>` to `CCE.Domain.csproj`.

**Note on Clean Architecture:** Yes, this leaks the `Microsoft.Extensions.Identity.Stores` namespace into Domain. The trade-off is well-known: extending `IdentityUser<Guid>` from a single `User` entity is simpler and less fragile than the alternative dual-class pattern (DomainUser + IdentityUser sync). Spec §4.1 accepts this trade-off explicitly. The package itself depends only on `Microsoft.Extensions.Logging.Abstractions` — it does NOT pull EF Core.

- [ ] **Step 1: Add the package to CPM**

Open `backend/Directory.Packages.props`. Find the `Identity (external users)` ItemGroup near line 57 and insert before `<PackageVersion Include="Microsoft.AspNetCore.Identity" Version="2.3.1" />`:

```xml
    <PackageVersion Include="Microsoft.Extensions.Identity.Stores" Version="8.0.10" />
```

Final group:

```xml
  <ItemGroup Label="Identity (external users)">
    <PackageVersion Include="Microsoft.Extensions.Identity.Stores" Version="8.0.10" />
    <PackageVersion Include="Microsoft.AspNetCore.Identity" Version="2.3.1" />
    <PackageVersion Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.10" />
    <PackageVersion Include="Microsoft.AspNetCore.Identity.UI" Version="8.0.10" />
  </ItemGroup>
```

- [ ] **Step 2: Reference the package from `CCE.Domain.csproj`**

Open `backend/src/CCE.Domain/CCE.Domain.csproj`. After the existing `<ItemGroup>` block holding the Source generator project reference, add a new `<ItemGroup>`:

```xml
  <ItemGroup>
    <!-- IdentityUser&lt;TKey&gt; / IdentityRole&lt;TKey&gt; without EF dependency. -->
    <PackageReference Include="Microsoft.Extensions.Identity.Stores" />
  </ItemGroup>
```

- [ ] **Step 3: Restore + build**

Run:

```bash
dotnet restore backend/src/CCE.Domain/CCE.Domain.csproj --source ~/.nuget/packages 2>&1 | tail -5
```

Expected: `Restored ...`. (If NuGet feed timeouts, the local-cache source covers everything because Identity.EFCore 8.0.10 is already present in cache and pulls Identity.Stores transitively.)

```bash
dotnet build backend/src/CCE.Domain/CCE.Domain.csproj --nologo --no-restore 2>&1 | tail -6
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 4: Quick reachability test — verify `IdentityUser<Guid>` is now visible from Domain**

Create `backend/src/CCE.Domain/Identity/IdentityReferenceProbe.cs` (temporary):

```csharp
namespace CCE.Domain.Identity;

internal static class IdentityReferenceProbe
{
    // Compiles only if Microsoft.AspNetCore.Identity.IdentityUser<Guid> is reachable.
    internal static System.Type Probe = typeof(Microsoft.AspNetCore.Identity.IdentityUser<System.Guid>);
}
```

Run:

```bash
dotnet build backend/src/CCE.Domain/CCE.Domain.csproj --nologo --no-restore 2>&1 | tail -6
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`. If it fails with `IdentityUser<>` not found, the package transitive didn't bring the assembly — STOP and add `<PackageReference Include="Microsoft.AspNetCore.Identity" />` to CPM workaround. Once verified, **delete the probe file before commit**.

- [ ] **Step 5: Delete the probe file**

```bash
rm backend/src/CCE.Domain/Identity/IdentityReferenceProbe.cs
```

(Step 4's purpose was reachability check, not a permanent test. Task 2.2 creates `User.cs` in this same folder.)

- [ ] **Step 6: Commit**

```bash
git add backend/Directory.Packages.props backend/src/CCE.Domain/CCE.Domain.csproj
git -c commit.gpgsign=false commit -m "chore(identity): pin Microsoft.Extensions.Identity.Stores 8.0.10 + reference from CCE.Domain"
```

---

## Task 2.2: `User` entity (extends `IdentityUser<Guid>`) — properties + defaults

**Files:**
- Create: `backend/src/CCE.Domain/Identity/KnowledgeLevel.cs`
- Create: `backend/src/CCE.Domain/Identity/User.cs`
- Create: `backend/tests/CCE.Domain.Tests/Identity/UserDefaultsTests.cs`

**Rationale:** The `User` entity stores user-profile fields that ASP.NET Identity doesn't natively model: locale preference, knowledge level, interests, country, avatar. Defaults are domain decisions (LocalePreference="ar", KnowledgeLevel=Beginner, Interests=empty).

This task ships the type + defaults only. Task 2.3 adds mutators + invariants on a separate commit.

- [ ] **Step 1: Write failing tests for User defaults**

`backend/tests/CCE.Domain.Tests/Identity/UserDefaultsTests.cs`:

```csharp
using CCE.Domain.Identity;

namespace CCE.Domain.Tests.Identity;

public class UserDefaultsTests
{
    [Fact]
    public void New_user_defaults_LocalePreference_to_ar()
    {
        var user = new User();
        user.LocalePreference.Should().Be("ar");
    }

    [Fact]
    public void New_user_defaults_KnowledgeLevel_to_Beginner()
    {
        var user = new User();
        user.KnowledgeLevel.Should().Be(KnowledgeLevel.Beginner);
    }

    [Fact]
    public void New_user_defaults_Interests_to_empty_list()
    {
        var user = new User();
        user.Interests.Should().BeEmpty();
    }

    [Fact]
    public void New_user_defaults_CountryId_to_null()
    {
        var user = new User();
        user.CountryId.Should().BeNull();
    }

    [Fact]
    public void New_user_defaults_AvatarUrl_to_null()
    {
        var user = new User();
        user.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public void User_inherits_IdentityUser_of_Guid()
    {
        var user = new User();
        user.Should().BeAssignableTo<Microsoft.AspNetCore.Identity.IdentityUser<System.Guid>>();
    }
}
```

- [ ] **Step 2: Run — expect compile errors**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~UserDefaultsTests" 2>&1 | tail -8
```

Expected: build error referencing `User` and `KnowledgeLevel` not found.

- [ ] **Step 3: Write `KnowledgeLevel.cs`**

`backend/src/CCE.Domain/Identity/KnowledgeLevel.cs`:

```csharp
namespace CCE.Domain.Identity;

/// <summary>
/// Self-declared user knowledge level — drives content-recommendation defaults and
/// feeds the Knowledge Maps starting node selection.
/// </summary>
public enum KnowledgeLevel
{
    /// <summary>Default for new accounts.</summary>
    Beginner = 0,
    Intermediate = 1,
    Advanced = 2,
}
```

- [ ] **Step 4: Write `User.cs`**

`backend/src/CCE.Domain/Identity/User.cs`:

```csharp
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
```

- [ ] **Step 5: Build + run tests**

```bash
dotnet build backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore 2>&1 | tail -6
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-build --no-restore --filter "FullyQualifiedName~UserDefaultsTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:     6`.

- [ ] **Step 6: Commit**

```bash
git add backend/src/CCE.Domain/Identity/KnowledgeLevel.cs backend/src/CCE.Domain/Identity/User.cs backend/tests/CCE.Domain.Tests/Identity/UserDefaultsTests.cs
git -c commit.gpgsign=false commit -m "feat(identity): User extends IdentityUser<Guid> with CCE profile fields + defaults (6 TDD tests)"
```

---

## Task 2.3: `User` mutators + locale invariant + tests

**Files:**
- Modify: `backend/src/CCE.Domain/Identity/User.cs`
- Create: `backend/tests/CCE.Domain.Tests/Identity/UserMutationTests.cs`

**Rationale:** Setters on User are public for ASP.NET Identity's UserManager-driven flow, but business mutations (locale change, interests update) should go through methods that enforce invariants. We replace public setters for `LocalePreference`, `Interests`, and `KnowledgeLevel` with private setters + dedicated mutator methods.

The single hard invariant is locale: only `"ar"` and `"en"` are allowed (UI is bilingual; spec §3.1 of foundation design). A wrong locale silently breaks the UI.

- [ ] **Step 1: Write failing tests**

`backend/tests/CCE.Domain.Tests/Identity/UserMutationTests.cs`:

```csharp
using CCE.Domain.Common;
using CCE.Domain.Identity;

namespace CCE.Domain.Tests.Identity;

public class UserMutationTests
{
    [Fact]
    public void SetLocalePreference_accepts_ar()
    {
        var user = new User();
        user.SetLocalePreference("ar");
        user.LocalePreference.Should().Be("ar");
    }

    [Fact]
    public void SetLocalePreference_accepts_en()
    {
        var user = new User();
        user.SetLocalePreference("en");
        user.LocalePreference.Should().Be("en");
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("AR")]
    [InlineData("")]
    [InlineData("  ")]
    public void SetLocalePreference_rejects_anything_else(string invalid)
    {
        var user = new User();
        var act = () => user.SetLocalePreference(invalid);
        act.Should().Throw<DomainException>().WithMessage("*locale*");
    }

    [Fact]
    public void SetLocalePreference_rejects_null()
    {
        var user = new User();
        var act = () => user.SetLocalePreference(null!);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetKnowledgeLevel_updates_field()
    {
        var user = new User();
        user.SetKnowledgeLevel(KnowledgeLevel.Advanced);
        user.KnowledgeLevel.Should().Be(KnowledgeLevel.Advanced);
    }

    [Fact]
    public void UpdateInterests_replaces_list()
    {
        var user = new User();
        user.UpdateInterests(new[] { "Solar", "Wind" });
        user.Interests.Should().Equal("Solar", "Wind");
    }

    [Fact]
    public void UpdateInterests_with_null_throws()
    {
        var user = new User();
        var act = () => user.UpdateInterests(null!);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateInterests_deduplicates_and_trims()
    {
        var user = new User();
        user.UpdateInterests(new[] { " Solar ", "Solar", "Wind", "" });
        user.Interests.Should().Equal("Solar", "Wind");
    }

    [Fact]
    public void AssignCountry_sets_id()
    {
        var user = new User();
        var country = System.Guid.NewGuid();
        user.AssignCountry(country);
        user.CountryId.Should().Be(country);
    }

    [Fact]
    public void ClearCountry_sets_null()
    {
        var user = new User { CountryId = System.Guid.NewGuid() };
        user.ClearCountry();
        user.CountryId.Should().BeNull();
    }

    [Fact]
    public void SetAvatarUrl_accepts_https_url()
    {
        var user = new User();
        user.SetAvatarUrl("https://cdn.example/avatar.png");
        user.AvatarUrl.Should().Be("https://cdn.example/avatar.png");
    }

    [Fact]
    public void SetAvatarUrl_rejects_non_https()
    {
        var user = new User();
        var act = () => user.SetAvatarUrl("http://insecure.example/x.png");
        act.Should().Throw<DomainException>().WithMessage("*https*");
    }

    [Fact]
    public void SetAvatarUrl_with_null_clears_value()
    {
        var user = new User();
        user.SetAvatarUrl("https://cdn.example/a.png");
        user.SetAvatarUrl(null);
        user.AvatarUrl.Should().BeNull();
    }
}
```

- [ ] **Step 2: Run — expect compile errors**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~UserMutationTests" 2>&1 | tail -8
```

Expected: build error referencing `SetLocalePreference`, `SetKnowledgeLevel`, `UpdateInterests`, `AssignCountry`, `ClearCountry`, `SetAvatarUrl` not found.

- [ ] **Step 3: Replace `User.cs` with the mutator-driven version**

`backend/src/CCE.Domain/Identity/User.cs`:

```csharp
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
```

- [ ] **Step 4: Build + run tests**

```bash
dotnet build backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore 2>&1 | tail -6
```

Expected: 0 errors, 0 warnings.

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-build --no-restore --filter "FullyQualifiedName~Identity" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:    21` (6 Defaults + 15 Mutation = wait, count: 4 locale + 1 null locale + 1 knowledge + 4 interests (1 replaces, 1 null throws, 1 dedup) — 3 actually for Interests — let me recount tests in file: `accepts_ar` + `accepts_en` + `rejects_anything_else (4 inline)` + `rejects_null` + `SetKnowledgeLevel` + `UpdateInterests_replaces_list` + `UpdateInterests_with_null_throws` + `UpdateInterests_deduplicates_and_trims` + `AssignCountry` + `ClearCountry` + `SetAvatarUrl_accepts_https_url` + `SetAvatarUrl_rejects_non_https` + `SetAvatarUrl_with_null_clears_value` = 1+1+4+1+1+1+1+1+1+1+1+1+1 = 16 mutation tests. Plus 6 defaults = 22 total in Identity folder).

If your count differs by 1–2, that's fine — count is informational. STOP only on failures, not count drift.

- [ ] **Step 5: Commit**

```bash
git add backend/src/CCE.Domain/Identity/User.cs backend/tests/CCE.Domain.Tests/Identity/UserMutationTests.cs
git -c commit.gpgsign=false commit -m "feat(identity): User mutators with locale + avatar invariants (16 TDD tests)"
```

---

## Task 2.4: `Role` entity (extends `IdentityRole<Guid>`)

**Files:**
- Create: `backend/src/CCE.Domain/Identity/Role.cs`
- Create: `backend/tests/CCE.Domain.Tests/Identity/RoleTests.cs`

**Rationale:** Role is a thin alias around `IdentityRole<Guid>`. The 6 known role names are already a domain decision (encoded in `RolePermissionMap` from Phase 01). This task just makes the type exist so EF Core's `IdentityDbContext<User, Role, Guid>` (Phase 07) wires correctly.

- [ ] **Step 1: Write failing tests**

`backend/tests/CCE.Domain.Tests/Identity/RoleTests.cs`:

```csharp
using CCE.Domain.Identity;

namespace CCE.Domain.Tests.Identity;

public class RoleTests
{
    [Fact]
    public void Role_inherits_IdentityRole_of_Guid()
    {
        var role = new Role();
        role.Should().BeAssignableTo<Microsoft.AspNetCore.Identity.IdentityRole<System.Guid>>();
    }

    [Fact]
    public void Role_constructed_with_name_sets_Name()
    {
        var role = new Role("SuperAdmin");
        role.Name.Should().Be("SuperAdmin");
    }

    [Fact]
    public void Role_default_constructor_leaves_name_null()
    {
        var role = new Role();
        role.Name.Should().BeNull();
    }
}
```

- [ ] **Step 2: Run — expect compile errors**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~RoleTests" 2>&1 | tail -8
```

Expected: build error referencing `Role` not found.

- [ ] **Step 3: Write `Role.cs`**

`backend/src/CCE.Domain/Identity/Role.cs`:

```csharp
using CCE.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace CCE.Domain.Identity;

/// <summary>
/// CCE role — thin specialization of <see cref="IdentityRole{TKey}"/> with <see cref="System.Guid"/>
/// keys. The seeded role names are listed in <c>backend/permissions.yaml</c> and reflected in
/// <c>CCE.Domain.RolePermissionMap</c>: SuperAdmin, ContentManager, StateRepresentative,
/// CommunityExpert, RegisteredUser. (<c>Anonymous</c> is NOT seeded — it's an implicit role
/// representing unauthenticated callers.)
/// </summary>
[Audited]
public class Role : IdentityRole<System.Guid>
{
    public Role() { }

    public Role(string roleName) : base(roleName) { }
}
```

- [ ] **Step 4: Build + run tests**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~RoleTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:     3`.

- [ ] **Step 5: Commit**

```bash
git add backend/src/CCE.Domain/Identity/Role.cs backend/tests/CCE.Domain.Tests/Identity/RoleTests.cs
git -c commit.gpgsign=false commit -m "feat(identity): Role extends IdentityRole<Guid> (3 TDD tests)"
```

---

## Task 2.5: `StateRepresentativeAssignment` entity + Revoke invariants

**Files:**
- Create: `backend/src/CCE.Domain/Identity/StateRepresentativeAssignment.cs`
- Create: `backend/tests/CCE.Domain.Tests/Identity/StateRepresentativeAssignmentTests.cs`

**Rationale:** Tracks which state representative is assigned to which country. Soft-deletable: revoking an assignment marks it deleted but preserves history. The "unique active assignment per (UserId, CountryId)" invariant is enforced at the persistence layer (filtered unique index in Phase 08) — the domain entity enforces single-revocation and tracks revoke metadata.

- [ ] **Step 1: Write failing tests**

`backend/tests/CCE.Domain.Tests/Identity/StateRepresentativeAssignmentTests.cs`:

```csharp
using CCE.Domain.Common;
using CCE.Domain.Identity;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Identity;

public class StateRepresentativeAssignmentTests
{
    private static FakeSystemClock NewClock() => new();

    [Fact]
    public void Assign_factory_sets_required_fields()
    {
        var clock = NewClock();
        var userId = System.Guid.NewGuid();
        var countryId = System.Guid.NewGuid();
        var adminId = System.Guid.NewGuid();

        var a = StateRepresentativeAssignment.Assign(userId, countryId, adminId, clock);

        a.Id.Should().NotBe(System.Guid.Empty);
        a.UserId.Should().Be(userId);
        a.CountryId.Should().Be(countryId);
        a.AssignedById.Should().Be(adminId);
        a.AssignedOn.Should().Be(clock.UtcNow);
        a.RevokedOn.Should().BeNull();
        a.RevokedById.Should().BeNull();
        a.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Assign_with_empty_userId_throws()
    {
        var clock = NewClock();
        var act = () => StateRepresentativeAssignment.Assign(System.Guid.Empty, System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>().WithMessage("*UserId*");
    }

    [Fact]
    public void Assign_with_empty_countryId_throws()
    {
        var clock = NewClock();
        var act = () => StateRepresentativeAssignment.Assign(System.Guid.NewGuid(), System.Guid.Empty, System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>().WithMessage("*CountryId*");
    }

    [Fact]
    public void Assign_with_empty_assignedById_throws()
    {
        var clock = NewClock();
        var act = () => StateRepresentativeAssignment.Assign(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.Empty, clock);
        act.Should().Throw<DomainException>().WithMessage("*AssignedById*");
    }

    [Fact]
    public void Revoke_sets_revoke_fields_and_marks_deleted()
    {
        var clock = NewClock();
        var a = StateRepresentativeAssignment.Assign(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        clock.Advance(System.TimeSpan.FromHours(2));
        var revoker = System.Guid.NewGuid();

        a.Revoke(revoker, clock);

        a.RevokedOn.Should().Be(clock.UtcNow);
        a.RevokedById.Should().Be(revoker);
        a.IsDeleted.Should().BeTrue();
        a.DeletedOn.Should().Be(clock.UtcNow);
        a.DeletedById.Should().Be(revoker);
    }

    [Fact]
    public void Revoking_already_revoked_assignment_throws()
    {
        var clock = NewClock();
        var a = StateRepresentativeAssignment.Assign(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        a.Revoke(System.Guid.NewGuid(), clock);
        clock.Advance(System.TimeSpan.FromMinutes(5));

        var act = () => a.Revoke(System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>().WithMessage("*already revoked*");
    }
}
```

- [ ] **Step 2: Run — expect compile errors**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~StateRepresentativeAssignmentTests" 2>&1 | tail -8
```

Expected: build error referencing `StateRepresentativeAssignment` not found.

- [ ] **Step 3: Write `StateRepresentativeAssignment.cs`**

`backend/src/CCE.Domain/Identity/StateRepresentativeAssignment.cs`:

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Identity;

/// <summary>
/// Persistent record of a state-representative role assignment to a user, scoped by country.
/// Soft-deletable: revoking the assignment sets <see cref="RevokedOn"/>/<see cref="RevokedById"/>
/// AND marks the row deleted (so the unique-active-assignment filtered index ignores it).
/// </summary>
[Audited]
public sealed class StateRepresentativeAssignment : Entity<System.Guid>, ISoftDeletable
{
    private StateRepresentativeAssignment(
        System.Guid id,
        System.Guid userId,
        System.Guid countryId,
        System.Guid assignedById,
        System.DateTimeOffset assignedOn) : base(id)
    {
        UserId = userId;
        CountryId = countryId;
        AssignedById = assignedById;
        AssignedOn = assignedOn;
    }

    /// <summary>FK to <see cref="User.Id"/>.</summary>
    public System.Guid UserId { get; private set; }

    /// <summary>FK to <c>Country.Id</c>.</summary>
    public System.Guid CountryId { get; private set; }

    /// <summary>UTC moment the assignment was created.</summary>
    public System.DateTimeOffset AssignedOn { get; private set; }

    /// <summary>Admin <see cref="User.Id"/> who created the assignment.</summary>
    public System.Guid AssignedById { get; private set; }

    /// <summary>UTC moment the assignment was revoked; null if still active.</summary>
    public System.DateTimeOffset? RevokedOn { get; private set; }

    /// <summary>Admin <see cref="User.Id"/> who revoked; null if still active.</summary>
    public System.Guid? RevokedById { get; private set; }

    /// <inheritdoc />
    public bool IsDeleted { get; private set; }

    /// <inheritdoc />
    public System.DateTimeOffset? DeletedOn { get; private set; }

    /// <inheritdoc />
    public System.Guid? DeletedById { get; private set; }

    /// <summary>
    /// Factory: create a new active assignment. The "unique active per (User, Country)" invariant
    /// is checked at the persistence layer (Phase 08 filtered unique index).
    /// </summary>
    public static StateRepresentativeAssignment Assign(
        System.Guid userId,
        System.Guid countryId,
        System.Guid assignedById,
        ISystemClock clock)
    {
        if (userId == System.Guid.Empty)
        {
            throw new DomainException("UserId is required.");
        }
        if (countryId == System.Guid.Empty)
        {
            throw new DomainException("CountryId is required.");
        }
        if (assignedById == System.Guid.Empty)
        {
            throw new DomainException("AssignedById is required.");
        }
        return new StateRepresentativeAssignment(
            id: System.Guid.NewGuid(),
            userId: userId,
            countryId: countryId,
            assignedById: assignedById,
            assignedOn: clock.UtcNow);
    }

    /// <summary>
    /// Revoke this assignment. Sets <see cref="RevokedOn"/>/<see cref="RevokedById"/> and marks the
    /// row soft-deleted. Throws if already revoked.
    /// </summary>
    public void Revoke(System.Guid revokedById, ISystemClock clock)
    {
        if (IsDeleted || RevokedOn is not null)
        {
            throw new DomainException("Assignment is already revoked.");
        }
        if (revokedById == System.Guid.Empty)
        {
            throw new DomainException("RevokedById is required.");
        }
        var now = clock.UtcNow;
        RevokedOn = now;
        RevokedById = revokedById;
        IsDeleted = true;
        DeletedOn = now;
        DeletedById = revokedById;
    }
}
```

- [ ] **Step 4: Build + run tests**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~StateRepresentativeAssignmentTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:     6`.

- [ ] **Step 5: Commit**

```bash
git add backend/src/CCE.Domain/Identity/StateRepresentativeAssignment.cs backend/tests/CCE.Domain.Tests/Identity/StateRepresentativeAssignmentTests.cs
git -c commit.gpgsign=false commit -m "feat(identity): StateRepresentativeAssignment entity with Revoke invariants (6 TDD tests)"
```

---

## Task 2.6: `ExpertRegistrationRequest` aggregate root + state machine + domain events

**Files:**
- Create: `backend/src/CCE.Domain/Identity/ExpertRegistrationStatus.cs`
- Create: `backend/src/CCE.Domain/Identity/Events/ExpertRegistrationApprovedEvent.cs`
- Create: `backend/src/CCE.Domain/Identity/Events/ExpertRegistrationRejectedEvent.cs`
- Create: `backend/src/CCE.Domain/Identity/ExpertRegistrationRequest.cs`
- Create: `backend/tests/CCE.Domain.Tests/Identity/ExpertRegistrationRequestTests.cs`

**Rationale:** This is the workflow that turns a registered user into a community expert. State machine: `Pending → Approved` or `Pending → Rejected` (terminal). Approving raises an `ExpertRegistrationApprovedEvent`; an in-process handler in Phase 07 reacts by creating an `ExpertProfile` (Task 2.7 below).

- [ ] **Step 1: Write the status enum**

`backend/src/CCE.Domain/Identity/ExpertRegistrationStatus.cs`:

```csharp
namespace CCE.Domain.Identity;

/// <summary>
/// Lifecycle status of an <see cref="ExpertRegistrationRequest"/>.
/// </summary>
public enum ExpertRegistrationStatus
{
    /// <summary>Awaiting admin review. Initial state.</summary>
    Pending = 0,

    /// <summary>Admin approved; an <see cref="ExpertProfile"/> was created. Terminal.</summary>
    Approved = 1,

    /// <summary>Admin rejected; rejection reason recorded. Terminal.</summary>
    Rejected = 2,
}
```

- [ ] **Step 2: Write the domain events**

`backend/src/CCE.Domain/Identity/Events/ExpertRegistrationApprovedEvent.cs`:

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Identity.Events;

/// <summary>
/// Raised when an <see cref="ExpertRegistrationRequest"/> is approved. Phase 07's
/// DomainEventDispatcher routes it to a handler that creates the <see cref="ExpertProfile"/>.
/// </summary>
public sealed record ExpertRegistrationApprovedEvent(
    System.Guid RequestId,
    System.Guid RequestedById,
    System.Guid ApprovedById,
    string RequestedBioAr,
    string RequestedBioEn,
    IReadOnlyList<string> RequestedTags,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
```

`backend/src/CCE.Domain/Identity/Events/ExpertRegistrationRejectedEvent.cs`:

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Identity.Events;

/// <summary>
/// Raised when an <see cref="ExpertRegistrationRequest"/> is rejected. Phase 07's
/// DomainEventDispatcher routes it to a notification handler that emails the requester.
/// </summary>
public sealed record ExpertRegistrationRejectedEvent(
    System.Guid RequestId,
    System.Guid RequestedById,
    System.Guid RejectedById,
    string RejectionReasonAr,
    string RejectionReasonEn,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
```

- [ ] **Step 3: Write failing tests**

`backend/tests/CCE.Domain.Tests/Identity/ExpertRegistrationRequestTests.cs`:

```csharp
using CCE.Domain.Common;
using CCE.Domain.Identity;
using CCE.Domain.Identity.Events;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Identity;

public class ExpertRegistrationRequestTests
{
    private static FakeSystemClock NewClock() => new();

    private static ExpertRegistrationRequest NewPending(FakeSystemClock clock) =>
        ExpertRegistrationRequest.Submit(
            requesterId: System.Guid.NewGuid(),
            bioAr: "خبير",
            bioEn: "Expert",
            tags: new[] { "Solar", "Storage" },
            clock: clock);

    [Fact]
    public void Submit_factory_creates_pending_request()
    {
        var clock = NewClock();
        var req = NewPending(clock);

        req.Status.Should().Be(ExpertRegistrationStatus.Pending);
        req.RequestedBioAr.Should().Be("خبير");
        req.RequestedBioEn.Should().Be("Expert");
        req.RequestedTags.Should().Equal("Solar", "Storage");
        req.ProcessedOn.Should().BeNull();
        req.ProcessedById.Should().BeNull();
        req.RejectionReasonAr.Should().BeNull();
        req.RejectionReasonEn.Should().BeNull();
        req.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Submit_with_empty_bios_throws()
    {
        var clock = NewClock();
        var act1 = () => ExpertRegistrationRequest.Submit(System.Guid.NewGuid(), "", "Expert", new[] { "x" }, clock);
        var act2 = () => ExpertRegistrationRequest.Submit(System.Guid.NewGuid(), "خبير", "", new[] { "x" }, clock);
        act1.Should().Throw<DomainException>();
        act2.Should().Throw<DomainException>();
    }

    [Fact]
    public void Submit_with_empty_requesterId_throws()
    {
        var clock = NewClock();
        var act = () => ExpertRegistrationRequest.Submit(System.Guid.Empty, "خبير", "Expert", new[] { "x" }, clock);
        act.Should().Throw<DomainException>().WithMessage("*RequesterId*");
    }

    [Fact]
    public void Approve_transitions_to_Approved_and_records_processor()
    {
        var clock = NewClock();
        var req = NewPending(clock);
        clock.Advance(System.TimeSpan.FromHours(1));
        var admin = System.Guid.NewGuid();

        req.Approve(admin, clock);

        req.Status.Should().Be(ExpertRegistrationStatus.Approved);
        req.ProcessedById.Should().Be(admin);
        req.ProcessedOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void Approve_raises_ExpertRegistrationApprovedEvent()
    {
        var clock = NewClock();
        var req = NewPending(clock);
        var admin = System.Guid.NewGuid();
        req.Approve(admin, clock);

        req.DomainEvents.Should().HaveCount(1);
        var evt = req.DomainEvents.OfType<ExpertRegistrationApprovedEvent>().Single();
        evt.RequestId.Should().Be(req.Id);
        evt.ApprovedById.Should().Be(admin);
        evt.RequestedTags.Should().Equal("Solar", "Storage");
    }

    [Fact]
    public void Reject_transitions_to_Rejected_and_records_reasons()
    {
        var clock = NewClock();
        var req = NewPending(clock);
        var admin = System.Guid.NewGuid();

        req.Reject(admin, "سبب", "Reason", clock);

        req.Status.Should().Be(ExpertRegistrationStatus.Rejected);
        req.ProcessedById.Should().Be(admin);
        req.RejectionReasonAr.Should().Be("سبب");
        req.RejectionReasonEn.Should().Be("Reason");
    }

    [Fact]
    public void Reject_raises_ExpertRegistrationRejectedEvent()
    {
        var clock = NewClock();
        var req = NewPending(clock);
        var admin = System.Guid.NewGuid();
        req.Reject(admin, "سبب", "Reason", clock);

        req.DomainEvents.OfType<ExpertRegistrationRejectedEvent>().Should().HaveCount(1);
    }

    [Fact]
    public void Approving_already_processed_request_throws()
    {
        var clock = NewClock();
        var req = NewPending(clock);
        req.Approve(System.Guid.NewGuid(), clock);

        var act = () => req.Approve(System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>().WithMessage("*Pending*");
    }

    [Fact]
    public void Rejecting_already_processed_request_throws()
    {
        var clock = NewClock();
        var req = NewPending(clock);
        req.Reject(System.Guid.NewGuid(), "ا", "a", clock);

        var act = () => req.Reject(System.Guid.NewGuid(), "ب", "b", clock);
        act.Should().Throw<DomainException>().WithMessage("*Pending*");
    }

    [Fact]
    public void Approving_after_rejection_throws()
    {
        var clock = NewClock();
        var req = NewPending(clock);
        req.Reject(System.Guid.NewGuid(), "ا", "a", clock);

        var act = () => req.Approve(System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reject_with_empty_reason_throws()
    {
        var clock = NewClock();
        var req = NewPending(clock);

        var act = () => req.Reject(System.Guid.NewGuid(), "", "", clock);
        act.Should().Throw<DomainException>();
    }
}
```

- [ ] **Step 4: Run — expect compile errors**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~ExpertRegistrationRequestTests" 2>&1 | tail -8
```

Expected: build error referencing `ExpertRegistrationRequest` not found.

- [ ] **Step 5: Write `ExpertRegistrationRequest.cs`**

`backend/src/CCE.Domain/Identity/ExpertRegistrationRequest.cs`:

```csharp
using CCE.Domain.Common;
using CCE.Domain.Identity.Events;

namespace CCE.Domain.Identity;

/// <summary>
/// Workflow record for a registered user requesting expert status. Aggregate root —
/// the <see cref="Approve"/> transition raises an <see cref="ExpertRegistrationApprovedEvent"/>
/// that Phase 07's domain-event dispatcher routes to an in-process handler which creates
/// the corresponding <see cref="ExpertProfile"/>. Soft-deletable for admin recovery flows.
/// </summary>
[Audited]
public sealed class ExpertRegistrationRequest : AggregateRoot<System.Guid>, ISoftDeletable
{
    private ExpertRegistrationRequest(
        System.Guid id,
        System.Guid requestedById,
        string requestedBioAr,
        string requestedBioEn,
        IReadOnlyList<string> requestedTags,
        System.DateTimeOffset submittedOn) : base(id)
    {
        RequestedById = requestedById;
        RequestedBioAr = requestedBioAr;
        RequestedBioEn = requestedBioEn;
        RequestedTags = requestedTags;
        SubmittedOn = submittedOn;
        Status = ExpertRegistrationStatus.Pending;
    }

    public System.Guid RequestedById { get; private set; }

    public string RequestedBioAr { get; private set; } = string.Empty;

    public string RequestedBioEn { get; private set; } = string.Empty;

    public IReadOnlyList<string> RequestedTags { get; private set; } = System.Array.Empty<string>();

    public System.DateTimeOffset SubmittedOn { get; private set; }

    public ExpertRegistrationStatus Status { get; private set; }

    public System.Guid? ProcessedById { get; private set; }

    public System.DateTimeOffset? ProcessedOn { get; private set; }

    public string? RejectionReasonAr { get; private set; }

    public string? RejectionReasonEn { get; private set; }

    public bool IsDeleted { get; private set; }

    public System.DateTimeOffset? DeletedOn { get; private set; }

    public System.Guid? DeletedById { get; private set; }

    /// <summary>
    /// Submit a new pending registration request. Validates inputs and records the submission moment.
    /// </summary>
    public static ExpertRegistrationRequest Submit(
        System.Guid requesterId,
        string bioAr,
        string bioEn,
        IEnumerable<string> tags,
        ISystemClock clock)
    {
        if (requesterId == System.Guid.Empty)
        {
            throw new DomainException("RequesterId is required.");
        }
        if (string.IsNullOrWhiteSpace(bioAr))
        {
            throw new DomainException("Arabic bio is required.");
        }
        if (string.IsNullOrWhiteSpace(bioEn))
        {
            throw new DomainException("English bio is required.");
        }
        var tagList = (tags ?? throw new DomainException("Tags collection is required."))
            .Select(static s => s?.Trim() ?? string.Empty)
            .Where(static s => s.Length > 0)
            .Distinct()
            .ToList();
        return new ExpertRegistrationRequest(
            id: System.Guid.NewGuid(),
            requestedById: requesterId,
            requestedBioAr: bioAr,
            requestedBioEn: bioEn,
            requestedTags: tagList,
            submittedOn: clock.UtcNow);
    }

    /// <summary>
    /// Admin approval transition. Allowed only from <see cref="ExpertRegistrationStatus.Pending"/>.
    /// Raises an <see cref="ExpertRegistrationApprovedEvent"/>.
    /// </summary>
    public void Approve(System.Guid approvedById, ISystemClock clock)
    {
        if (Status != ExpertRegistrationStatus.Pending)
        {
            throw new DomainException($"Cannot approve a {Status} request — only Pending allowed.");
        }
        if (approvedById == System.Guid.Empty)
        {
            throw new DomainException("ApprovedById is required.");
        }
        var now = clock.UtcNow;
        Status = ExpertRegistrationStatus.Approved;
        ProcessedById = approvedById;
        ProcessedOn = now;
        RaiseDomainEvent(new ExpertRegistrationApprovedEvent(
            RequestId: Id,
            RequestedById: RequestedById,
            ApprovedById: approvedById,
            RequestedBioAr: RequestedBioAr,
            RequestedBioEn: RequestedBioEn,
            RequestedTags: RequestedTags,
            OccurredOn: now));
    }

    /// <summary>
    /// Admin rejection transition with bilingual reason. Allowed only from
    /// <see cref="ExpertRegistrationStatus.Pending"/>. Raises an
    /// <see cref="ExpertRegistrationRejectedEvent"/>.
    /// </summary>
    public void Reject(System.Guid rejectedById, string reasonAr, string reasonEn, ISystemClock clock)
    {
        if (Status != ExpertRegistrationStatus.Pending)
        {
            throw new DomainException($"Cannot reject a {Status} request — only Pending allowed.");
        }
        if (rejectedById == System.Guid.Empty)
        {
            throw new DomainException("RejectedById is required.");
        }
        if (string.IsNullOrWhiteSpace(reasonAr))
        {
            throw new DomainException("Arabic rejection reason is required.");
        }
        if (string.IsNullOrWhiteSpace(reasonEn))
        {
            throw new DomainException("English rejection reason is required.");
        }
        var now = clock.UtcNow;
        Status = ExpertRegistrationStatus.Rejected;
        ProcessedById = rejectedById;
        ProcessedOn = now;
        RejectionReasonAr = reasonAr;
        RejectionReasonEn = reasonEn;
        RaiseDomainEvent(new ExpertRegistrationRejectedEvent(
            RequestId: Id,
            RequestedById: RequestedById,
            RejectedById: rejectedById,
            RejectionReasonAr: reasonAr,
            RejectionReasonEn: reasonEn,
            OccurredOn: now));
    }
}
```

- [ ] **Step 6: Build + run tests**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~ExpertRegistrationRequestTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:    11`.

- [ ] **Step 7: Commit**

```bash
git add backend/src/CCE.Domain/Identity/ExpertRegistrationStatus.cs backend/src/CCE.Domain/Identity/Events/ backend/src/CCE.Domain/Identity/ExpertRegistrationRequest.cs backend/tests/CCE.Domain.Tests/Identity/ExpertRegistrationRequestTests.cs
git -c commit.gpgsign=false commit -m "feat(identity): ExpertRegistrationRequest aggregate + state machine + Approved/Rejected events (11 TDD tests)"
```

---

## Task 2.7: `ExpertProfile` entity + factory from approved request

**Files:**
- Create: `backend/src/CCE.Domain/Identity/ExpertProfile.cs`
- Create: `backend/tests/CCE.Domain.Tests/Identity/ExpertProfileTests.cs`

**Rationale:** `ExpertProfile` is the artifact created when an `ExpertRegistrationRequest` is approved. Static factory `CreateFromApprovedRequest` enforces the invariant: profiles only exist for approved requests. Mutators allow profile updates after creation.

- [ ] **Step 1: Write failing tests**

`backend/tests/CCE.Domain.Tests/Identity/ExpertProfileTests.cs`:

```csharp
using CCE.Domain.Common;
using CCE.Domain.Identity;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Identity;

public class ExpertProfileTests
{
    private static FakeSystemClock NewClock() => new();

    private static ExpertRegistrationRequest NewApproved(FakeSystemClock clock, out System.Guid approverId)
    {
        var req = ExpertRegistrationRequest.Submit(
            requesterId: System.Guid.NewGuid(),
            bioAr: "خبير الطاقة المتجددة",
            bioEn: "Renewable energy expert",
            tags: new[] { "Solar", "Wind" },
            clock: clock);
        approverId = System.Guid.NewGuid();
        req.Approve(approverId, clock);
        return req;
    }

    [Fact]
    public void CreateFromApprovedRequest_copies_request_fields()
    {
        var clock = NewClock();
        var req = NewApproved(clock, out var approverId);
        clock.Advance(System.TimeSpan.FromMinutes(5));

        var profile = ExpertProfile.CreateFromApprovedRequest(req, "د.", "Dr.", clock);

        profile.UserId.Should().Be(req.RequestedById);
        profile.BioAr.Should().Be("خبير الطاقة المتجددة");
        profile.BioEn.Should().Be("Renewable energy expert");
        profile.ExpertiseTags.Should().Equal("Solar", "Wind");
        profile.AcademicTitleAr.Should().Be("د.");
        profile.AcademicTitleEn.Should().Be("Dr.");
        profile.ApprovedOn.Should().Be(req.ProcessedOn!.Value);
        profile.ApprovedById.Should().Be(approverId);
        profile.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void CreateFromApprovedRequest_throws_when_request_is_pending()
    {
        var clock = NewClock();
        var pending = ExpertRegistrationRequest.Submit(
            System.Guid.NewGuid(), "ا", "a", new[] { "x" }, clock);

        var act = () => ExpertProfile.CreateFromApprovedRequest(pending, "د.", "Dr.", clock);
        act.Should().Throw<DomainException>().WithMessage("*Approved*");
    }

    [Fact]
    public void CreateFromApprovedRequest_throws_when_request_is_rejected()
    {
        var clock = NewClock();
        var rejected = ExpertRegistrationRequest.Submit(
            System.Guid.NewGuid(), "ا", "a", new[] { "x" }, clock);
        rejected.Reject(System.Guid.NewGuid(), "ر", "r", clock);

        var act = () => ExpertProfile.CreateFromApprovedRequest(rejected, "د.", "Dr.", clock);
        act.Should().Throw<DomainException>().WithMessage("*Approved*");
    }

    [Fact]
    public void UpdateBio_replaces_bilingual_bios()
    {
        var clock = NewClock();
        var req = NewApproved(clock, out _);
        var profile = ExpertProfile.CreateFromApprovedRequest(req, "د.", "Dr.", clock);

        profile.UpdateBio("نص جديد", "New text");

        profile.BioAr.Should().Be("نص جديد");
        profile.BioEn.Should().Be("New text");
    }

    [Fact]
    public void UpdateBio_with_empty_throws()
    {
        var clock = NewClock();
        var req = NewApproved(clock, out _);
        var profile = ExpertProfile.CreateFromApprovedRequest(req, "د.", "Dr.", clock);

        var act = () => profile.UpdateBio("", "");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateExpertiseTags_dedupes_and_trims()
    {
        var clock = NewClock();
        var req = NewApproved(clock, out _);
        var profile = ExpertProfile.CreateFromApprovedRequest(req, "د.", "Dr.", clock);

        profile.UpdateExpertiseTags(new[] { " Solar ", "solar", "Storage" });

        profile.ExpertiseTags.Should().Contain("Solar").And.Contain("Storage").And.Contain("solar");
        profile.ExpertiseTags.Should().HaveCount(3);
    }

    [Fact]
    public void UpdateAcademicTitle_replaces_titles()
    {
        var clock = NewClock();
        var req = NewApproved(clock, out _);
        var profile = ExpertProfile.CreateFromApprovedRequest(req, "د.", "Dr.", clock);

        profile.UpdateAcademicTitle("أستاذ", "Prof.");

        profile.AcademicTitleAr.Should().Be("أستاذ");
        profile.AcademicTitleEn.Should().Be("Prof.");
    }
}
```

- [ ] **Step 2: Run — expect compile errors**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~ExpertProfileTests" 2>&1 | tail -8
```

Expected: build error referencing `ExpertProfile` not found.

- [ ] **Step 3: Write `ExpertProfile.cs`**

`backend/src/CCE.Domain/Identity/ExpertProfile.cs`:

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Identity;

/// <summary>
/// Approved community-expert profile. Created exclusively via
/// <see cref="CreateFromApprovedRequest"/> from an approved
/// <see cref="ExpertRegistrationRequest"/>. The 1:1 link to <see cref="User"/> is
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
        IReadOnlyList<string> tags,
        string academicTitleAr,
        string academicTitleEn,
        System.DateTimeOffset approvedOn,
        System.Guid approvedById) : base(id)
    {
        UserId = userId;
        BioAr = bioAr;
        BioEn = bioEn;
        ExpertiseTags = tags;
        AcademicTitleAr = academicTitleAr;
        AcademicTitleEn = academicTitleEn;
        ApprovedOn = approvedOn;
        ApprovedById = approvedById;
    }

    public System.Guid UserId { get; private set; }

    public string BioAr { get; private set; } = string.Empty;

    public string BioEn { get; private set; } = string.Empty;

    public IReadOnlyList<string> ExpertiseTags { get; private set; } = System.Array.Empty<string>();

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
            tags: request.RequestedTags,
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
```

- [ ] **Step 4: Build + run tests**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~ExpertProfileTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:     7`.

- [ ] **Step 5: Commit**

```bash
git add backend/src/CCE.Domain/Identity/ExpertProfile.cs backend/tests/CCE.Domain.Tests/Identity/ExpertProfileTests.cs
git -c commit.gpgsign=false commit -m "feat(identity): ExpertProfile entity with CreateFromApprovedRequest factory (7 TDD tests)"
```

---

## Task 2.8: Phase 02 close — full backend run + progress doc

**Files:**
- Modify: `docs/subprojects/02-data-domain-progress.md`

**Rationale:** Validate the cumulative test count across the whole backend, then mark Phase 02 done.

- [ ] **Step 1: Full backend test run**

```bash
dotnet test backend/CCE.sln --nologo --no-restore --logger "console;verbosity=minimal" 2>&1 | grep -E "(Passed!|Failed!)"
```

Expected (5 lines, all `Passed!`):

```
Passed!  - Failed:     0, Passed:    72, ... CCE.Domain.Tests.dll
Passed!  - Failed:     0, Passed:    12, ... CCE.Application.Tests.dll
Passed!  - Failed:     0, Passed:    28, ... CCE.Api.IntegrationTests.dll
Passed!  - Failed:     0, Passed:     6, ... CCE.Infrastructure.Tests.dll
Passed!  - Failed:     0, Passed:    10, ... CCE.Domain.SourceGenerators.Tests.dll
```

Domain.Tests breakdown: 29 (Phase 01) + 6 UserDefaults + 16 UserMutation + 3 Role + 6 StateRep + 11 ExpertRequest + 7 ExpertProfile = 78. (Note: small drift is OK because of how `[Theory]` rows count; the headline is "all green, no failures, > 70".)

Backend total ≈ 72 + 12 + 28 + 6 + 10 = 128. (Use the actual numbers reported.)

- [ ] **Step 2: Update progress doc**

Open `docs/subprojects/02-data-domain-progress.md`. Replace:

```markdown
| 02 | Identity | ⏳ Pending |
```

with:

```markdown
| 02 | Identity | ✅ Done |
```

Replace the test totals table with the actual numbers from Step 1. For example:

```markdown
| Layer | At start | Current | Target |
|---|---|---|---|
| Domain | 16 | 78 | ~136 |
| Application | 12 | 12 | ~72 |
| Infrastructure | 6 | 6 | ~46 |
| Architecture | 0 | 0 | ~15 |
| Source generator | 0 | 10 | ~20 |
| Api Integration | 28 | 28 | ~38 |
| **Cumulative** | **62** (backend) | **134** | **~327** (backend) |
```

(Use the EXACT numbers reported by Step 1 — don't trust the example.)

- [ ] **Step 3: Commit**

```bash
git add docs/subprojects/02-data-domain-progress.md
git -c commit.gpgsign=false commit -m "docs(sub-2): mark Phase 02 done; update test totals (Identity bounded context shipped)"
```

---

## Phase 02 — completion checklist

- [ ] `Microsoft.Extensions.Identity.Stores 8.0.10` pinned in CPM and referenced from Domain.
- [ ] 5 entities exist under `backend/src/CCE.Domain/Identity/`: `User`, `Role`, `StateRepresentativeAssignment`, `ExpertRegistrationRequest`, `ExpertProfile`.
- [ ] `KnowledgeLevel`, `ExpertRegistrationStatus` enums exist.
- [ ] 2 domain events exist under `Identity/Events/`: `ExpertRegistrationApprovedEvent`, `ExpertRegistrationRejectedEvent`.
- [ ] `User.SetLocalePreference` rejects anything other than `"ar"` / `"en"`.
- [ ] `User.SetAvatarUrl` rejects non-https URLs.
- [ ] `StateRepresentativeAssignment.Revoke` cannot be called twice.
- [ ] `ExpertRegistrationRequest.Approve` and `Reject` allowed only from `Pending`.
- [ ] `ExpertProfile.CreateFromApprovedRequest` rejects non-Approved requests.
- [ ] Approving a request raises `ExpertRegistrationApprovedEvent`.
- [ ] Rejecting a request raises `ExpertRegistrationRejectedEvent`.
- [ ] `dotnet build backend/CCE.sln` 0 errors / 0 warnings.
- [ ] All Foundation regression tests still pass.
- [ ] `git status` clean.
- [ ] 8 new commits with the messages shown above.

**If all boxes ticked, Phase 02 is complete. Proceed to Phase 03 (Content bounded context — 8 entities).**
