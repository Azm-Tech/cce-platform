import { test, expect } from '@playwright/test';

/**
 * Phase 02 navigation smoke. Anonymous user clicks Knowledge Center in the
 * top nav and lands on the list page; the filter rail (CategoriesTreeComponent
 * + filter rail primitive from Phase 0.6) is mounted.
 *
 * Full-stack run (with the External API + actual data) is deferred to
 * Phase 9 close-out; this spec only verifies navigation + DOM structure.
 */
test.describe('knowledge center smoke', () => {
  test('navigates from header → /knowledge-center', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('cce-header')).toBeAttached({ timeout: 15_000 });

    const link = page.getByRole('link', { name: /knowledge center|مركز المعرفة/i });
    await link.first().click();

    await expect(page).toHaveURL(/\/knowledge-center/);
    await expect(page.locator('cce-resources-list, cce-filter-rail')).toBeAttached({ timeout: 10_000 });
  });

  test('resources-list page mounts the filter rail', async ({ page }) => {
    await page.goto('/knowledge-center');
    await expect(page.locator('cce-filter-rail')).toBeAttached({ timeout: 15_000 });
    await expect(page.locator('cce-categories-tree')).toBeAttached();
  });
});
