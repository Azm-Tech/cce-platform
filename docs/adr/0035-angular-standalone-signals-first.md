# ADR-0035 — Angular standalone components + signals-first state for admin CMS

**Status:** Accepted
**Date:** 2026-04-30
**Deciders:** CCE frontend team

---

## Context

Sub-project 5 (Admin CMS) is the first first-party Angular application in CCE. It must support ~30 admin screens (users, experts, content, taxonomies, country profiles, notifications, reports, audit), bilingual (ar/en) with RTL/LTR, and remain maintainable through ongoing feature development.

Modern Angular (v17+) offers two state-management styles that are both endorsed by the framework:

| Option | Notes |
|---|---|
| NgRx with selectors + effects | Explicit, redux-style; heavy boilerplate; long learning curve |
| RxJS BehaviorSubject + plain services | Familiar; verbose around derived state; no DI integration with templates |
| Angular signals (v17+) | First-class change-detection integration; concise; effect() for side-effects |
| Component Store / Akita | Smaller scope but adds another library + paradigm |

Module systems were also debated:

| Option | Notes |
|---|---|
| NgModules per feature | Long-standing pattern; requires module declaration plumbing |
| Standalone components (default since v15) | No module boilerplate; lazy-loading via `loadComponent` |

---

## Decision

The admin-cms application uses:

1. **Standalone components only.** Every component, directive, and pipe is `standalone: true`. No NgModules anywhere in `apps/admin-cms`. Lazy-loading uses `loadComponent` (single component) and `loadChildren` (route module returning a `Routes` array).

2. **Signals-first state.** Page components hold their state in `signal()` and `computed()`. Async data flows in via async/await on per-feature `*ApiService` wrappers. RxJS is used only for HTTP and dialog `afterClosed()` streams; everything else is signals.

3. **Reactive Forms with typed `FormGroup<T>`.** All editable forms use the `FormGroup<{...FormControl}>` shape with `nonNullable: true` and explicit validators. Template-driven forms are reserved for trivial single-input filters in list pages.

4. **Per-feature `*ApiService`** that wraps the generated `libs/api-client` (or hand-written `HttpClient` calls when the generated client emits `unknown` due to missing `Produces<T>()`). Each method returns a discriminated `Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError }` so pages can render typed errors without re-doing the HTTP discrimination per call.

---

## Consequences

**Positive:**
- No module boilerplate. Every new screen is a single TS file plus templates.
- Signals integrate with OnPush change detection automatically — fewer subscription lifetimes to track.
- Typed forms catch field-name typos at compile time.
- The `Result<T>` pattern keeps page controllers thin: pages render the kind via `errors.<kind>` i18n keys without translating `HttpErrorResponse` themselves.

**Negative:**
- Developers familiar with NgRx must learn a different pattern. The trade-off is justified by the small size of each page's state surface (typically <10 signals).
- Signals replace effects for derived state, but `effect()` for side-effects must be used carefully to avoid infinite loops. The team relies on review checklists rather than runtime safeguards.

**Mitigation:**
- The PermissionDirective demonstrates correct `effect()` usage (re-evaluate on `currentUser` signal changes). All future signal-driven directives reference it.
