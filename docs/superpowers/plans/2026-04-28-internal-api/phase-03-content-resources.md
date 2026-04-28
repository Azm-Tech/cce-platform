# Phase 03 — Content (resources + assets + virus scan)

> Parent: [`../2026-04-28-internal-api.md`](../2026-04-28-internal-api.md) · Spec: [`../../specs/2026-04-28-internal-api-design.md`](../../specs/2026-04-28-internal-api-design.md) §3.4 + §3.7 (Phase 3)

**Phase goal:** Ship the asset-upload pipeline (multipart → `LocalFileStorage` → ClamAV synchronous scan → `AssetFile` row), full resources CRUD, and country-resource-request approve/reject.

**Tasks:** 8
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 02 closed at `00a98ac`. 466 + 1 skipped backend tests passing.
- `clamav` docker container running on `:3310` (existing dev compose; `cce-clamav` per `docker-compose.yml`).

---

## Pre-execution sanity checks

1. `git status` clean apart from `.claude/`.
2. `dotnet build backend/CCE.sln --no-restore` 0/0.
3. `dotnet test backend/CCE.sln --no-build --no-restore` → 466 + 1 skipped.
4. `nc -z localhost 3310 && echo clamav-up` (ClamAV reachable).

---

## Endpoint catalog

| # | Verb + path | Permission | Body / query | Returns |
|---|---|---|---|---|
| 3.2 | `POST /api/admin/assets` (multipart) | `Resource.Center.Upload` OR `Resource.Country.Submit` | binary file | `201 AssetFileDto` |
| 3.3 | `GET /api/admin/assets/{id}` | `Resource.Center.Upload` (or any Resource.* — keep simple, use Center.Upload) | – | `200 AssetFileDto` |
| 3.4 | `GET /api/admin/resources?page=...&search=...&categoryId=...&countryId=...&isPublished=...` | `Resource.Center.Upload` | – | `PagedResult<ResourceDto>` |
| 3.5 | `POST /api/admin/resources` | `Resource.Center.Upload` | `{ TitleAr, TitleEn, DescriptionAr, DescriptionEn, ResourceType, CategoryId, CountryId?, AssetFileId }` | `201 ResourceDto` |
| 3.6 | `PUT /api/admin/resources/{id}` | `Resource.Center.Update` | `{ ...fields, RowVersion }` | `200 ResourceDto` (or 409) |
| 3.7 | `POST /api/admin/resources/{id}/publish` | `Resource.Center.Upload` | – | `200 ResourceDto` (or 409 if asset not Clean) |
| 3.8 | `POST /api/admin/country-resource-requests/{id}/approve` + `/reject` | `Resource.Country.Approve` / `.Reject` | `{ AdminNotesAr, AdminNotesEn }` (notesAr/En required for reject; optional for approve) | `200 CountryResourceRequestDto` |

(Task 3.1 ships the cross-cutting infrastructure — no endpoint.)

---

## Cross-cutting Task 3.1: `IFileStorage`, `LocalFileStorage`, `IClamAvScanner`, `ClamAvScanner`

**Goal:** Abstractions + dev implementations needed by Tasks 3.2–3.7.

### `IFileStorage` (Application layer)

```csharp
public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string suggestedFileName, CancellationToken ct);   // returns storageKey
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct);
    Task DeleteAsync(string storageKey, CancellationToken ct);
}
```

Storage key shape: `uploads/yyyy/MM/{guid}{ext}`. Returns the key (a relative path), not a URL.

### `LocalFileStorage` (Infrastructure)

- Reads `CceInfrastructureOptions.LocalUploadsRoot` (default `./backend/uploads/`). The options class lives in `CCE.Infrastructure/CceInfrastructureOptions.cs` — extend with the new property.
- `SaveAsync` ensures the directory exists, generates a Guid filename keeping the original extension, writes the stream, returns the storage key.
- `OpenReadAsync` opens the file at `{root}/{key}` for reading.
- `DeleteAsync` deletes the file (no-op if missing).

### `IClamAvScanner` (Application layer)

```csharp
public enum VirusScanResult { Clean, Infected, ScanFailed }

public interface IClamAvScanner
{
    Task<VirusScanResult> ScanAsync(Stream content, CancellationToken ct);
}
```

### `ClamAvScanner` (Infrastructure)

- Reads `CceInfrastructureOptions.ClamAvHost` (default `localhost`) + `ClamAvPort` (default `3310`).
- Implements ClamAV INSTREAM TCP protocol manually (avoids adding a NuGet dep): open TCP, send `zINSTREAM\0`, send framed chunks (4-byte length prefix big-endian + bytes), send 4 zero bytes terminator, read response. Parse: response containing `OK` → Clean; `FOUND` → Infected; anything else (or exception) → ScanFailed.
- 5-second connect/read timeout. ConfigureAwait(false) on TCP I/O.

