# Merge `country_codes` → `countries` — Implementation Plan

**Goal:** One canonical `countries` table with an `is_cce_country` flag, replacing the parallel `country_codes` table. `User.CountryCodeId` is dropped and consolidated into `User.CountryId`.

---

## Context — What Exists Today

### `countries` table (CCE platform countries)
Fields: `Id`, `IsoAlpha3`, `IsoAlpha2`, `NameAr`, `NameEn`, `RegionAr`, `RegionEn`, `FlagUrl`, `LatestKapsarcSnapshotId`, `IsActive`
Referenced by **8 FK relationships** — none of these change.

### `country_codes` table (all world countries + dial codes)
Fields: `Id`, `Name.Ar`, `Name.En`, `DialCode`, `FlagUrl`, `IsActive`
Referenced by **1 FK** — `User.CountryCodeId`. This is the only thing that changes.

### After migration
- `countries` gains `dial_code` (nullable) and `is_cce_country` (bool)
- `country_codes` table is dropped
- `users.country_code_id` column is dropped
- `users.country_id` becomes the sole country FK everywhere

---

## FK Impact Map

| Entity | Column | Points To | After Migration |
|--------|--------|-----------|-----------------|
| `StateRepresentativeAssignment` | `country_id` | `countries` | ✅ Unchanged |
| `CountryProfile` | `country_id` | `countries` | ✅ Unchanged |
| `CountryKapsarcSnapshot` | `country_id` | `countries` | ✅ Unchanged |
| `CountryContentRequest` | `country_id` | `countries` | ✅ Unchanged |
| `Resource` | `country_id` | `countries` | ✅ Unchanged |
| `ResourceCountry` (join) | `country_id` | `countries` | ✅ Unchanged |
| `HomepageCountry` | `country_id` | `countries` | ✅ Unchanged |
| `User` | `country_id` | `countries` | ✅ Unchanged (geographic) |
| **User** | **`country_code_id`** | `country_codes` | ❌ Dropped — data migrated to `country_id` |

---

## User FK Conflict Resolution Rule

A user can have both columns set pointing to different countries (geographic vs phone nationality). Migration handles each case:

| User state | Action |
|------------|--------|
| `country_id = NULL`, `country_code_id = X` | Set `country_id` = mapped entry for X in merged table |
| `country_id = Y`, `country_code_id = NULL` | No change |
| `country_id = Y`, `country_code_id = X` (same country) | Drop `country_code_id`, no data loss |
| `country_id = Y`, `country_code_id = X` (**different** country) | Keep `country_id = Y` (geographic takes priority). Log conflicts before dropping column. |

> ⚠️ **Run Phase 0 conflict-detection query first.** If conflict count is significant, add a `phone_country_id` column to users instead of overloading `country_id`.

---

## Phase 0 — Pre-flight

1. Take a full DB backup.

2. Record baseline counts:
```sql
SELECT COUNT(*) FROM countries;
SELECT COUNT(*) FROM country_codes;
SELECT COUNT(*) FROM users WHERE country_code_id IS NOT NULL;
SELECT COUNT(*) FROM users WHERE country_id IS NOT NULL;
```

3. Run conflict-detection query:
```sql
-- Users with both FKs pointing to DIFFERENT countries
SELECT u.id, c.name_en AS geographic_country, cc.name_en AS phone_country
FROM   users u
JOIN   countries    c  ON c.id  = u.country_id
JOIN   country_codes cc ON cc.id = u.country_code_id
WHERE  u.country_id      IS NOT NULL
  AND  u.country_code_id IS NOT NULL
  AND  c.name_en <> cc.name_en;
```

4. If conflict count > 0, decide whether to add a `phone_country_id` column before proceeding. Update the plan accordingly.

---

## Phase 1 — Domain Layer

### 1a. Extend `Country` entity
**File:** `src/CCE.Domain/Country/Country.cs`

