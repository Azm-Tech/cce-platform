import { test, expect } from '@playwright/test';

test.describe('web-portal layout', () => {
  test('cce-portal-shell renders with header + main + footer', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('cce-portal-shell')).toBeAttached({ timeout: 15_000 });
    await expect(page.locator('cce-header')).toBeAttached();
    await expect(page.locator('main')).toBeAttached();
    await expect(page.locator('cce-footer')).toBeAttached();
  });

  test('header has primary nav links + locale switcher + sign-in button', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('cce-header nav')).toBeAttached({ timeout: 15_000 });
    await expect(page.locator('cce-locale-switcher')).toBeAttached();
    await expect(page.locator('cce-search-box')).toBeAttached();
  });
});
