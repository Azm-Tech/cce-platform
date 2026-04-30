# Phase 06 — Account (register, /me, expert-request, service-rating)

> Parent: [`../2026-04-30-web-portal.md`](../2026-04-30-web-portal.md) · Spec: [`../../specs/2026-04-30-web-portal-design.md`](../../specs/2026-04-30-web-portal-design.md) §5 (`/api/users/register`, `/api/me`, `/api/me/expert-status`, `/api/users/expert-request`, `/api/surveys/service-rating`)

**Phase goal:** Self-service account flows — anonymous users register, authenticated users view + edit their profile, submit expert-registration requests, and any user (anonymous or authenticated) submits a service-rating survey. After Phase 06, every authentication-scoped endpoint of the External API surface has a working FE.

**Tasks:** 8
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 05 closed (`e38bb16`).
- Phase 0 AuthService already exposes `signIn(returnUrl?)`, `signOut()`, `currentUser()`, `isAuthenticated()`. The auth callback route `/auth/callback` is already wired (Sub-5 lessons absorbed in Phase 0).

---

## Endpoint coverage

| Endpoint | Method | Phase 06 surface | Auth |
|---|---|---|---|
| `/api/users/register` | POST (302 redirect to Keycloak) | Task 6.2 (RegisterPage) | Anon |
| `/api/users/expert-request` | POST | Task 6.5 (ExpertRequestPage) | ✗ |
| `/api/me` | GET | Task 6.3 (ProfilePage read) | ✗ |
| `/api/me` | PUT | Task 6.4 (ProfilePage edit) | ✗ |
| `/api/me/expert-status` | GET | Task 6.5 (status banner) | ✗ |
| `/api/surveys/service-rating` | POST | Task 6.6 (ServiceRatingDialog) | Anon |

**Backend contract notes** (verified against `backend/src/CCE.Api.External/Endpoints/ProfileEndpoints.cs` + `SurveysEndpoints.cs` + `Identity/Public/Dtos/`):

```csharp
// UserProfileDto (GET /api/me + PUT /api/me response)
public sealed record UserProfileDto(
    Guid Id, string? Email, string? UserName,
    string LocalePreference, KnowledgeLevel KnowledgeLevel,    // enum string: "Beginner" | "Intermediate" | "Advanced"
    IReadOnlyList<string> Interests,
    Guid? CountryId, string? AvatarUrl);
// NB: NO row-version / concurrency token. The master plan's "concurrency token"
// language is aspirational; plan downgrades to last-write-wins for v0.1.0
// and adds a Phase 9 backlog item to revisit if the backend grows one.

// UpdateMyProfileRequest (PUT /api/me body)
public sealed record UpdateMyProfileRequest(
    string LocalePreference,
    KnowledgeLevel KnowledgeLevel,
    IReadOnlyList<string>? Interests,
    string? AvatarUrl,
    Guid? CountryId);

// ExpertRequestStatusDto (GET /api/me/expert-status)
public sealed record ExpertRequestStatusDto(
    Guid Id, Guid RequestedById,
    string RequestedBioAr, string RequestedBioEn,
    IReadOnlyList<string> RequestedTags,
    DateTimeOffset SubmittedOn,
    ExpertRegistrationStatus Status,                            // enum string: "Pending" | "Approved" | "Rejected"
    DateTimeOffset? ProcessedOn,
    string? RejectionReasonAr, string? RejectionReasonEn);

// SubmitExpertRequestRequest (POST /api/users/expert-request body)
public sealed record SubmitExpertRequestRequest(
    string RequestedBioAr,
    string RequestedBioEn,
    IReadOnlyList<string>? RequestedTags);

// ServiceRatingRequest (POST /api/surveys/service-rating body)
public sealed record ServiceRatingRequest(
    int Rating,                  // 1-5 (validated server-side)
    string? CommentAr,
    string? CommentEn,
    string Page,                 // canonical page identifier, e.g. "knowledge-center"
    string Locale);              // "ar" | "en"
```

