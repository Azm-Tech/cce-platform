# PlatformSettings Database Structure

## Overview

There are **3 singleton parent tables** and **4 child/collection tables**.
All singleton parents support soft delete. Child collection tables do **not**
have soft-delete columns (hard delete only).

---

## Singleton Parent Tables (1 row each)

### `homepage_settings`

| Column | Type | Single / List | How Populated |
|---|---|---|---|
| `id` | `uniqueidentifier` PK | Single | `ReferenceDataSeeder` creates, `PlatformSettingsSeeder` enriches |
| `objective_ar` | `nvarchar(1000)` | Single | Seeder + Admin API `PUT /api/admin/settings/homepage` |
| `objective_en` | `nvarchar(1000)` | Single | Seeder + Admin API |
| `video_url` | `nvarchar(max)` | Single | Seeder + Admin API |
| `cce_concepts_ar` | `nvarchar(max)` | Single | Seeder + Admin API |
| `cce_concepts_en` | `nvarchar(max)` | Single | Seeder + Admin API |
| `created_by_id`, `created_on`, `last_modified_by_id`, `last_modified_on` | audit | Single | Auto |
| `deleted_by_id`, `deleted_on`, `is_deleted` | soft delete | Single | Auto |
| `row_version` | `rowversion` | Single | Auto (concurrency) |

**LocalizedText mapping:** `Objective` → `objective_ar` / `objective_en`

---

### `about_settings`

| Column | Type | Single / List | How Populated |
|---|---|---|---|
| `id` | `uniqueidentifier` PK | Single | `ReferenceDataSeeder` creates, `PlatformSettingsSeeder` enriches |
| `description_ar` | `nvarchar(1000)` | Single | Seeder + Admin API `PUT /api/admin/settings/about` |
| `description_en` | `nvarchar(1000)` | Single | Seeder + Admin API |
| `how_to_use_video_url` | `nvarchar(max)` | Single | Seeder + Admin API |
| `created_by_id`, `created_on`, `last_modified_by_id`, `last_modified_on` | audit | Single | Auto |
| `deleted_by_id`, `deleted_on`, `is_deleted` | soft delete | Single | Auto |
| `row_version` | `rowversion` | Single | Auto (concurrency) |

**LocalizedText mapping:** `Description` → `description_ar` / `description_en`

---

### `policies_settings`

| Column | Type | Single / List | How Populated |
|---|---|---|---|
| `id` | `uniqueidentifier` PK | Single | `ReferenceDataSeeder` creates bare row |
| `created_by_id`, `created_on`, `last_modified_by_id`, `last_modified_on` | audit | Single | Auto |
| `deleted_by_id`, `deleted_on`, `is_deleted` | soft delete | Single | Auto |
| `row_version` | `rowversion` | Single | Auto (concurrency) |

**Note:** No admin endpoint updates this table directly. It is managed
indirectly through its child `policy_sections`.

---

## Child / Collection Tables (0..N rows per parent)

### `homepage_countries` — **List** of country links

| Column | Type | Single / List | How Populated |
|---|---|---|---|
| `id` | `uniqueidentifier` PK | Single per row | Seeder + Admin API |
| `homepage_settings_id` | `uniqueidentifier` FK | Single per row | Set by `SyncCountries()` domain method |
| `country_id` | `uniqueidentifier` | Single per row | Seeder + Admin API |
| `order_index` | `int` | Single per row | Auto (0, 1, 2...) |
| `created_by_id`, `created_on`, `last_modified_by_id`, `last_modified_on` | audit | Single per row | Auto |

**Populated by:**
- **Seeder:** `PlatformSettingsSeeder` adds 5 GCC countries (SAU, ARE, KWT, QAT, BHR)
- **Admin API:** `PUT /api/admin/settings/homepage` sends `ParticipatingCountryIds: ["guid", "guid"]` → `SyncCountries()` adds/removes/reorders

---

### `glossary_entries` — **List** of entries

| Column | Type | Single / List | How Populated |
|---|---|---|---|
| `id` | `uniqueidentifier` PK | Single per row | Seeder + Admin API |
| `about_settings_id` | `uniqueidentifier` FK | Single per row | Set by `AddGlossaryEntry()` |
| `term_ar` | `nvarchar(100)` | Single per row | Seeder + Admin API |
| `term_en` | `nvarchar(100)` | Single per row | Seeder + Admin API |
| `definition_ar` | `nvarchar(1000)` | Single per row | Seeder + Admin API |
| `definition_en` | `nvarchar(1000)` | Single per row | Seeder + Admin API |
| `order_index` | `int` | Single per row | Auto |
| `created_by_id`, `created_on`, `last_modified_by_id`, `last_modified_on` | audit | Single per row | Auto |

**LocalizedText mappings:**
- `Term` → `term_ar` / `term_en`
- `Definition` → `definition_ar` / `definition_en`

**Populated by:**
- **Seeder:** `PlatformSettingsSeeder` adds 4 entries (CCE, DAC, CCUS, LCOE)
- **Admin API:**
  - `POST /api/admin/settings/about/glossary`
  - `PUT /api/admin/settings/about/glossary/{id}`
  - `DELETE /api/admin/settings/about/glossary/{id}`

---

### `knowledge_partners` — **List** of partners

| Column | Type | Single / List | How Populated |
|---|---|---|---|
| `id` | `uniqueidentifier` PK | Single per row | Seeder + Admin API |
| `about_settings_id` | `uniqueidentifier` FK | Single per row | Set by `AddKnowledgePartner()` |
| `name_ar` | `nvarchar(200)` | Single per row | Seeder + Admin API |
| `name_en` | `nvarchar(200)` | Single per row | Seeder + Admin API |
| `description_ar` | `nvarchar(1000)` | Single per row | Seeder + Admin API |
| `description_en` | `nvarchar(1000)` | Single per row | Seeder + Admin API |
| `logo_url` | `nvarchar(max)` | Single per row | Seeder + Admin API |
| `website_url` | `nvarchar(max)` | Single per row | Seeder + Admin API |
| `order_index` | `int` | Single per row | Auto |
| `created_by_id`, `created_on`, `last_modified_by_id`, `last_modified_on` | audit | Single per row | Auto |

