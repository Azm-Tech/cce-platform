import { test, expect } from '@playwright/test';

/**
 * Phase 07 smoke. Verifies the anonymous behavior of:
 * - the header bell is NOT visible when not authenticated
 * - /me/notifications and /me/follows do NOT mount their pages for
 *   anonymous users (authGuard from Phase 6.7 bounces them)
 *
 * Authenticated full-stack run is deferred to Phase 9 close-out.
 */
test.describe('notifications + follows smoke', () => {
  test('header bell is not rendered when anonymous', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('cce-header')).toBeAttached({ timeout: 15_000 });
    await expect(page.locator('.cce-header__bell')).toHaveCount(0);
  });

  test('/me/notifications does not mount for anonymous users', async ({ page }) => {
    await page.goto('/me/notifications');
    await expect(page.locator('cce-header')).toBeAttached({ timeout: 15_000 });
    await expect(page.locator('cce-notifications-page')).toHaveCount(0);
  });

  test('/me/follows does not mount for anonymous users', async ({ page }) => {
    await page.goto('/me/follows');
    await expect(page.locator('cce-header')).toBeAttached({ timeout: 15_000 });
    await expect(page.locator('cce-follows-page')).toHaveCount(0);
  });
});