`POST /api/users/register` is a **302-redirect** endpoint — backend issues a `Location:` header to the Keycloak registrations URL. The FE register affordance is just a button that navigates there (`window.location.assign('/api/users/register')`); there is no register form on the SPA.

`/api/me` and `/api/me/expert-status` both return `Results.NotFound()` when the user record exists in Keycloak but no DB-side profile row has been provisioned yet (race between first sign-in and the first user-provisioner run). FE must tolerate `404` on `/api/me` GET and treat it as "profile not yet provisioned, retry-with-button".

## Hand-defined DTOs

```ts
// frontend/apps/web-portal/src/app/features/account/account.types.ts

export type KnowledgeLevel = 'Beginner' | 'Intermediate' | 'Advanced';
export const KNOWLEDGE_LEVELS: readonly KnowledgeLevel[] = ['Beginner', 'Intermediate', 'Advanced'] as const;

export type ExpertRegistrationStatus = 'Pending' | 'Approved' | 'Rejected';

export interface UserProfile {
  id: string;
  email: string | null;
  userName: string | null;
  localePreference: string;       // "ar" | "en" — but server-side accepts arbitrary BCP-47
  knowledgeLevel: KnowledgeLevel;
  interests: string[];
  countryId: string | null;
  avatarUrl: string | null;
}

export interface UpdateMyProfilePayload {
  localePreference: string;
  knowledgeLevel: KnowledgeLevel;
  interests?: string[];
  avatarUrl?: string | null;
  countryId?: string | null;
}

export interface ExpertRequestStatus {
  id: string;
  requestedById: string;
  requestedBioAr: string;
  requestedBioEn: string;
  requestedTags: string[];
  submittedOn: string;            // ISO datetime
  status: ExpertRegistrationStatus;
  processedOn: string | null;
  rejectionReasonAr: string | null;
  rejectionReasonEn: string | null;
}

export interface SubmitExpertRequestPayload {
  requestedBioAr: string;
  requestedBioEn: string;
  requestedTags?: string[];
}

export interface ServiceRatingPayload {
  rating: number;                 // 1..5
  commentAr?: string | null;
  commentEn?: string | null;
  page: string;
  locale: 'ar' | 'en';
}
```

## Folder structure

```
apps/web-portal/src/app/features/account/
├── account.types.ts
├── account-api.service.{ts,spec.ts}            # Task 6.1 (all 5 endpoints)
├── register.page.{ts,html,scss,spec.ts}        # Task 6.2 (anon)
├── profile.page.{ts,html,scss,spec.ts}         # Task 6.3 + 6.4 (read + edit)
├── expert-request.page.{ts,html,scss,spec.ts}  # Task 6.5
└── routes.ts

apps/web-portal/src/app/core/feedback/
└── service-rating-dialog.component.{ts,scss,spec.ts}  # Task 6.6 (also re-exported from a shell helper)
```

`/me/follows` lives in Phase 07 — Phase 06 leaves the route entry alone.

---

## Task 6.1: AccountApiService + types

**Files (all new):**
- `features/account/account.types.ts`
- `features/account/account-api.service.{ts,spec.ts}`

AccountApiService methods:
- `getProfile()` → `Result<UserProfile>` (GET `/api/me`). 404 → `{ kind: 'not-found' }` (profile not yet provisioned).
- `updateProfile(payload)` → `Result<UserProfile>` (PUT `/api/me`).
- `getExpertStatus()` → `Result<ExpertRequestStatus | null>` (GET `/api/me/expert-status`). 404 → `{ ok: true, value: null }` (user has not yet submitted a request — the same KAPSARC pattern from Phase 4.1).
- `submitExpertRequest(payload)` → `Result<ExpertRequestStatus>` (POST `/api/users/expert-request`).
- `submitServiceRating(payload)` → `Result<{ id: string }>` (POST `/api/surveys/service-rating`).

Follows the Result + toFeatureError pattern.

