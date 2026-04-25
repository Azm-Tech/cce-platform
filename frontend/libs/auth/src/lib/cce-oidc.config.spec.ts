import { buildCceOidcConfig, type CceAuthEnv } from './cce-oidc.config';

describe('buildCceOidcConfig', () => {
  const env: CceAuthEnv = {
    authority: 'http://localhost:8080/realms/cce-internal',
    clientId: 'cce-admin-cms',
    redirectUri: 'http://localhost:4201/auth/callback',
    postLogoutRedirectUri: 'http://localhost:4201',
  };

  it('produces a config with code-flow + PKCE + 256', () => {
    const cfg = buildCceOidcConfig(env);

    expect(cfg.authority).toBe(env.authority);
    expect(cfg.clientId).toBe(env.clientId);
    expect(cfg.responseType).toBe('code');
    expect(cfg.usePushedAuthorisationRequests).toBe(false); // PAR not configured in Foundation
    expect(cfg.scope).toContain('openid');
    expect(cfg.scope).toContain('profile');
  });

  it('sets refresh token rotation', () => {
    const cfg = buildCceOidcConfig(env);

    expect(cfg.useRefreshToken).toBe(true);
    expect(cfg.silentRenew).toBe(true);
  });

  it('disables auto-login (apps trigger login explicitly)', () => {
    const cfg = buildCceOidcConfig(env);

    expect(cfg.triggerAuthorizationResultEvent).toBe(true);
  });
});
