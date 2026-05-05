import { test, expect } from '@playwright/test';

/**
 * Phase 06 account flows smoke. Verifies anonymous behavior of:
 * - the header sign-in button rendering
 * - /register attaching the register page
 * - /me/* bouncing anonymous users back through the auth flow
 *
 * Full-stack verification (real Entra ID + cookie session + profile
 * provisioning) is deferred to Phase 9 close-out.
 */
test.describe('account smoke', () => {
  test('header sign-in is visible when anonymous', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('cce-header')).toBeAttached({ timeout: 15_000 });
    await expect(page.getByRole('button', { name: /sign in|تسجيل الدخول/i })).toBeVisible();
  });

  test('/register attaches the register page', async ({ page }) => {
    // Sub-11 Phase 03: register page is now an info page with a Sign In
    // button (anonymous self-service deferred to Sub-11d).
    await page.goto('/register');
    await expect(page.locator('cce-register')).toBeAttached({ timeout: 10_000 });
    await expect(
      page.getByRole('button', { name: /sign in|تسجيل الدخول/i }),
    ).toBeVisible();
  });

  test('/me/profile does not mount the profile page for anonymous users', async ({ page }) => {
    // authGuard.signIn(returnUrl) calls window.location.assign('/auth/login?...').
    // Without a real BFF in this smoke run, the guard either bounces or 404s the
    // SPA-shell — either way, cce-profile-page MUST NOT render. Wait for the
    // header to attach as a stable signal that the SPA has booted, then verify
    // the guarded page never mounted.
    await page.goto('/me/profile');
    await expect(page.locator('cce-header')).toBeAttached({ timeout: 15_000 });
    await expect(page.locator('cce-profile-page')).toHaveCount(0);
  });
});
