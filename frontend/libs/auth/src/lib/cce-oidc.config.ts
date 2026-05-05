import type { OpenIdConfiguration } from 'angular-auth-oidc-client';

export interface CceAuthEnv {
  authority: string;
  clientId: string;
  redirectUri: string;
  postLogoutRedirectUri: string;
}

/**
 * Build an angular-auth-oidc-client configuration for one of the CCE
 * Entra ID app registrations. Apps call this from their bootstrap with
 * values pulled from /assets/env.json so the same image deploys to
 * dev/staging/prod by swapping the runtime config file.
 *
 * Multi-tenant Entra ID — `authority` points at
 * `https://login.microsoftonline.com/<tenant>/v2.0` (or `/common` for
 * any-tenant); the BFF's IssuerValidator accepts any tenant matching the
 * canonical shape (see EntraIdIssuerValidator).
 */
export function buildCceOidcConfig(env: CceAuthEnv): OpenIdConfiguration {
  return {
    authority: env.authority,
    redirectUrl: env.redirectUri,
    postLogoutRedirectUri: env.postLogoutRedirectUri,
    clientId: env.clientId,
    // Entra ID standard scopes; adfs-compat (Keycloak-only) removed in Sub-11.
    scope: 'openid profile email offline_access',
    responseType: 'code',
    silentRenew: true,
    useRefreshToken: true,
    renewTimeBeforeTokenExpiresInSeconds: 30,
    usePushedAuthorisationRequests: false,
    triggerAuthorizationResultEvent: true,
    logLevel: 0,
  } as OpenIdConfiguration;
}
