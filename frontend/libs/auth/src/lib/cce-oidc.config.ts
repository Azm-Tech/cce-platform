import type { OpenIdConfiguration } from 'angular-auth-oidc-client';

export interface CceAuthEnv {
  authority: string;
  clientId: string;
  redirectUri: string;
  postLogoutRedirectUri: string;
}

/**
 * Build an angular-auth-oidc-client configuration for one of the CCE Keycloak realms.
 * Apps call this from their bootstrap with values pulled from /assets/env.json so the
 * same image deploys to dev/staging/prod by swapping the runtime config file.
 */
export function buildCceOidcConfig(env: CceAuthEnv): OpenIdConfiguration {
  return {
    authority: env.authority,
    redirectUrl: env.redirectUri,
    postLogoutRedirectUri: env.postLogoutRedirectUri,
    clientId: env.clientId,
    scope: 'openid profile email adfs-compat offline_access',
    responseType: 'code',
    silentRenew: true,
    useRefreshToken: true,
    renewTimeBeforeTokenExpiresInSeconds: 30,
    usePushedAuthorisationRequests: false,
    triggerAuthorizationResultEvent: true,
    logLevel: 0,
  } as OpenIdConfiguration;
}
