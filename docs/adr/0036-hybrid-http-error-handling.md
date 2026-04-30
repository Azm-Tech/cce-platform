# ADR-0036 — Hybrid HTTP error handling: global interceptor + per-feature wrappers

**Status:** Accepted
**Date:** 2026-04-30
**Deciders:** CCE frontend team

---

## Context

The admin-cms makes ~75 HTTP calls across feature pages. Each backend response can fail with:

- `0` — network failure
- `400` — FluentValidation ProblemDetails (field-keyed)
- `401` — session expired (must redirect to login)
- `403` — permission denied
- `404` — resource not found
- `409` — concurrency conflict OR duplicate (distinguished by `type` URN)
- `5xx` — server error

Some failures need cross-cutting handling (401 → redirect, 5xx → toast). Others need feature-domain handling (409/concurrency on Resource update → "someone else edited; reload"). A single global handler cannot distinguish, but per-call try/catch in every page bloats controllers.

Options:

| Option | Notes |
|---|---|
| Global interceptor only | Cross-cutting OK, but loses feature-level context |
| Per-call try/catch | Repetitive; no shared logic |
| Hybrid: interceptor for cross-cutting + per-feature wrapper | Both layers, no duplication |

---

## Decision

Use a **hybrid** model:

1. **Three functional `HttpInterceptorFn`s**, registered on `provideHttpClient(withInterceptors([...]))`:
   - `correlationIdInterceptor` — adds `X-Correlation-Id` UUID per request.
   - `authInterceptor` — sets `withCredentials: true`, redirects to `/auth/login` on 401 (skips `/api/me`).
   - `serverErrorInterceptor` — toasts `errors.server` on 5xx and `errors.forbidden` on 403.

2. **Per-feature `*ApiService` wrappers** that translate every error to a typed `FeatureError`:

   ```ts
   export type FeatureError =
     | { kind: 'concurrency' }
     | { kind: 'duplicate' }
     | { kind: 'validation'; fieldErrors: Record<string, string[]> }
     | { kind: 'not-found' }
     | { kind: 'forbidden' }
     | { kind: 'server' }
     | { kind: 'network' }
     | { kind: 'unknown'; status: number };
   ```

   Each service method returns `Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError }`. The mapping function `toFeatureError(HttpErrorResponse)` lives in `core/ui/error-formatter.ts` and is used by every wrapper.

3. **Page controllers** render error kinds through i18n: `('errors.' + error.kind) | translate`. They never see raw `HttpErrorResponse`. They can branch on `error.kind === 'concurrency'` to surface targeted UX (e.g., "reload to see latest").

---

## Consequences

**Positive:**
- A single `toFeatureError` mapping is the source of truth for what an HTTP response means. Page logic stays small.
- 5xx and 401 handling is centralised — every page benefits without per-page wiring.
- New features cannot accidentally drop network or server errors; the wrapper catches everything.

**Negative:**
- The 5xx interceptor toasts on 5xx; the per-feature wrapper also returns `{ kind: 'server' }`. Pages that surface a banner risk double-notification (banner + toast).
  - **Mitigation:** Pages that show a banner choose not to also call `toast.error()` on `server` kind. The plan documents this.
- The validation kind carries `fieldErrors`; consumers must remember to use them. Most forms use them via `mat-error` on each field.

**Verification:**
- `core/ui/error-formatter.spec.ts` covers every mapping branch.
- Each `*ApiService.spec.ts` confirms the URL + the relevant error path (404, 409, 5xx).