Add two new properties:
```csharp
public string? DialCode { get; private set; }
public bool IsCceCountry { get; private set; } = true;
```

Add a factory for dial-code-only (non-CCE) rows:
```csharp
public static Country RegisterLookup(
    Guid id, string nameAr, string nameEn, string dialCode, string? flagUrl)
{
    return new Country(id)
    {
        NameAr = nameAr, NameEn = nameEn,
        DialCode = dialCode, FlagUrl = flagUrl,
        IsCceCountry = false, IsActive = true
    };
}
```

Add a setter so admins can populate dial codes on existing CCE countries:
```csharp
public void SetDialCode(string? dialCode) => DialCode = dialCode;
```

Make `IsoAlpha3`, `IsoAlpha2`, `RegionAr`, `RegionEn` nullable in the private EF constructor so non-CCE rows (which have no ISO codes) can be materialised. The public `Register()` factory keeps its validation — ISO fields remain required for CCE countries.

### 1b. Update `User` entity
**File:** `src/CCE.Domain/Identity/User.cs`

Remove:
```csharp
// DELETE this property
public Guid? CountryCodeId { get; set; }
```

`CountryId` stays as-is — it now serves as the sole country reference.

### 1c. Mark `CountryCode` entity for deletion
**File:** `src/CCE.Domain/Lookups/CountryCode.cs`

Keep the file during Phase 3 (needed for EF to generate the DROP TABLE migration step), then delete once the migration is applied and verified.

---

## Phase 2 — EF Configuration & DbContext

### 2a. Update `CountryConfiguration.cs`
**File:** `src/CCE.Infrastructure/Persistence/Configurations/Country/CountryConfiguration.cs`

```csharp
// Add these to Configure():
builder.Property(c => c.DialCode).HasMaxLength(16).IsRequired(false);
builder.Property(c => c.IsCceCountry).IsRequired().HasDefaultValue(true);

// Relax required constraints so non-CCE rows can have NULLs:
builder.Property(c => c.IsoAlpha3).HasMaxLength(3).IsRequired(false);
builder.Property(c => c.IsoAlpha2).HasMaxLength(2).IsRequired(false);
builder.Property(c => c.RegionAr).HasMaxLength(128).IsRequired(false);
builder.Property(c => c.RegionEn).HasMaxLength(128).IsRequired(false);

// Add index for dial-code lookups:
builder.HasIndex(c => c.DialCode)
       .HasFilter("[dial_code] IS NOT NULL")
       .HasDatabaseName("ix_country_dial_code");

// Update the ISO unique index to only enforce on CCE countries:
// Change HasFilter from "is_deleted = 0" to "is_deleted = 0 AND is_cce_country = 1"
```

### 2b. Delete `CountryCodeConfiguration.cs`
**File:** `src/CCE.Infrastructure/Persistence/Configurations/Lookups/CountryCodeConfiguration.cs`

Delete after EF migration is applied and verified.

### 2c. Update `UserConfiguration.cs`
**File:** `src/CCE.Infrastructure/Persistence/Configurations/Identity/UserConfiguration.cs`

Remove:
```csharp
// DELETE this index declaration
builder.HasIndex(u => u.CountryCodeId).HasDatabaseName("ix_users_country_code_id");
```

### 2d. Update `ICceDbContext.cs`
**File:** `src/CCE.Application/Common/Interfaces/ICceDbContext.cs`

Remove:
```csharp
// DELETE this line
IQueryable<CountryCode> CountryCodes { get; }
```

`IQueryable<Country> Countries` stays unchanged.

---

## Phase 3 — EF Migration (SQL)

> ⚠️ Run each step against a dev DB and verify counts before applying to production.

