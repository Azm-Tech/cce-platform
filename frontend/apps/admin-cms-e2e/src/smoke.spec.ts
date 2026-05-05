import { test, expect } from '@playwright/test';
import { expectNoA11yViolations } from './support/axe';

// Sub-11 Phase 03: matches either the legacy Keycloak `/realms/` URL or the
// new Entra ID `login.microsoftonline.com` URL. Phase 04 cutover deletes the
// Keycloak surface; this regex tightens to Entra-only at that point.
const idpUrlPattern = /(\/realms\/cce-internal|login\.microsoftonline\.com)/;

test.describe('admin-cms smoke', () => {
  test('renders shell with sign-in CTA before login', async ({ page }, testInfo) => {
    // Track every URL the frame navigates through. The autoLoginPartialRoutesGuard from
    // Phase 12 redirects to the IdP as soon as the app bootstraps, so the sign-in CTA
    // may only be visible for a moment (or not at all) before the redirect fires.
    const visitedUrls: string[] = [];
    page.on('framenavigated', (frame) => {
      if (frame === page.mainFrame()) {
        visitedUrls.push(frame.url());
      }
    });

    await page.goto('/');
    // Let the app fully bootstrap (app initializer loads env.json + translations) and
    // give the OIDC guard time to either resolve auth state or trigger its redirect.
    // eslint-disable-next-line playwright/no-networkidle -- waiting on OIDC redirect chain that has no single deterministic selector
    await page.waitForLoadState('networkidle');

    const finalUrl = page.url();
    const onIdp =
      idpUrlPattern.test(finalUrl) || visitedUrls.some((u) => idpUrlPattern.test(u));

    if (!onIdp) {
      const signIn = page.getByRole('button', { name: /sign in|تسجيل الدخول/i });
      await expect(signIn).toBeVisible({ timeout: 10_000 });
      await expectNoA11yViolations(page, testInfo);
    } else {
      // Auto-login redirected us (or attempted to). Either we're on the IdP now, or
      // it rejected our request and bounced us back to /auth/callback?error=... .
      // Both outcomes prove the OIDC redirect path works.
      const passed =
        idpUrlPattern.test(finalUrl) ||
        finalUrl.includes('/auth/callback') ||
        visitedUrls.some((u) => idpUrlPattern.test(u));
      expect(passed).toBe(true);
    }
  });

  test('clicking sign-in redirects to the IdP', async ({ page }) => {
    // Capture navigations so we can assert an IdP URL was visited even if the
    // page later bounces back (e.g. IdP rejects the request and redirects to
    // /auth/callback?error=invalid_scope).
    const visitedUrls: string[] = [];
    page.on('framenavigated', (frame) => {
      if (frame === page.mainFrame()) {
        visitedUrls.push(frame.url());
      }
    });

    await page.goto('/');
    // eslint-disable-next-line playwright/no-networkidle -- waiting on OIDC redirect chain that has no single deterministic selector
    await page.waitForLoadState('networkidle');

    // If the auto-login guard already redirected through the IdP, we're done — the
    // OIDC redirect happened without a manual click.
    const alreadyVisitedIdp = visitedUrls.some((u) => idpUrlPattern.test(u));
    if (alreadyVisitedIdp || idpUrlPattern.test(page.url())) {
      expect(true).toBe(true);
      return;
    }

    // Otherwise click the sign-in CTA and confirm the next navigation is the IdP's
    // authorize endpoint. We use page.waitForRequest because a successful IdP hit
    // may immediately redirect back to /auth/callback?error=... (e.g. invalid_scope),
    // which would race with page.waitForURL(load).
    const signIn = page.getByRole('button', { name: /sign in|تسجيل الدخول/i });
    await expect(signIn).toBeVisible({ timeout: 10_000 });

    const idpRequest = page.waitForRequest(
      (req) => idpUrlPattern.test(req.url()),
      { timeout: 15_000 },
    );
    await signIn.click();
    const req = await idpRequest;
    expect(idpUrlPattern.test(req.url())).toBe(true);
  });
});
