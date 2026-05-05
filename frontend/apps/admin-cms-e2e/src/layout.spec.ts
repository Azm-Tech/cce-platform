import { test, expect } from '@playwright/test';

/**
 * Phase 0.5 layout regression guard. The smoke.spec.ts above proves the OIDC
 * redirect path. This spec proves the SPA shell itself — `<cce-shell>` with
 * `<mat-sidenav-container>` and the side-nav drawer — renders before any
 * redirect can fire. It blocks IdP network requests (Sub-11: Entra ID) so
 * the app stays mounted on the dev origin long enough for the assertions
 * to run.
 */
test.describe('admin-cms layout', () => {
  test.beforeEach(async ({ page }) => {
    // Block Entra ID traffic so the OIDC guard cannot redirect away from the
    // SPA. The auth-toolbar will show its sign-in CTA instead. Sub-11 Phase
    // 04 deleted the Keycloak path; this only blocks login.microsoftonline.com.
    await page.route('**/login.microsoftonline.com/**', (route) => route.abort());
  });

  test('cce-shell renders with mat-sidenav-container and side-nav drawer', async ({ page }) => {
    await page.goto('/');
    // Wait for the Angular app to bootstrap (env.json + translations + render).
    await expect(page.locator('cce-shell')).toBeAttached({ timeout: 15_000 });
    // Sidenav layout from Task 0.5.
    await expect(page.locator('mat-sidenav-container')).toBeAttached();
    await expect(page.locator('mat-sidenav')).toBeAttached();
    await expect(page.locator('cce-side-nav')).toBeAttached();
    // Existing chrome continues to slot in.
    await expect(page.locator('cce-app-shell')).toBeAttached();
    await expect(page.locator('cce-locale-switcher')).toBeAttached();
    await expect(page.locator('cce-auth-toolbar')).toBeAttached();
  });

  test('side-nav has zero links when unauthenticated (permission-gated)', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('cce-shell')).toBeAttached({ timeout: 15_000 });

    // Without an authenticated user, AuthService.hasPermission() returns false for
    // every nav item, so the *ccePermission directive hides them all.
    const links = page.locator('cce-side-nav a[mat-list-item]');
    await expect(links).toHaveCount(0);
  });
});
