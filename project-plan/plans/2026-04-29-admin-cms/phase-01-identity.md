# Phase 01 — Identity admin

> Parent: [`../2026-04-29-admin-cms.md`](../2026-04-29-admin-cms.md) · Spec: [`../../specs/2026-04-29-admin-cms-design.md`](../../specs/2026-04-29-admin-cms-design.md) §4.1.19, §5

**Phase goal:** Ship the first feature area — Users + State-rep assignments — using every Phase 0 building block. After Phase 01, ministry SuperAdmins can paginate users, view a profile, assign/revoke roles, and manage state-representative-to-country assignments.

**Tasks:** 6
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 00 closed (`117a727`).
- Contracts regenerated, api-client emits `listUsers`, `getUserById`, `assignUserRoles`, `listStateRepAssignments`, `createStateRepAssignment`, `revokeStateRepAssignment` (`2997eba`).

---

## Endpoint coverage (backend → frontend)

| Endpoint | Method | Auth | Phase 01 surface |
|---|---|---|---|
| `/api/admin/users` | GET | `User.Read` | Task 1.1 (list) |
| `/api/admin/users/{id}` | GET | `User.Read` | Task 1.2 (detail) |
| `/api/admin/users/{id}/roles` | PUT | `Role.Assign` | Task 1.3 (role-assign dialog) |
| `/api/admin/state-rep-assignments` | GET | `Role.Assign` | Task 1.4 (list) |
| `/api/admin/state-rep-assignments` | POST | `Role.Assign` | Task 1.5 (create) |
| `/api/admin/state-rep-assignments/{id}` | DELETE | `Role.Assign` | Task 1.6 (revoke) |

---

## DTOs (hand-defined — generated `*Response` types are `unknown`)

```ts
// frontend/apps/admin-cms/src/app/features/identity/identity.types.ts
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface UserListItem {
  id: string;
  email: string | null;
  userName: string | null;
  roles: string[];
  isActive: boolean;
}

export type KnowledgeLevel = 'Beginner' | 'Intermediate' | 'Advanced';

export interface UserDetail {
  id: string;
  email: string | null;
  userName: string | null;
  localePreference: string;
  knowledgeLevel: KnowledgeLevel;
  interests: string[];
  countryId: string | null;
  avatarUrl: string | null;
  roles: string[];
  isActive: boolean;
}

export interface StateRepAssignment {
  id: string;
  userId: string;
  userName: string | null;
  countryId: string;
  assignedOn: string;
  assignedById: string;
  revokedOn: string | null;
  revokedById: string | null;
  isActive: boolean;
}

/** Roles known to the backend (CCE.Domain.RolePermissionMap.KnownRoles). */
export const KNOWN_ROLES = [
  'SuperAdmin',
  'ContentManager',
  'StateRepresentative',
  'CommunityExpert',
  'RegisteredUser',
] as const;
export type RoleName = (typeof KNOWN_ROLES)[number];
```

## Identity feature folder structure

```
apps/admin-cms/src/app/features/identity/
├── routes.ts                                 // IDENTITY_ROUTES
├── identity.types.ts                         // DTOs above
├── identity-api.service.ts                   // wraps generated client; toFeatureError mapping
├── identity-api.service.spec.ts
├── users-list.page.ts/.html/.scss/.spec.ts   // Task 1.1
├── user-detail.page.ts/.html/.scss/.spec.ts  // Task 1.2 + 1.3 (role-assign dialog opens from here)
├── role-assign.dialog.ts/.html/.spec.ts      // Task 1.3
├── state-rep-list.page.ts/.html/.scss/.spec.ts // Task 1.4 + 1.6 (revoke action lives in the row)
├── state-rep-create.dialog.ts/.html/.spec.ts // Task 1.5
└── state-rep-revoke.spec.ts                  // Task 1.6 verifies the confirm-then-revoke flow
```

Lazy-load via:
```ts
{
  path: 'users',
  loadChildren: () =>
    import('./features/identity/routes').then((m) => m.IDENTITY_ROUTES),
}
```

---

## Task 1.1: Users list page