**Tests (~7):**
1. `getProfile()` GETs `/api/me`.
2. `getProfile` returns `{ kind: 'not-found' }` on 404.
3. `updateProfile({...})` PUTs `/api/me` with the payload body and returns the updated DTO on 200.
4. `getExpertStatus` returns `{ ok: true, value: null }` on 404 (no request yet).
5. `getExpertStatus` returns the DTO on 200.
6. `submitExpertRequest({ requestedBioAr, requestedBioEn })` POSTs `/api/users/expert-request`.
7. `submitServiceRating({ rating: 5, page: 'home', locale: 'en' })` POSTs `/api/surveys/service-rating` and returns the new id.

Commit: `feat(web-portal): AccountApiService + DTOs (Phase 6.1)`

---

## Task 6.2: RegisterPage (anon redirect button)

**Files:**
- `features/account/register.page.{ts,html,scss,spec.ts}`

The `POST /api/users/register` endpoint is a 302-redirect to Keycloak's `/registrations` URL. The SPA cannot follow a 302 cross-origin via fetch (the browser blocks the redirect for `mode: 'cors'`); instead, the page renders explanatory copy + a primary button that calls `window.location.assign('/api/users/register')`, letting the browser handle the redirect natively.

RegisterPage:
- Renders i18n title, body explaining "we'll redirect you to the secure sign-up screen", and a "Continue to sign-up" button.
- Button click → `window.location.assign('/api/users/register')`.
- If already authenticated, render a "You're already signed in" message + link to `/me/profile` instead of the button.

Tests (~3):
1. Button click calls `window.location.assign('/api/users/register')` (jsdom-spy on `window.location` via `Object.defineProperty` — same pattern as `auth.service.spec.ts`).
2. When authenticated, button is hidden + "Already signed in" copy renders + `routerLink` to `/me/profile` is present.
3. Locale toggle switches title/body to ar text.

Commit: `feat(web-portal): RegisterPage with Keycloak redirect button (Phase 6.2)`

---

## Task 6.3: ProfilePage — read

**Files:**
- `features/account/profile.page.{ts,html,scss,spec.ts}` (read-only first; edit form deferred to Task 6.4 in the *same* file)

ProfilePage:
- Guarded by `authGuard` (Phase 0). Anonymous → bounce to sign-in.
- Loads on init via `account.getProfile()`.
- 404 path: render "We're still setting up your profile. Please retry in a moment." + retry button. (Profile-provisioner runs out-of-band; first sign-in can race the first profile insert.)
- Renders avatar (when set), email, userName, localePreference, knowledgeLevel (badge), interests as chips, countryId (resolved to country name via Phase 4 `CountriesApiService.listCountries({})` cache, fall back to "—" when unresolved).
- Localized labels via LocaleService.

For Task 6.3 only: render-mode (no edit fields, no save button). Task 6.4 augments with the form; we keep them in the same file because the cyclomatic complexity is low and fixture setup is shared.

Tests (~3 added in 6.3):
1. Loads on init via `getProfile()` and binds DOM (.cce-profile__email, .cce-profile__user-name, etc.).
2. 404 renders the "still setting up" hint + retry button; clicking retry re-fires `getProfile()`.
3. Locale toggle switches knowledgeLevel badge label (Beginner/Intermediate/Advanced) to ar.

Commit: `feat(web-portal): ProfilePage (read mode) (Phase 6.3)`

---

## Task 6.4: ProfilePage — edit (typed Reactive Form)

**Files:**
- Modify: `features/account/profile.page.{ts,html,scss,spec.ts}` (extend with edit mode)

