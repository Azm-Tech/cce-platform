/**
 * Runtime environment loaded from /assets/env.json at app bootstrap.
 * Same shape across web-portal and admin-cms; the values differ.
 */
export interface CceEnv {
  /** Logical environment name — "development" | "staging" | "production". */
  readonly environment: string;

  /** Backend API base URL — External API for web-portal, Internal API for admin-cms. */
  readonly apiBaseUrl: string;

  /** OIDC authority (full Keycloak realm URL). */
  readonly oidcAuthority: string;

  /** OIDC client ID — `cce-web-portal` or `cce-admin-cms`. */
  readonly oidcClientId: string;

  /** Sentry DSN; empty string disables Sentry. */
  readonly sentryDsn: string;
}