**Files:**
- Create: `frontend/apps/admin-cms/src/app/features/identity/identity.types.ts` (full content above)
- Create: `frontend/apps/admin-cms/src/app/features/identity/identity-api.service.ts`
- Create: `frontend/apps/admin-cms/src/app/features/identity/identity-api.service.spec.ts`
- Create: `frontend/apps/admin-cms/src/app/features/identity/users-list.page.ts/.html/.scss/.spec.ts`
- Create: `frontend/apps/admin-cms/src/app/features/identity/routes.ts`
- Modify: `frontend/apps/admin-cms/src/app/app.routes.ts` (add `users` lazy route + permissionGuard)

### Step 1: IdentityApiService (HttpClient + toFeatureError mapping)

```ts
// identity-api.service.ts
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '../../core/ui/error-formatter';
import type {
  PagedResult,
  UserListItem,
  UserDetail,
  StateRepAssignment,
} from './identity.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class IdentityApiService {
  private readonly http = inject(HttpClient);

  async listUsers(opts: { page?: number; pageSize?: number; search?: string; role?: string } = {}): Promise<Result<PagedResult<UserListItem>>> {
    let params = new HttpParams();
    if (opts.page) params = params.set('page', opts.page);
    if (opts.pageSize) params = params.set('pageSize', opts.pageSize);
    if (opts.search) params = params.set('search', opts.search);
    if (opts.role) params = params.set('role', opts.role);
    return this.run(() => firstValueFrom(this.http.get<PagedResult<UserListItem>>('/api/admin/users', { params })));
  }

  async getUser(id: string): Promise<Result<UserDetail>> {
    return this.run(() => firstValueFrom(this.http.get<UserDetail>(`/api/admin/users/${id}`)));
  }

  async assignRoles(id: string, roles: string[]): Promise<Result<UserDetail>> {
    return this.run(() => firstValueFrom(this.http.put<UserDetail>(`/api/admin/users/${id}/roles`, { roles })));
  }

  async listStateRepAssignments(opts: { page?: number; pageSize?: number; userId?: string; countryId?: string; active?: boolean } = {}): Promise<Result<PagedResult<StateRepAssignment>>> {
    let params = new HttpParams();
    if (opts.page) params = params.set('page', opts.page);
    if (opts.pageSize) params = params.set('pageSize', opts.pageSize);
    if (opts.userId) params = params.set('userId', opts.userId);
    if (opts.countryId) params = params.set('countryId', opts.countryId);
    if (opts.active !== undefined) params = params.set('active', String(opts.active));
    return this.run(() => firstValueFrom(this.http.get<PagedResult<StateRepAssignment>>('/api/admin/state-rep-assignments', { params })));
  }

  async createStateRepAssignment(body: { userId: string; countryId: string }): Promise<Result<StateRepAssignment>> {
    return this.run(() => firstValueFrom(this.http.post<StateRepAssignment>('/api/admin/state-rep-assignments', body)));
  }

  async revokeStateRepAssignment(id: string): Promise<Result<void>> {
    return this.run(() => firstValueFrom(this.http.delete<void>(`/api/admin/state-rep-assignments/${id}`)));
  }

  private async run<T>(fn: () => Promise<T>): Promise<Result<T>> {
    try {
      return { ok: true, value: await fn() };
    } catch (err) {
      const error = toFeatureError(err as HttpErrorResponse);
      return { ok: false, error };
    }
  }
}
```

### Step 2: Tests for IdentityApiService

TestBed + HttpTestingController. Verify:
- listUsers() builds query string with page/pageSize/search/role.
- getUser/assignRoles/listStateRepAssignments/createStateRepAssignment/revokeStateRepAssignment all hit the right URL with the right method.
- Error path: 404 → `{ ok: false, error: { kind: 'not-found' } }`; 409 type=duplicate → `{ ok: false, error: { kind: 'duplicate' } }`.

### Step 3: UsersListPage (signal-based, uses `<cce-paged-table>`)

