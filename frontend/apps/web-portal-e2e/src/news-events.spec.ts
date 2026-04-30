import { test, expect } from '@playwright/test';

/**
 * Phase 03 navigation smoke. Anonymous user clicks News + Events from the
 * top nav and lands on the corresponding list pages.
 *
 * Full-stack run with the External API + actual data is deferred to
 * Phase 9 close-out; this spec only verifies navigation + DOM structure.
 */
test.describe('news + events nav smoke', () => {
  test('navigates from header → /news', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('cce-header')).toBeAttached({ timeout: 15_000 });
    await page.getByRole('link', { name: /^news|الأخبار/i }).first().click();
    await expect(page).toHaveURL(/\/news/);
    await expect(page.locator('cce-news-list')).toBeAttached({ timeout: 10_000 });
  });

  test('navigates from header → /events', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('cce-header')).toBeAttached({ timeout: 15_000 });
    await page.getByRole('link', { name: /^events|الفعاليات/i }).first().click();
    await expect(page).toHaveURL(/\/events/);
    await expect(page.locator('cce-events-list')).toBeAttached({ timeout: 10_000 });
  });
});