Adds:
- `mode = signal<'view' | 'edit'>('view')`. "Edit" button toggles to `edit`; "Cancel" reverts to `view`.
- Typed Reactive Form (`FormGroup<…>`) with controls: `localePreference` (radio ar/en), `knowledgeLevel` (mat-select), `interests` (chip-input — comma-split → trim → dedupe), `countryId` (mat-select pre-loaded from `CountriesApiService.listCountries({})`), `avatarUrl` (mat-input, optional URL). On `mode='edit'` switch, form is patched from the current profile signal.
- Save:
  - `account.updateProfile(payload)` with payload built from form.value.
  - On success: `profile.set(updated)`, `mode.set('view')`, toast "Profile saved." (uses ui-kit ToastService).
  - On error: leave form in edit mode, surface error banner (server / validation / network).
  - **No optimistic concurrency** — backend doesn't expose a row-version. Last-write-wins for v0.1.0; Phase 9 backlog item documents the gap.

Tests (~5 added in 6.4):
1. Clicking "Edit" patches the form from the current profile signal and switches to `mode='edit'`.
2. Submitting the form with valid values calls `updateProfile(payload)` with the right shape.
3. On success, profile is updated + mode reverts to view + toast.success called.
4. On error (server kind), error banner renders; mode stays edit.
5. "Cancel" button resets the form to the original profile values + reverts mode to view.

Commit: `feat(web-portal): ProfilePage edit mode (Phase 6.4)`

---

## Task 6.5: ExpertRequestPage — submit + status

**Files:**
- `features/account/expert-request.page.{ts,html,scss,spec.ts}`

ExpertRequestPage:
- Guarded by `authGuard`.
- On init: `account.getExpertStatus()` to drive a status banner.
  - `null` (no request yet) → render the submission form.
  - `'Pending'` → status banner "Your request was submitted on {date}. Awaiting review."; form is hidden.
  - `'Approved'` → banner "Your expert profile is active." + link to expert profile (placeholder, real link lives in Sub-7).
  - `'Rejected'` → banner with rejection reasonAr/En (locale-driven) + "Submit a new request" button that re-opens the form.
- Form fields: `requestedBioAr` (textarea, required, 50..2000 chars), `requestedBioEn` (textarea, required, 50..2000 chars), `requestedTags` (chip-input, optional, max 10 tags).
- Submit:
  - `account.submitExpertRequest(payload)`.
  - On success: refresh status (the response IS the new status DTO) and replace the form with the Pending banner; toast "Request submitted."
  - On error: surface error banner.

Tests (~5):
1. Init load with no request renders form + does NOT show banner.
2. Init load with `Pending` status hides the form + renders the banner with submitted-on date.
3. Init load with `Rejected` status renders banner with rejection reason (locale-driven) + "Submit a new request" button.
4. Submit with valid form posts payload to `submitExpertRequest` and on success swaps to Pending banner.
5. Validation: bio fields shorter than 50 chars block submit (form invalid).

Commit: `feat(web-portal): ExpertRequestPage submit + status banner (Phase 6.5)`

---

## Task 6.6: ServiceRatingDialog (mat-dialog, anon-friendly)

**Files:**
- `core/feedback/service-rating-dialog.component.{ts,scss,spec.ts}`

ServiceRatingDialogComponent: a Material dialog component (`MatDialogRef` + `MAT_DIALOG_DATA`).
- Inputs (via `MAT_DIALOG_DATA`): `{ page: string; locale: 'ar' | 'en' }`.
- Form: `rating` (1-5 star widget), `comment` (single textarea — backend takes both `commentAr` + `commentEn` but the FE ships locale-specific copy in only the matching field; the other side is `null`).
- Submit:
  - `account.submitServiceRating({ rating, commentAr/En, page, locale })`.
  - On success: close dialog with `{ submitted: true }`, toast "Thanks for your feedback."
  - On error: keep dialog open, surface inline error.

Star rating: keyboard-accessible (Tab through each star button, Enter/Space toggles; `aria-label` per star).

The dialog is opened from anywhere by injecting the dialog service and calling:
```ts
this.dialog.open(ServiceRatingDialogComponent, { data: { page: 'home', locale: 'en' } });
```
A small consumer (a "Rate this page" floating button) is added to PortalShellComponent's footer area in Task 6.8 (routing + i18n + E2E), wired to open the dialog with the current page identifier resolved from the active route.