### Tests

- Unit tests for `LocalFileStorage` against a temp directory: `Save_round_trips_via_OpenRead`, `Save_returns_key_under_year_month_subdirectory`, `Delete_is_idempotent_when_missing`. Use `Path.GetTempPath()` + cleanup in `IDisposable.Dispose`.
- Unit tests for `ClamAvScanner` against a TCP-server stub (not real clamav): `Returns_Clean_on_OK_response`, `Returns_Infected_on_FOUND_response`, `Returns_ScanFailed_on_unparseable_response`. Use `System.Net.Sockets.TcpListener` to mock clamd; the test starts a listener on a free port, scripts the response, asserts.

### DI

`AddInfrastructure` registers `IFileStorage` → `LocalFileStorage` (singleton) and `IClamAvScanner` → `ClamAvScanner` (transient).

### Permissions

No changes — existing `Resource.Center.Upload` / `Resource.Country.Submit` / `Resource.Center.Update` / `Resource.Center.Delete` / `Resource.Country.Approve` / `Resource.Country.Reject` cover Phase 3.

### CPM additions

None. ClamAV protocol is implemented manually using `System.Net.Sockets.TcpClient`.

### Commit

`feat(infrastructure): IFileStorage + LocalFileStorage + IClamAvScanner + ClamAvScanner (Phase 3.1)`

---

## Task 3.2 — `POST /api/admin/assets`

