# Phase 02 — Expert workflow

> Parent: [`../2026-04-29-admin-cms.md`](../2026-04-29-admin-cms.md) · Spec: [`../../specs/2026-04-29-admin-cms-design.md`](../../specs/2026-04-29-admin-cms-design.md) §4.1.20

**Phase goal:** Ship the expert workflow surface — review pending expert-registration requests (approve/reject) and browse approved expert profiles. Single permission: `Community.Expert.ApproveRequest`.

**Tasks:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 01 closed (`8c8a760`).

---

## Endpoint coverage (backend → frontend)

| Endpoint | Method | Auth | Phase 02 surface |
|---|---|---|---|
| `/api/admin/expert-requests` | GET | `Community.Expert.ApproveRequest` | Task 2.1 |
| `/api/admin/expert-requests/{id}/approve` | POST | `Community.Expert.ApproveRequest` | Task 2.2 |
| `/api/admin/expert-requests/{id}/reject` | POST | `Community.Expert.ApproveRequest` | Task 2.3 |
| `/api/admin/expert-profiles` | GET | `Community.Expert.ApproveRequest` | Task 2.4 |

---

## DTOs

```ts
// frontend/apps/admin-cms/src/app/features/experts/expert.types.ts
export type ExpertRegistrationStatus = 'Pending' | 'Approved' | 'Rejected';

export interface ExpertRequest {
  id: string;
  requestedById: string;
  requestedByUserName: string | null;
  requestedBioAr: string;
  requestedBioEn: string;
  requestedTags: string[];
  submittedOn: string;
  status: ExpertRegistrationStatus;
  processedById: string | null;
  processedOn: string | null;
  rejectionReasonAr: string | null;
  rejectionReasonEn: string | null;
}

export interface ExpertProfile {
  id: string;
  userId: string;
  userName: string | null;
  bioAr: string;
  bioEn: string;
  expertiseTags: string[];
  academicTitleAr: string;
  academicTitleEn: string;
  approvedOn: string;
  approvedById: string;
}

export interface ApproveExpertRequestBody {
  academicTitleAr: string;
  academicTitleEn: string;
}
export interface RejectExpertRequestBody {
  rejectionReasonAr: string;
  rejectionReasonEn: string;
}
```

## Folder structure

```
apps/admin-cms/src/app/features/experts/
├── routes.ts                          // EXPERTS_ROUTES (/experts list = requests, /experts/profiles = profiles list)
├── expert.types.ts
├── expert-api.service.ts              // listRequests, approve, reject, listProfiles
├── expert-api.service.spec.ts
├── expert-requests-list.page.ts/.html/.scss/.spec.ts
├── approve-expert.dialog.ts/.html/.spec.ts
├── reject-expert.dialog.ts/.html/.spec.ts
└── expert-profiles-list.page.ts/.html/.scss/.spec.ts
```

Top-level routes: `/experts` (requests), `/experts/profiles` (profiles).

---

## Task 2.1: ExpertApiService + Expert requests list page

Service mirrors IdentityApiService pattern: HttpClient + toFeatureError.

ExpertRequestsListPage uses `<cce-paged-table>`:
- Columns: requestedByUserName, submittedOn (date), tags (joined), status (chip), actions.
- Filters: status select (all/Pending/Approved/Rejected), default = Pending.
- Row actions (only when status='Pending'):
  - "Approve" button → opens ApproveExpertDialog (Task 2.2).
  - "Reject" button → opens RejectExpertDialog (Task 2.3).
- Bio columns are long; show in a disclosure or expand-row in v0.1.0 (skip — display tags + name only; bios available on detail in a future phase).

## Task 2.2: ApproveExpertDialog

Reactive Form with `academicTitleAr` + `academicTitleEn` (both required). Save → POST /approve. Dialog closes with the updated ExpertRequest.

## Task 2.3: RejectExpertDialog

Reactive Form with `rejectionReasonAr` + `rejectionReasonEn` (both required, multi-line). Save → POST /reject. Dialog closes with the updated ExpertRequest.

## Task 2.4: ExpertProfilesListPage

Read-only list at `/experts/profiles`. `<cce-paged-table>`:
- Columns: userName, academicTitleAr, academicTitleEn, expertiseTags (joined), approvedOn (date).
- Filter: search box (user name LIKE) — calls api with `search` query param.

## Routing

`apps/admin-cms/src/app/app.routes.ts` add:
```ts
{
  path: 'experts',
  canActivate: [autoLoginPartialRoutesGuard],
  loadChildren: () => import('./features/experts/routes').then((m) => m.EXPERTS_ROUTES),
  title: 'CCE — Experts',
}
```

`features/experts/routes.ts`:
```ts
export const EXPERTS_ROUTES: Routes = [
  { path: '', loadComponent: ... ExpertRequestsListPage, data: { permission: 'Community.Expert.ApproveRequest' }, canMatch: [permissionGuard] },
  { path: 'profiles', loadComponent: ... ExpertProfilesListPage, data: { permission: 'Community.Expert.ApproveRequest' }, canMatch: [permissionGuard] },
];
```

Side-nav: `nav.experts` already wired in Phase 0; secondary nav item for "Expert profiles" lands as a child route — for v0.1.0, the user navigates via in-page "View profiles" link.

## Commits

- 2.1+2.2+2.3 in one commit (requests list + approve dialog + reject dialog — tightly coupled).
- 2.4 separate commit (profiles list).

## i18n keys

```
experts: {
  requests: { title, col: { user, submitted, tags, status, actions }, filter: { status, allStatuses }, action: { approve, reject } },
  approve: { title, titleAr, titleEn, save, toast, openButton },
  reject: { title, reasonAr, reasonEn, save, toast, openButton },
  profiles: { title, col: { user, titleAr, titleEn, tags, approvedOn }, filter: { search }, viewProfilesButton, viewRequestsButton },
  status: { Pending, Approved, Rejected }
}
```

---

## Phase 02 — completion checklist

- [ ] Task 2.1 — Expert requests list + ExpertApiService.
- [ ] Task 2.2 — Approve dialog.
- [ ] Task 2.3 — Reject dialog.
- [ ] Task 2.4 — Expert profiles list.
- [ ] All Jest tests passing for `features/experts`.
- [ ] admin-cms lint clean.
- [ ] Build clean.
- [ ] 2 atomic commits.