Tests (~4):
1. 5-star rating submit calls `submitServiceRating({ rating: 5, ..., page, locale })` with the right payload.
2. Comment populates `commentEn` when `locale='en'` and leaves `commentAr=null` (and vice versa).
3. On success, `dialogRef.close({ submitted: true })` and toast.success fired.
4. On error, dialog stays open and error banner shows.

Commit: `feat(web-portal): ServiceRatingDialog with 1-5 stars (Phase 6.6)`

---

## Task 6.7: AuthGuard (production-grade)

**Files:**
- Modify: `apps/web-portal/src/app/core/auth/auth.guard.ts` + `auth.guard.spec.ts`

Phase 0 shipped a placeholder `authGuard`. Phase 6 hardens it for the real account pages:
- Functional `CanActivateFn`. Reads `AuthService.isAuthenticated()`.
- If authenticated → `true`.
- If not → call `auth.signIn(returnUrl)` where `returnUrl` is the URL the guard was invoked for (`state.url`), then return `false` so the route doesn't render.
- **Edge case**: if AuthService hasn't yet refreshed (cold start, `currentUser() === null`), the guard awaits `auth.refresh()` once, then re-checks `isAuthenticated()`. This avoids spuriously bouncing to sign-in when the cookie is valid but `/api/me` hasn't been called yet.

Tests (~4):
1. authenticated user → guard returns `true`.
2. unauthenticated user (after refresh) → calls `signIn(returnUrl=state.url)` + returns `false`.
3. cold-start path: `currentUser()` is null on first call → guard awaits `refresh()`, then proceeds.
4. Repeat call after a real auth: guard short-circuits without calling `refresh()` again (idempotent).

Commit: `feat(web-portal): production-grade authGuard with cold-start refresh (Phase 6.7)`

---

## Task 6.8: Routes + i18n + E2E nav smoke

**Files:**
- New: `features/account/routes.ts`
- Modify: `apps/web-portal/src/app/app.routes.ts` — add `register` (anon) and `me` (authGuard).
- Modify: `apps/web-portal/src/app/core/layout/portal-shell.component.html` — wire the "Rate this page" button in the footer area to open ServiceRatingDialog.
- Modify: `libs/i18n/src/lib/i18n/{en,ar}.json` — extend.
- New: `apps/web-portal-e2e/src/account.spec.ts`.

`features/account/routes.ts`:

```ts
export const ACCOUNT_ROUTES: Routes = [
  { path: 'profile', loadComponent: () => import('./profile.page').then((m) => m.ProfilePage) },
  { path: 'expert-request', loadComponent: () => import('./expert-request.page').then((m) => m.ExpertRequestPage) },
  // 'follows' lives in Phase 07
];
```

Add to `app.routes.ts`:

```ts
{
  path: 'register',
  loadComponent: () => import('./features/account/register.page').then((m) => m.RegisterPage),
  title: 'CCE — Register',
},
{
  path: 'me',
  canActivate: [authGuard],
  loadChildren: () => import('./features/account/routes').then((m) => m.ACCOUNT_ROUTES),
  title: 'CCE — My account',
},
```

i18n additions (en + ar mirrored):

`account.register`:
- `account.register.title` — "Create your CCE account"
- `account.register.body` — "We'll redirect you to the secure sign-up screen. After completing it, you'll come back here signed in."
- `account.register.continueButton` — "Continue to sign-up"
- `account.register.alreadySignedIn` — "You're already signed in."
- `account.register.openProfile` — "Open my profile"

`account.profile`:
- `account.profile.title` — "My profile"
- `account.profile.notProvisioned` — "We're still setting up your profile. Please retry in a moment."
- `account.profile.field.email`, `.userName`, `.localePreference`, `.knowledgeLevel`, `.interests`, `.country`, `.avatarUrl`
- `account.profile.editButton` — "Edit"
- `account.profile.saveButton` — "Save"
- `account.profile.cancelButton` — "Cancel"
- `account.profile.toast.saved` — "Profile saved."