**Files:**
- `CCE.Application/Content/Dtos/AssetFileDto.cs` (Id, Url, OriginalFileName, SizeBytes, MimeType, UploadedById, UploadedOn, VirusScanStatus, ScannedOn).
- `CCE.Application/Content/Commands/UploadAsset/UploadAssetCommand.cs` — `(Stream Content, string OriginalFileName, string MimeType, long SizeBytes) : IRequest<AssetFileDto>`.
- `.../UploadAssetCommandHandler.cs` — calls `IFileStorage.SaveAsync` → reads back via `OpenReadAsync` to scan (or saves twice; or buffers content in memory once for both save + scan: choose buffered approach for files ≤ 100 MB) → calls `IClamAvScanner.ScanAsync` → builds `AssetFile` with `Register(...)` → applies the scan result via `MarkClean/Infected/ScanFailed`. If Infected, calls `IFileStorage.DeleteAsync(key)` to remove the storage object. Persists via a new `IAssetService` (Application interface; Infrastructure implementation). Returns DTO.
- MIME allow-list: read from `CceInfrastructureOptions.AllowedAssetMimeTypes` (new option, default `["application/pdf","image/png","image/jpeg","image/svg+xml","video/mp4","application/zip"]`). Reject 415 Unsupported Media Type if not in list.
- Endpoint: `MapPost("/api/admin/assets", ... /* IFormFile + binding */).RequireAuthorization` with policy that accepts EITHER `Resource.Center.Upload` OR `Resource.Country.Submit` — register a custom policy `Permissions.AssetUpload` covering both, OR just use one permission and document that StateRepresentative + ContentManager + SuperAdmin all have access. **For simplicity, gate on `Permissions.Resource_Center_Upload`** (StateRep won't have it but Phase 3.8 separately handles state-rep submission via country-resource-requests). Re-check in 3.8 whether StateRep needs direct asset upload.
- 100 MB request size limit via `[RequestSizeLimit]` attribute / minimal-API equivalent (`endpoint.WithMetadata(new RequestSizeLimitAttribute(100 * 1024 * 1024))` or set `KestrelServerOptions.Limits.MaxRequestBodySize` globally).
- Tests: handler unit (5: rejected-mime, save+clean, save+infected→deletes file, save+scan-failed, big-file). Integration: 1 (anonymous → 401).

**Commit:** `feat(api-internal): POST /api/admin/assets (multipart upload + virus scan) (Phase 3.2)`

---

## Task 3.3 — `GET /api/admin/assets/{id}`

**Files:**
- `CCE.Application/Content/Queries/GetAssetById/GetAssetByIdQuery.cs` + Handler.
- Endpoint mapping.
- Tests: 2 handler + 2 endpoint.

**Commit:** `feat(api-internal): GET /api/admin/assets/{id} (Phase 3.3)`

---

## Task 3.4 — `GET /api/admin/resources`

**Files:**
- `CCE.Application/Content/Dtos/ResourceDto.cs` — Id, TitleAr/En, DescriptionAr/En, ResourceType, CategoryId, CountryId, UploadedById, AssetFileId, PublishedOn, ViewCount, IsDeleted, IsCenterManaged, IsPublished, RowVersion (base64 string).
- `CCE.Application/Content/Queries/ListResources/ListResourcesQuery.cs` (Page, PageSize, Search?, CategoryId?, CountryId?, IsPublished?).
- `.../ListResourcesQueryHandler.cs`.
- Endpoint mapping.
- Tests: 4 handler + 2 endpoint.

**Commit:** `feat(api-internal): GET /api/admin/resources (Phase 3.4)`

---

## Task 3.5 — `POST /api/admin/resources`

**Files:**
- `CCE.Application/Content/Commands/CreateResource/CreateResourceCommand.cs` (TitleAr, TitleEn, DescriptionAr, DescriptionEn, ResourceType, CategoryId, CountryId?, AssetFileId).
- `.../CreateResourceCommandValidator.cs`.
- `.../CreateResourceCommandHandler.cs` — verifies asset exists + is `VirusScanStatus.Clean`, builds via `Resource.Draft(...)`, persists, returns DTO.
- `IResourceService` (Application) + Infrastructure impl for Add/Update/Find operations across 3.5/3.6/3.7.
- Endpoint mapping.
- Tests: 4 handler + 3 validator + 2 endpoint.

**Commit:** `feat(api-internal): POST /api/admin/resources (Phase 3.5)`

---

## Task 3.6 — `PUT /api/admin/resources/{id}`

**Files:**
- `CCE.Application/Content/Commands/UpdateResource/UpdateResourceCommand.cs` — full field set + `byte[] RowVersion`. Returns `ResourceDto`.
- Validator (rowVersion length 8).
- Handler — load via service `FindAsync`, set `OriginalValues["RowVersion"]` to the incoming value before save (concurrency), apply field updates via domain methods (Resource has no full-edit method; we'll need to add `Resource.UpdateContent(titleAr, ...)` to domain — DEFERRED: instead, handle simple cases by re-creating not allowed; add a permissive `UpdateContent` method in the domain entity inside Phase 3.6 alongside the application work).
- Endpoint.
- Tests: 4 handler + 2 endpoint (200 + 409 concurrency).

**Domain change:** Phase 3.6 adds `Resource.UpdateContent(string titleAr, string titleEn, string descriptionAr, string descriptionEn, ResourceType type, Guid categoryId)` + a domain test in `CCE.Domain.Tests`. Audited via existing interceptor.

**Commit:** `feat(api-internal): PUT /api/admin/resources/{id} + Resource.UpdateContent (Phase 3.6)`

---

## Task 3.7 — `POST /api/admin/resources/{id}/publish`

**Files:**
- Command + handler. Handler loads resource, loads associated asset, asserts `asset.VirusScanStatus == Clean` (else throw `DomainException("Asset has not passed virus scan.")` → 400 via Phase 0 mapping; spec says 409 — switch to a custom exception type if 409 desired, OR raise `DomainException` and the spec wording for 409 is a mismatch we'll document). Calls `resource.Publish(_clock)`. Persists.
- Endpoint.
- Tests: 3 handler + 2 endpoint.

**Commit:** `feat(api-internal): POST /api/admin/resources/{id}/publish (Phase 3.7)`

---

## Task 3.8 — `POST /api/admin/country-resource-requests/{id}/approve` + `/reject`

**Files:**
- `CCE.Application/Content/Dtos/CountryResourceRequestDto.cs`.
- Approve command + handler (calls `request.Approve(adminId, notesAr, notesEn, clock)`; the domain event handler in Phase 07 — already wired in sub-project 2 — creates the Resource).
- Reject command + handler (calls `request.Reject(adminId, notesAr, notesEn, clock)`).
- Endpoints (2 verbs on a single MapGroup).
- Tests: 3 + 3 handler + 2 + 2 endpoint = ~10.

**Permissions:** `Resource.Country.Approve` / `Resource.Country.Reject`.

**Commit:** `feat(api-internal): POST /api/admin/country-resource-requests/{id}/{approve,reject} (Phase 3.8)`

---

## Phase 03 — completion checklist

- [ ] 7 endpoints + cross-cutting infrastructure live.
- [ ] `IFileStorage` + `LocalFileStorage` + `IClamAvScanner` + `ClamAvScanner` shipped.
- [ ] `Resource.UpdateContent` domain method added with test.
- [ ] `IAssetService` + `IResourceService` services in Application + Infrastructure.
- [ ] +~50 net new tests.
- [ ] 8 atomic commits.
- [ ] `contracts/openapi.internal.json` regenerated; drift script clean.
- [ ] Build clean; full suite green.

When all boxes ticked, Phase 03 is complete.
