/**
 * Runtime environment loaded from /assets/env.json at app bootstrap.
 * Same shape across web-portal and admin-cms; the values differ.
 */
export interface CceEnv {
  /** Logical environment name — "development" | "staging" | "production". */
  readonly environment: string;

  /** Backend API base URL — External API for web-portal, Internal API for admin-cms. */
  readonly apiBaseUrl: string;

  /** OIDC authority — full Entra ID v2.0 endpoint, e.g.
   *  `https://login.microsoftonline.com/common/v2.0` (multi-tenant) or
   *  `https://login.microsoftonline.com/<tenant-guid>/v2.0` (single-tenant override). */
  readonly oidcAuthority: string;

  /** OIDC client ID — the Entra ID app registration's Application (client) ID. Same value
   *  for both web-portal and admin-cms (they share one app registration with multiple
   *  redirect URIs; see infra/entra/app-registration-manifest.json). */
  readonly oidcClientId: string;

  /** Sentry DSN; empty string disables Sentry. */
  readonly sentryDsn: string;
}
