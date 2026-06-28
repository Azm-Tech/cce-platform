/**
 * Curated aliases over the generated OpenAPI types.
 *
 * `external.ts` / `internal.ts` are GENERATED — do not edit them.
 * Regenerate with: pnpm nx run api-client:generate-types
 * (refreshes ../contracts/openapi.*.json from the live swagger URLs first).
 *
 * Only endpoints the backend has annotated with `.Produces<T>()` carry
 * response schemas. As of 2026-06-07 that covers resources, countries,
 * news, events, users, auth and more — but NOT yet: categories,
 * knowledge-maps, follows, admin countries/expert-requests/pages/audit-events.
 * For those, the hand-written types in each feature remain the source
 * of truth until the backend finishes annotating.
 *
 * ⚠️ Known spec/runtime mismatch: the spec declares enums (e.g.
 * `ResourceType`) as INTEGER values, but the live API serializes them
 * as STRINGS ("Article"). Reported to the backend 2026-06-07. Until
 * fixed, prefer the feature-level string unions for enum fields.
 */
import type { components as ExternalComponents, paths as ExternalPaths } from './external';
import type { components as InternalComponents, paths as InternalPaths } from './internal';

export type { ExternalComponents, ExternalPaths, InternalComponents, InternalPaths };

type ExtSchemas = ExternalComponents['schemas'];
type IntSchemas = InternalComponents['schemas'];

/* ── Generic shapes ─────────────────────────────────────── */

/** Standard 7-field response envelope: { success, code, message, data, errors, traceId, timestamp }. */
export type ApiEnvelope<T> = Omit<ExtSchemas['PublicResourceDtoResponse'], 'data'> & { data?: T };

/** Standard paged payload: { items, page, pageSize, total }. */
export type ApiPaged<T> = Omit<ExtSchemas['PublicResourceDtoPagedResult'], 'items'> & {
  items?: T[] | null;
};

/* ── External (web-portal) DTOs ─────────────────────────── */

export type PublicResourceDto = ExtSchemas['PublicResourceDto'];
export type PublicResourcePagedResponse = ExtSchemas['PublicResourceDtoPagedResultResponse'];
export type PublicResourceResponse = ExtSchemas['PublicResourceDtoResponse'];

export type PublicCountryDto = ExtSchemas['PublicCountryDto'];
export type PublicCountryPagedResponse = ExtSchemas['PublicCountryDtoPagedResultResponse'];

export type LoginRequest = ExtSchemas['LoginRequest'];
export type AuthLoginResponse = ExternalPaths['/api/auth/login']['post']['responses'];

/* ── Internal (admin-cms) DTOs ──────────────────────────── */

export type AdminResourceDto = IntSchemas['ResourceDto'];
export type AdminResourcePagedResponse = IntSchemas['ResourceDtoPagedResultResponse'];

export type AdminUserListItemDto = IntSchemas['UserListItemDto'];
export type AdminUserPagedResponse = IntSchemas['UserListItemDtoPagedResultResponse'];

export type AdminNewsDto = IntSchemas['NewsDto'];
export type AdminNewsPagedResponse = IntSchemas['NewsDtoPagedResultResponse'];

export type AdminCountryContentRequestDto = IntSchemas['CountryContentRequestDto'];
export type AdminCountryProfileDto = IntSchemas['CountryProfileDto'];