```ts
// users-list.page.ts
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { TranslateModule } from '@ngx-translate/core';
import { PagedTableComponent, type PagedTableColumn, type PagedTablePageChange } from '../../core/ui/paged-table.component';
import { IdentityApiService } from './identity-api.service';
import { KNOWN_ROLES, type UserListItem } from './identity.types';

@Component({
  selector: 'cce-users-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatSelectModule, TranslateModule, PagedTableComponent],
  templateUrl: './users-list.page.html',
  styleUrl: './users-list.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersListPage {
  private readonly api = inject(IdentityApiService);

  readonly searchInput = signal('');
  readonly roleFilter = signal<string>('');
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly rows = signal<UserListItem[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly knownRoles = KNOWN_ROLES;

  readonly columns: PagedTableColumn<UserListItem>[] = [
    { key: 'userName', labelKey: 'users.col.userName', cell: (r) => r.userName ?? '—' },
    { key: 'email', labelKey: 'users.col.email', cell: (r) => r.email ?? '—' },
    { key: 'roles', labelKey: 'users.col.roles', cell: (r) => r.roles.join(', ') || '—' },
    { key: 'isActive', labelKey: 'users.col.isActive', cell: (r) => (r.isActive ? '✓' : '✗') },
  ];

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    const res = await this.api.listUsers({
      page: this.page(),
      pageSize: this.pageSize(),
      search: this.searchInput() || undefined,
      role: this.roleFilter() || undefined,
    });
    this.loading.set(false);
    if (res.ok) {
      this.rows.set(res.value.items);
      this.total.set(Number(res.value.total));
    } else {
      this.error.set(res.error.kind);
    }
  }

  ngOnInit(): void { void this.load(); }

  onPage(e: PagedTablePageChange): void {
    this.page.set(e.page);
    this.pageSize.set(e.pageSize);
    void this.load();
  }

  onSearch(): void { this.page.set(1); void this.load(); }
  onRoleFilter(): void { this.page.set(1); void this.load(); }
}
```

```html
<!-- users-list.page.html -->
<div class="cce-users-list__filters">
  <mat-form-field appearance="outline">
    <mat-label>{{ 'users.filter.search' | translate }}</mat-label>
    <input matInput [ngModel]="searchInput()" (ngModelChange)="searchInput.set($event)" (keyup.enter)="onSearch()" />
  </mat-form-field>
  <mat-form-field appearance="outline">
    <mat-label>{{ 'users.filter.role' | translate }}</mat-label>
    <mat-select [ngModel]="roleFilter()" (ngModelChange)="roleFilter.set($event); onRoleFilter()">
      <mat-option value="">{{ 'users.filter.allRoles' | translate }}</mat-option>
      @for (role of knownRoles; track role) {
        <mat-option [value]="role">{{ role }}</mat-option>
      }
    </mat-select>
  </mat-form-field>
</div>

@if (error()) {
  <div class="cce-users-list__error">{{ ('errors.' + error()) | translate }}</div>
}

<cce-paged-table
  [columns]="columns"
  [rows]="rows()"
  [total]="total()"
  [page]="page()"
  [pageSize]="pageSize()"
  [loading]="loading()"
  (pageChange)="onPage($event)"
/>
```

```scss
.cce-users-list__filters { display: flex; gap: 1rem; margin-bottom: 1rem; }
.cce-users-list__error { color: #b00020; padding: .5rem 1rem; }
```

### Step 4: routes.ts + register lazy route

```ts
// frontend/apps/admin-cms/src/app/features/identity/routes.ts
import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/permission.guard';

export const IDENTITY_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./users-list.page').then((m) => m.UsersListPage), data: { permission: 'User.Read' }, canMatch: [permissionGuard] },
  { path: ':id', loadComponent: () => import('./user-detail.page').then((m) => m.UserDetailPage), data: { permission: 'User.Read' }, canMatch: [permissionGuard] },
];
```

```ts
// app.routes.ts (add)
import { permissionGuard } from './core/auth/permission.guard';

export const appRoutes: Route[] = [
  { path: '', pathMatch: 'full', redirectTo: 'profile' },
  { path: 'profile', component: ProfilePage, canActivate: [autoLoginPartialRoutesGuard], title: 'CCE — Profile' },
  {
    path: 'users',
    loadChildren: () => import('./features/identity/routes').then((m) => m.IDENTITY_ROUTES),
    canActivate: [autoLoginPartialRoutesGuard],
    title: 'CCE — Users',
  },
  {
    path: 'state-rep-assignments',
    loadChildren: () => import('./features/identity/state-rep-routes').then((m) => m.STATE_REP_ROUTES),
    canActivate: [autoLoginPartialRoutesGuard],
    title: 'CCE — State Reps',
  },
];
```