`account.knowledgeLevel.{Beginner,Intermediate,Advanced}` — labels for the badge.

`account.expert`:
- `account.expert.title` — "Become an expert"
- `account.expert.body` — explanatory copy.
- `account.expert.field.bioAr`, `.bioEn`, `.tags`
- `account.expert.submitButton` — "Submit request"
- `account.expert.banner.pending` — "Your request was submitted on {{date}}. Awaiting review."
- `account.expert.banner.approved` — "Your expert profile is active."
- `account.expert.banner.rejected` — "Your previous request was declined: {{reason}}"
- `account.expert.resubmitButton` — "Submit a new request"
- `account.expert.toast.submitted` — "Request submitted."

`account.serviceRating`:
- `account.serviceRating.openButton` — "Rate this page"
- `account.serviceRating.title` — "Rate your experience"
- `account.serviceRating.commentLabel` — "Optional comment"
- `account.serviceRating.submitButton` — "Submit"
- `account.serviceRating.toast.thanks` — "Thanks for your feedback."
- `account.serviceRating.starLabel` — "{{n}} of 5 stars"

`nav.register` — "Register" / "إنشاء حساب" (added to header nav-config when anonymous-only).

E2E nav smoke at `apps/web-portal-e2e/src/account.spec.ts`:

```ts
import { test, expect } from '@playwright/test';

test.describe('account smoke', () => {
  test('header sign-in shows when anonymous', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('cce-header')).toBeAttached({ timeout: 15_000 });
    await expect(page.getByRole('button', { name: /sign in|تسجيل الدخول/i })).toBeVisible();
  });

  test('/register attaches the register page (anonymous)', async ({ page }) => {
    await page.goto('/register');
    await expect(page.locator('cce-register')).toBeAttached({ timeout: 10_000 });
  });

  test('/me/profile bounces anonymous user back to home (or sign-in flow)', async ({ page }) => {
    await page.goto('/me/profile');
    // authGuard.signIn(returnUrl) → window.location.assign('/auth/login?...').
    // In test (no real BFF), the assign is a no-op or 404; verify the page does
    // NOT mount cce-profile-page.
    await page.waitForLoadState('networkidle');
    await expect(page.locator('cce-profile-page')).toHaveCount(0);
  });
});
```

Commit: `feat(web-portal): /register + /me routes + i18n + E2E (Phase 6.8)`

---

## Phase 06 — completion checklist

- [ ] Task 6.1 — AccountApiService + DTOs (~7 tests).
- [ ] Task 6.2 — RegisterPage (~3 tests).
- [ ] Task 6.3 — ProfilePage read mode (~3 tests).
- [ ] Task 6.4 — ProfilePage edit mode (~5 tests).
- [ ] Task 6.5 — ExpertRequestPage submit + status (~5 tests).
- [ ] Task 6.6 — ServiceRatingDialog (~4 tests).
- [ ] Task 6.7 — Production-grade authGuard (~4 tests).
- [ ] Task 6.8 — Routes + i18n + E2E + footer "Rate this page" wiring.
- [ ] All Jest tests passing.
- [ ] admin-cms still 218/218.
- [ ] Lint + build clean.

**If all boxes ticked, Phase 06 complete. Proceed to Phase 07 (Notifications + Follows).**

---

## Phase 9 polish backlog notes

- **Profile concurrency token**: backend `UserProfileDto` does not expose a row version; v0.1.0 is last-write-wins. If the backend grows a `version: string` column, FE adds an If-Match header on the PUT.
- **Real consumer for ServiceRatingDialog**: footer "Rate this page" button in Phase 6.8 is the only invocation site. Phase 9 may add additional triggers (after a satisfying flow, e.g., 30s on a resource page).
- **Country resolution in ProfilePage**: uses `CountriesApiService.listCountries({})` ad-hoc; consider promoting a small `CountryRegistry` service if more pages need it.