### Step 1 — Extend the `countries` table
```sql
ALTER TABLE countries ADD dial_code       NVARCHAR(16) NULL;
ALTER TABLE countries ADD is_cce_country  BIT NOT NULL DEFAULT 1; -- existing rows = CCE

-- Relax NOT NULL on columns non-CCE rows won't have
ALTER TABLE countries ALTER COLUMN iso_alpha3 NVARCHAR(3)   NULL;
ALTER TABLE countries ALTER COLUMN iso_alpha2 NVARCHAR(2)   NULL;
ALTER TABLE countries ALTER COLUMN region_ar  NVARCHAR(128) NULL;
ALTER TABLE countries ALTER COLUMN region_en  NVARCHAR(128) NULL;
```

### Step 2 — Populate `dial_code` on existing CCE countries
```sql
-- Match by English name (most reliable shared key between the two tables)
UPDATE c
SET    c.dial_code = cc.dial_code
FROM   countries c
JOIN   country_codes cc ON cc.name_en = c.name_en
WHERE  c.is_cce_country = 1
  AND  c.dial_code IS NULL;

-- Verify: check how many CCE countries got a dial_code
SELECT COUNT(*) FROM countries WHERE is_cce_country = 1 AND dial_code IS NOT NULL;
```

### Step 3 — Build a temporary ID mapping table
```sql
-- Map every country_codes.id → the countries.id it corresponds to
CREATE TABLE #cc_map (
    cc_id      UNIQUEIDENTIFIER NOT NULL,
    country_id UNIQUEIDENTIFIER NOT NULL
);

-- Case A: country_codes row matches an existing CCE country by name_en
INSERT INTO #cc_map (cc_id, country_id)
SELECT cc.id, c.id
FROM   country_codes cc
JOIN   countries c ON c.name_en = cc.name_en;

-- Case B: no matching country — insert the country_codes row as a new non-CCE country
INSERT INTO countries (
    id, name_ar, name_en, flag_url, dial_code, is_cce_country, is_active,
    created_by_id, created_on, last_modified_by_id, last_modified_on, is_deleted
)
SELECT
    cc.id,                  -- keep same GUID so mapping below is trivial
    cc.name_ar, cc.name_en, cc.flag_url, cc.dial_code,
    0,                      -- is_cce_country = false
    cc.is_active,
    cc.created_by_id, cc.created_on,
    cc.last_modified_by_id, cc.last_modified_on,
    cc.is_deleted
FROM country_codes cc
WHERE NOT EXISTS (
    SELECT 1 FROM countries c WHERE c.name_en = cc.name_en
);

-- Add Case B rows to the mapping (same GUID used, so cc_id = country_id)
INSERT INTO #cc_map (cc_id, country_id)
SELECT cc.id, cc.id
FROM   country_codes cc
WHERE  NOT EXISTS (SELECT 1 FROM #cc_map m WHERE m.cc_id = cc.id);

-- SANITY CHECK: every country_codes row must have a mapping — must return 0
SELECT COUNT(*) FROM country_codes cc
WHERE NOT EXISTS (SELECT 1 FROM #cc_map m WHERE m.cc_id = cc.id);
```

### Step 4 — Migrate `users.country_code_id` → `users.country_id`
```sql
-- Only update users that have country_code_id but NO country_id (safe, no conflict)
UPDATE u
SET    u.country_id = m.country_id
FROM   users u
JOIN   #cc_map m ON m.cc_id = u.country_code_id
WHERE  u.country_code_id IS NOT NULL
  AND  u.country_id IS NULL;

-- CHECK: remaining users with country_code_id but still NULL country_id
-- These are the conflict users identified in Phase 0 (geographic country already set)
SELECT COUNT(*) FROM users
WHERE country_code_id IS NOT NULL AND country_id IS NULL;

DROP TABLE #cc_map;
```

### Step 5 — Drop old column and table
```sql
-- Drop index first
DROP INDEX IF EXISTS ix_users_country_code_id ON users;

-- Drop the column
ALTER TABLE users DROP COLUMN country_code_id;

-- Drop the now-redundant table
DROP TABLE country_codes;

-- Add dial_code index to countries
CREATE INDEX ix_country_dial_code
    ON countries (dial_code)
    WHERE dial_code IS NOT NULL;
```