### Step 5: i18n keys

Add to `libs/i18n/src/lib/i18n/en.json` + `ar.json`:

```json
"users": {
  "title": "Users",
  "col": { "userName": "User name", "email": "Email", "roles": "Roles", "isActive": "Active" },
  "filter": { "search": "Search", "role": "Role", "allRoles": "All roles" },
  "detail": { "title": "User profile", "back": "Back to users" }
},
"roleAssign": { "title": "Assign roles", "current": "Current roles", "available": "Available roles", "save": "Save", "cancel": "Cancel" },
"stateRep": { "title": "State-rep assignments", "col": { "user": "User", "country": "Country", "assignedOn": "Assigned on", "active": "Active" }, "create": { "title": "New assignment", "user": "User ID", "country": "Country ID", "save": "Create", "cancel": "Cancel" }, "revoke": { "title": "Revoke assignment", "message": "Revoke this assignment? It can be re-created later but the audit trail records the revocation.", "confirm": "Revoke" } }
```

### Step 6: Tests for UsersListPage

TestBed renders the page with stubbed IdentityApiService. Verify:
- ngOnInit triggers `load()` → service called with `{ page: 1, pageSize: 20 }`.
- rows + total signals populated from response.
- `onPage({ page: 2, pageSize: 50 })` re-fires load with new params.
- `onSearch()` resets page to 1 and re-fires.

### Step 7: Verify + commit

```bash
cd frontend
pnpm nx test admin-cms --testPathPattern="features/identity" 2>&1 | tail -10
pnpm nx test admin-cms 2>&1 | tail -5
pnpm nx lint admin-cms 2>&1 | tail -5
pnpm nx build admin-cms 2>&1 | tail -5
```

```bash
git add frontend/apps/admin-cms/src/app/features/identity/ \
        frontend/apps/admin-cms/src/app/app.routes.ts \
        frontend/libs/i18n/src/lib/i18n/
git -c commit.gpgsign=false commit -m "feat(admin-cms): users list page + IdentityApiService (Phase 1.1)"
```

---

## Task 1.2: User detail page

**Files:**
- Create: `features/identity/user-detail.page.ts/.html/.scss/.spec.ts`

