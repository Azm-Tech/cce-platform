import { test, expect } from '@playwright/test';

/**
 * Phase 04 navigation smoke. Anonymous user clicks Countries from the
 * top nav and lands on the grid page.
 *
 * Full-stack run with the External API + actual data is deferred to
 * Phase 9 close-out; this spec only verifies navigation + DOM structure.
 */
test.describe('countries nav smoke', () => {
  test('navigates from header → /countries', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('cce-header')).toBeAttached({ timeout: 15_000 });
    await page.getByRole('link', { name: /^countries|الدول/i }).first().click();
    await expect(page).toHaveURL(/\/countries/);
    await expect(page.locator('cce-countries-grid')).toBeAttached({ timeout: 10_000 });
  });
});