**LocalizedText mappings:**
- `Name` → `name_ar` / `name_en`
- `Description` → `description_ar` / `description_en`

**Populated by:**
- **Seeder:** `PlatformSettingsSeeder` adds 3 partners (KAPSARC, IRENA, GCEP)
- **Admin API:**
  - `POST /api/admin/settings/about/knowledge-partners`
  - `PUT /api/admin/settings/about/knowledge-partners/{id}`
  - `DELETE /api/admin/settings/about/knowledge-partners/{id}`

---

### `policy_sections` — **List** of sections

| Column | Type | Single / List | How Populated |
|---|---|---|---|
| `id` | `uniqueidentifier` PK | Single per row | Seeder + Admin API |
| `policies_settings_id` | `uniqueidentifier` FK | Single per row | Set by `AddSection()` |
| `type` | `int` (enum) | Single per row | Seeder + Admin API |
| `title_ar` | `nvarchar(500)` | Single per row | Seeder + Admin API |
| `title_en` | `nvarchar(500)` | Single per row | Seeder + Admin API |
| `content_ar` | `nvarchar(max)` | Single per row | Seeder + Admin API |
| `content_en` | `nvarchar(max)` | Single per row | Seeder + Admin API |
| `order_index` | `int` | Single per row | Auto |
| `created_by_id`, `created_on`, `last_modified_by_id`, `last_modified_on` | audit | Single per row | Auto |

**LocalizedText mappings:**
- `Title` → `title_ar` / `title_en`
- `Content` → `content_ar` / `content_en`

**Populated by:**
- **Seeder:** `PlatformSettingsSeeder` adds 3 sections (Terms, Privacy, FAQ)
- **Admin API:**
  - `POST /api/admin/settings/policies/sections`
  - `PUT /api/admin/settings/policies/sections/{id}`
  - `PUT /api/admin/settings/policies/sections/{id}/order`
  - `DELETE /api/admin/settings/policies/sections/{id}`

---

## Key Relationships

| Child Table | FK Column | Parent Table | Delete Behavior |
|---|---|---|---|
| `homepage_countries` | `homepage_settings_id` | `homepage_settings` | Cascade |
| `glossary_entries` | `about_settings_id` | `about_settings` | Cascade |
| `knowledge_partners` | `about_settings_id` | `about_settings` | Cascade |
| `policy_sections` | `policies_settings_id` | `policies_settings` | Cascade |

`homepage_countries.country_id` is a **logical reference** to the `countries`
table; there is no database-enforced foreign key constraint.

---

## LocalizedText Column Mappings

Every bilingual field is stored as two columns (`_ar` / `_en`) via EF Core
owned entities (`OwnsOne`):

| Table | Property | AR Column | EN Column | Max Length |
|---|---|---|---|---|
| `homepage_settings` | `Objective` | `objective_ar` | `objective_en` | 1000 |
| `about_settings` | `Description` | `description_ar` | `description_en` | 1000 |
| `glossary_entries` | `Term` | `term_ar` | `term_en` | 100 |
| `glossary_entries` | `Definition` | `definition_ar` | `definition_en` | 1000 |
| `knowledge_partners` | `Name` | `name_ar` | `name_en` | 200 |
| `knowledge_partners` | `Description` | `description_ar` | `description_en` | 1000 |
| `policy_sections` | `Title` | `title_ar` | `title_en` | 500 |
| `policy_sections` | `Content` | `content_ar` | `content_en` | max |

---

## Public API Read Models

- **Homepage:** Returns `VideoUrl`, `Objective` (ar/en), `CceConceptsAr`,
  `CceConceptsEn`, linked `Countries` (joined with `countries` table for
  name/flag/ISO), and active `HomepageSections` (from the separate
  `homepage_sections` content table).

- **About:** Returns `Description` (ar/en), `HowToUseVideoUrl`, ordered
  `GlossaryEntries`, and ordered `KnowledgePartners`.

- **Policies:** Returns ordered `PolicySections` with `Type`, `Title` (ar/en),
  and `Content` (ar/en) — currently as **single HTML strings**.

---

## The Problem

`policy_sections.content_ar` and `policy_sections.content_en` are currently
**Single values** (one big HTML string per section). You want them to become
a **List** so the API returns:

```json
{
  "contentItems": [
    { "ar": "1. القبول بالشروط", "en": "1. Acceptance of Terms" },
    { "ar": "باستخدامك لهذه المنصة...", "en": "By using this platform..." }
  ]
}
```

This would require a **new child table** following the exact same pattern as
`glossary_entries` and `knowledge_partners`.

---

## Related Files

| Layer | Path |
|---|---|
| Domain | `src/CCE.Domain/PlatformSettings/` |
| EF Config | `src/CCE.Infrastructure/Persistence/Configurations/PlatformSettings/` |
| Migrations | `src/CCE.Infrastructure/Persistence/Migrations/` |
| Commands | `src/CCE.Application/PlatformSettings/Commands/` |
| Queries | `src/CCE.Application/PlatformSettings/Queries/` |
| Public Queries | `src/CCE.Application/PlatformSettings/Public/Queries/` |
| Internal API | `src/CCE.Api.Internal/Endpoints/` |
| External API | `src/CCE.Api.External/Endpoints/` |
| Seeders | `src/CCE.Seeder/Seeders/` |