UserDetailPage shows the full UserDetailDto using a card layout. Read-only for v0.1.0 (profile editing is the user's own concern via /api/me). Action: "Assign roles" button opens RoleAssignDialog (Task 1.3).

Component sketch:
```ts
@Component({ selector: 'cce-user-detail', standalone: true, ... })
export class UserDetailPage {
  private readonly api = inject(IdentityApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);

  readonly user = signal<UserDetail | null>(null);
  readonly loading = signal(false);
  readonly error = signal<FeatureError | null>(null);

  async ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.loading.set(true);
    const res = await this.api.getUser(id);
    this.loading.set(false);
    if (res.ok) this.user.set(res.value);
    else this.error.set(res.error);
  }

  async openRoleAssign(): Promise<void> {
    const u = this.user();
    if (!u) return;
    const ref = this.dialog.open(RoleAssignDialog, { data: { userId: u.id, currentRoles: u.roles }, width: '480px' });
    const updated = await firstValueFrom(ref.afterClosed());
    if (updated) {
      this.user.set(updated);
      this.toast.success('roleAssign.saved');
    }
  }
}
```

Template: card with email, userName, roles (chips), isActive badge, "Assign roles" button gated by `*ccePermission="'Role.Assign'"`. "Back to users" link to `/users`.

Tests: page loads on init; openRoleAssign opens dialog; updates user signal on close; toast on success.

Commit: `feat(admin-cms): user detail page (Phase 1.2)`.

---

## Task 1.3: Role-assign dialog

**Files:**
- Create: `features/identity/role-assign.dialog.ts/.html/.spec.ts`

Standalone Material dialog. Inputs: `{ userId: string; currentRoles: string[] }`. Body:
- Multi-select (`mat-select` `multiple`) seeded from `KNOWN_ROLES`, default selection = currentRoles.
- "Save" → calls `api.assignRoles(userId, selected)`. On success, closes with the updated UserDetail. On error, displays inline message via ErrorFormatter.
- "Cancel" → closes with null.

Tests: stub IdentityApiService; assert PUT body has selected roles; assert `dialog.close(updatedUser)` on success; assert error path keeps the dialog open and shows the error.

Commit: `feat(admin-cms): role-assign dialog (Phase 1.3)`.

---

## Task 1.4: State-rep assignments list

**Files:**
- Create: `features/identity/state-rep-routes.ts` (separate routes module — top-level `/state-rep-assignments` URL)
- Create: `features/identity/state-rep-list.page.ts/.html/.scss/.spec.ts`

`<cce-paged-table>` with columns: userName, countryId (Guid for v0.1.0; country name lands in Phase 06), assignedOn (formatted via DatePipe), isActive (✓/✗), actions ("Revoke" button gated by `*ccePermission="'Role.Assign'"` — opens confirm + calls api).

Filter: `active` toggle (default true). "New assignment" button (top-right, gated by `*ccePermission="'Role.Assign'"`) opens StateRepCreateDialog (Task 1.5).

```ts
export const STATE_REP_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./state-rep-list.page').then((m) => m.StateRepListPage),
    data: { permission: 'Role.Assign' },
    canMatch: [permissionGuard],
  },
];
```

Update `app.routes.ts` `/state-rep-assignments` route to load STATE_REP_ROUTES.

Tests: page loads with default `{ active: true }` filter; toggling active reloads; clicking revoke calls confirm-dialog then api.

Commit: `feat(admin-cms): state-rep assignments list (Phase 1.4)`.

---

## Task 1.5: State-rep create dialog

**Files:**
- Create: `features/identity/state-rep-create.dialog.ts/.html/.spec.ts`

Standalone dialog with typed Reactive Form:
```ts
this.form = new FormGroup({
  userId: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.pattern(GUID_RE)] }),
  countryId: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.pattern(GUID_RE)] }),
});
```

Submit → `api.createStateRepAssignment(form.getRawValue())`. On success, closes with new StateRepAssignment + parent reloads list. Validation errors → maps via `toFieldErrors` to `<mat-error>` per field.

GUID picker UX: free-text in v0.1.0; Phase 06 adds country dropdown; Phase 02 adds expert profile dropdown that could feed user IDs. Document in component header that v0.1.0 is admin-power-user only.

Tests: invalid GUID → form invalid, save disabled; valid GUIDs → calls api, closes with result; 409 duplicate → error displayed inline; 400 validation → field errors displayed.

Commit: `feat(admin-cms): state-rep create dialog (Phase 1.5)`.

---

## Task 1.6: State-rep revoke flow

**Files:**
- Modify: `features/identity/state-rep-list.page.ts` (already wired in Task 1.4 — formalize the flow + tests)

The "Revoke" action in each row:
1. Calls `confirmDialog.confirm({ titleKey: 'stateRep.revoke.title', messageKey: 'stateRep.revoke.message', confirmKey: 'stateRep.revoke.confirm', cancelKey: 'common.actions.cancel' })`.
2. If confirmed, `api.revokeStateRepAssignment(row.id)`.
3. On success, refreshes list + toast.success('stateRep.revoke.toast').
4. On error, toast.error('errors.' + err.kind).

Tests: confirm → api called → list reloaded; cancel → no api call; api error → toast.error fired with right key.

Commit: `feat(admin-cms): state-rep revoke flow + confirm + toast (Phase 1.6)`.

---

## Phase 01 — completion checklist

- [ ] Task 1.1 — Users list + IdentityApiService.
- [ ] Task 1.2 — User detail.
- [ ] Task 1.3 — Role-assign dialog.
- [ ] Task 1.4 — State-rep assignments list.
- [ ] Task 1.5 — State-rep create dialog.
- [ ] Task 1.6 — State-rep revoke flow.
- [ ] All Jest tests passing for `features/identity`.
- [ ] admin-cms lint clean (0 errors).
- [ ] Build clean.
- [ ] 6 atomic commits.

**If all boxes ticked, Phase 01 is complete. Proceed to Phase 02 (expert workflow).**
