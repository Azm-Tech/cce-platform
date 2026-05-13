# Phase 07 — Reports CSV (streamed)

> Parent: [`../2026-04-28-internal-api.md`](../2026-04-28-internal-api.md) · Spec: [`../../specs/2026-04-28-internal-api-design.md`](../../specs/2026-04-28-internal-api-design.md) §3.5

**Phase goal:** Ship 8 streaming-CSV admin report endpoints under `/api/admin/reports/{name}.csv`, each filterable by `?from=ISO8601&to=ISO8601`. Streaming via `IAsyncEnumerable<TRow>` + CsvHelper writes directly to `Response.Body` so memory stays bounded.

**Tasks:** 9 (consolidated into 4 commits)
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 06 closed at `7681962`. 764 + 1 skipped tests.

## Endpoints

| # | Endpoint | Permission | Source entity |
|---|---|---|---|
| 7.1 | `GET /reports/users-registrations.csv` | `Report.UserRegistrations` | Users |
| 7.2 | `GET /reports/experts.csv` | `Report.ExpertList` | ExpertProfiles |
| 7.3 | `GET /reports/satisfaction-survey.csv` | `Report.SatisfactionSurvey` | ServiceRatings |
| 7.4 | `GET /reports/community-posts.csv` | `Report.CommunityPosts` | Posts |
| 7.5 | `GET /reports/news.csv` | `Report.News` | News |
| 7.6 | `GET /reports/events.csv` | `Report.Events` | Events |
| 7.7 | `GET /reports/resources.csv` | `Report.Resources` | Resources |
| 7.8 | `GET /reports/country-profiles.csv` | `Report.CountryProfiles` | Countries |

All 8 permissions exist in `permissions.yaml` (verified). All gated to `SuperAdmin` only.

## Cross-cutting (Task 7.1)

- Add `CsvHelper` 33.0.1 to `Directory.Packages.props` (CPM) and reference from `CCE.Application` (where the streaming logic lives — Application can depend on CsvHelper because it's a transport-format helper, not infrastructure).
- Add `ICceDbContext.ServiceRatings` accessor (the `ServiceRating` entity is in `CCE.Domain.Surveys`).
- Define `ICsvStreamWriter` (Application) that takes an `IAsyncEnumerable<TRow>` + writes to a `Stream` with UTF-8 BOM + CSV header. Implementation in Application namespace (no Infrastructure dep).
- Endpoint helper `ReportsHelper.StreamAsync<TRow>(IAsyncEnumerable<TRow>, string filename, HttpContext)` wraps `Response.ContentType = "text/csv"` + `Content-Disposition: attachment; filename="..."` + invokes the writer.
- Each report handler returns `IAsyncEnumerable<TRow>` directly (NOT through MediatR — MediatR's `IRequestHandler<TRequest, TResponse>` doesn't support streaming responses cleanly without IRequestHandler<,IAsyncEnumerable<>>). For Phase 07 we BYPASS MediatR for reports — endpoint receives `IFooReportService.QueryAsync(filter, ct)` directly via DI.

(This is a deliberate deviation from the established MediatR pattern, justified by streaming response shape. Each report has a thin Application service.)

## Tasks

| Task | Reports |
|---|---|
| 7.1 | CsvHelper + ICsvStreamWriter infra + users-registrations.csv |
| 7.2 | experts.csv + satisfaction-survey.csv + community-posts.csv |
| 7.3 | news.csv + events.csv + resources.csv |
| 7.4 | country-profiles.csv + ServiceRatings DbContext accessor (if not in 7.1) |

## Phase 07 — completion checklist

- [ ] CsvHelper added to CPM + Application reference.
- [ ] `ICsvStreamWriter` infrastructure shipped.
- [ ] 8 endpoints live with streaming CSV + UTF-8 BOM.
- [ ] Each endpoint accepts `?from=ISO8601&to=ISO8601`.
- [ ] +~30 net tests (1-2 per report — happy + 401).
- [ ] 4 atomic commits.
- [ ] Build clean.
