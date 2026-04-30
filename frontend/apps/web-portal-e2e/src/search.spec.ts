import { test, expect } from '@playwright/test';

/**
 * Phase 05 search smoke. Anonymous user types in the header search box,
 * hits Enter, and the results page mounts at /search?q=.
 *
 * The full-stack run with the External API + Meilisearch + actual
 * indexed data is deferred to Phase 9 close-out; this spec only
 * verifies navigation + DOM mount + the empty-query hint state.
 */
test.describe('search nav smoke', () => {
  test('header search box → /search?q=carbon', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('cce-header')).toBeAttached({ timeout: 15_000 });
    const input = page.locator('cce-search-box input[type=search]');
    await input.fill('carbon');
    await input.press('Enter');
    await expect(page).toHaveURL(/\/search\?q=carbon/);
    await expect(page.locator('cce-search-results')).toBeAttached({ timeout: 10_000 });
  });

  test('empty query path renders "type a query" hint', async ({ page }) => {
    await page.goto('/search');
    await expect(page.locator('cce-search-results')).toBeAttached({ timeout: 10_000 });
    await expect(page.getByText(/type a query|اكتب استعلامك/i)).toBeVisible();
  });
});
