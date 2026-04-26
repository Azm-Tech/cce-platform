import { test, expect } from '@playwright/test';
import { expectNoA11yViolations } from './support/axe';

test.describe('admin-cms smoke', () => {
  test('renders shell with sign-in CTA before login', async ({ page }, testInfo) => {
    await page.goto('/');
    // Auto-login guard may immediately redirect; pre-redirect render also works.
    // Assert one of: sign-in button visible OR URL is on Keycloak realm.
    const onKeycloak = page.url().includes('/realms/cce-internal');
    if (!onKeycloak) {
      const signIn = page.getByRole('button', { name: /sign in|تسجيل الدخول/i });
      await expect(signIn).toBeVisible({ timeout: 10_000 });
      await expectNoA11yViolations(page, testInfo);
    } else {
      // Already redirected; just verify we're on Keycloak. No axe-core on third-party page.
      await expect(page).toHaveURL(/\/realms\/cce-internal/);
    }
  });

  test('clicking sign-in redirects to Keycloak realm', async ({ page }) => {
    await page.goto('/');
    if (page.url().includes('/realms/cce-internal')) {
      // Already redirected by autoLoginPartialRoutesGuard
      await expect(page).toHaveURL(/\/realms\/cce-internal/);
      return;
    }
    const signIn = page.getByRole('button', { name: /sign in|تسجيل الدخول/i });
    await signIn.click();
    await page.waitForURL(/\/realms\/cce-internal/, { timeout: 15_000 });
    await expect(page).toHaveURL(/\/realms\/cce-internal\/protocol\/openid-connect\/auth/);
  });
});