---

## Phase 4 — Application & API Layer

22 files reference `CountryCodeId` or `CountryCodes`. Changes by category:

### Registration & profile commands
| File | Change |
|------|--------|
| `Identity/Auth/Register/RegisterUserCommand.cs` | Replace `CountryCodeId` param with `CountryId` |
| `Identity/Auth/Register/RegisterUserCommandHandler.cs` | Set `user.CountryId` instead of `user.CountryCodeId` |
| `Identity/Public/Commands/UpdateMyProfile/UpdateMyProfileRequest.cs` | Remove `CountryCodeId`; `CountryId` handles both |
| `Identity/Public/Commands/UpdateMyProfile/UpdateMyProfileCommand.cs` | Same |
| `Identity/Public/Commands/UpdateMyProfile/UpdateMyProfileCommandHandler.cs` | Same |
| `Identity/Commands/CreateUser/CreateUserCommand.cs` | Replace `CountryCodeId` with `CountryId` |
| `Identity/Commands/CreateUser/CreateUserCommandHandler.cs` | Same |

### Phone change flow
| File | Change |
|------|--------|
| `Identity/Public/Commands/RequestPhoneChange/RequestPhoneChangeRequest.cs` | Remove `CountryCodeId` |
| `Identity/Public/Commands/RequestPhoneChange/RequestPhoneChangeCommand.cs` | Same |
| `Identity/Public/Commands/RequestPhoneChange/RequestPhoneChangeCommandHandler.cs` | Look up `DialCode` via `Countries.Where(c => c.Id == user.CountryId).Select(c => c.DialCode)` |
| `Identity/Public/Commands/ConfirmPhoneChange/ConfirmPhoneChangeCommandHandler.cs` | Same pattern |

### DTOs & queries
| File | Change |
|------|--------|
| `Identity/Public/Dtos/UserProfileDto.cs` | Remove `CountryCodeId`; expose `DialCode` from Country join |
| `Identity/Dtos/UserDetailDto.cs` | Same |
| `Identity/Queries/GetUserById/GetUserByIdQueryHandler.cs` | Remove CountryCodes join; add `.dial_code` from Countries join |
| `Identity/Public/Queries/GetMyProfile/GetMyProfileQueryHandler.cs` | Same |
| `Identity/Commands/DeleteUser/DeleteUserCommandHandler.cs` | Remove any `CountryCodeId` reference |

### Lookup queries (repoint from `CountryCodes` to `Countries`)
| File | Change |
|------|--------|
| `Lookups/Queries/ListCountryCodes/ListCountryCodesQueryHandler.cs` | Query `Countries` filtered by `dial_code IS NOT NULL` |
| `Lookups/Queries/ListCountryCodes/ListCountryCodesQuery.cs` | No structural change; filters still apply |
| `Lookups/Queries/GetCountryCodeById/GetCountryCodeByIdQueryHandler.cs` | Query `Countries` by id |
| `Lookups/Commands/UpsertCountryCode/UpsertCountryCodeCommandHandler.cs` | Upsert into `Countries` with `IsCceCountry = false` |

### API endpoint
**File:** `src/CCE.Api.External/Endpoints/CountryCodesPublicEndpoints.cs`

Route `/api/country-codes` stays unchanged for frontend compatibility. Query retargets to `Countries`:
```csharp
// Before: ListCountryCodesQuery → _db.CountryCodes
// After:  ListCountryCodesQuery → _db.Countries.Where(c => c.DialCode != null)
```

Same change in `src/CCE.Api.Internal/Endpoints/CountryCodesPublicEndpoints.cs` if it exists.

> **Backwards compatibility tip:** During the frontend transition, accept both `countryCodeId` and `countryId` in registration/profile request bodies, mapping both to `CountryId`. Remove `countryCodeId` in a follow-up once frontend is updated.

---

## Phase 5 — Seeder & Verification

### 5a. Rewrite `CountryCodeSeeder`
**File:** `src/CCE.Seeder/Seeders/CountryCodeSeeder.cs`

Change target from `country_codes` to `countries` table:
```csharp
// Before: _ctx.CountryCodes.Add(CountryCode.Create(...))
// After:  _ctx.Countries.Add(Country.RegisterLookup(id, nameAr, nameEn, dialCode, flagUrl))
```

Idempotency check: use `id` or `(dial_code + name_en)` as the sentinel.

Also update `ReferenceDataSeeder` to populate `dial_code` on CCE country rows where available.

### 5b. Verification queries after migration
```sql
-- 1. CCE countries preserved
SELECT COUNT(*) FROM countries WHERE is_cce_country = 1;

-- 2. Dial-code entries present
SELECT COUNT(*) FROM countries WHERE dial_code IS NOT NULL;

-- 3. No orphaned state-rep assignments — MUST return 0
SELECT COUNT(*) FROM state_representative_assignments sra
WHERE NOT EXISTS (SELECT 1 FROM countries c WHERE c.id = sra.country_id);

-- 4. No orphaned country profiles — MUST return 0
SELECT COUNT(*) FROM country_profiles cp
WHERE NOT EXISTS (SELECT 1 FROM countries c WHERE c.id = cp.country_id);

-- 5. User coverage (must be >= pre-migration users with either FK set)
SELECT COUNT(*) FROM users WHERE country_id IS NOT NULL;

-- 6. Old table is gone — must return NULL
SELECT OBJECT_ID('country_codes');
```

---

## Risks & Mitigations

| Risk | Severity | Mitigation |
|------|----------|------------|
| Name-match fails for some `country_codes` rows (e.g. "United States" vs "United States of America") — rows inserted as duplicates | **High** | Dry-run the name-match query before migration. Manually patch divergent names in `country_codes` before Step 3. |
| Users with `country_id` AND `country_code_id` pointing to different countries — phone country silently dropped | **High** | Phase 0 conflict query. If > 0, add `phone_country_id` column instead of overloading `country_id`. |
| Frontend sends `countryCodeId` in request bodies — API breaks after field removed | **Medium** | Accept both fields temporarily in request bodies, map both to `CountryId`. Remove in follow-up. |
| Unique index `ux_country_iso_alpha3_active` fails if non-CCE rows insert NULL `iso_alpha3` (NULLs are non-equal in SQL Server, so multiple NULLs pass — actually no issue, but confirm) | **Low** | Verify the index has a WHERE filter on `is_cce_country = 1`. Add it if missing. |

---

## Completion Checklist

- [ ] DB backup taken
- [ ] Phase 0 conflict-detection query run and result documented
- [ ] `Country` entity: `DialCode`, `IsCceCountry`, `RegisterLookup()`, `SetDialCode()` added
- [ ] ISO / Region properties nullable in private constructor
- [ ] `User.CountryCodeId` property removed from domain
- [ ] `CountryConfiguration` updated (nullable ISO, dial_code, is_cce_country, index filter)
- [ ] `UserConfiguration` updated (CountryCodeId index removed)
- [ ] `ICceDbContext.CountryCodes` removed
- [ ] EF migration generated and applied to dev DB
- [ ] All 5 SQL steps verified (sanity checks returned 0)
- [ ] All 22 Application files referencing `CountryCodeId`/`CountryCodes` updated
- [ ] `/api/country-codes` endpoint retargeted to `Countries` query
- [ ] `CountryCodeSeeder` rewritten to seed into `Countries`
- [ ] All 6 verification queries return expected values
- [ ] `dotnet build CCE.sln` passes with zero warnings
- [ ] `CountryCode` domain entity and `CountryCodeConfiguration.cs` deleted
