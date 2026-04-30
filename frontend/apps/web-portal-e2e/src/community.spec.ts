import { test, expect } from '@playwright/test';

/**
 * Phase 08 community smoke. Verifies the anonymous routing tree mounts:
 * - /community attaches the topics-list page
 * - /community/topics/{slug} attaches the topic-detail page (or
 *   not-found block when slug doesn't exist; both are valid mounts)
 * - /community/posts/{id} attaches the post-detail page
 *
 * Authenticated write flows (compose post / reply / rate / mark-answer)
 * are deferred to Phase 9 close-out for full-stack verification.
 */
test.describe('community smoke', () => {
  test('header → /community attaches topics-list page', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('cce-header')).toBeAttached({ timeout: 15_000 });
    const link = page.getByRole('link', { name: /^community|المجتمع/i });
    await link.first().click();
    await expect(page).toHaveURL(/\/community/);
    await expect(page.locator('cce-topics-list-page')).toBeAttached({ timeout: 10_000 });
  });

  test('/community/topics/some-slug mounts the topic-detail page', async ({ page }) => {
    await page.goto('/community/topics/some-slug');
    await expect(page.locator('cce-topic-detail-page')).toBeAttached({ timeout: 10_000 });
  });

  test('/community/posts/some-id mounts the post-detail page', async ({ page }) => {
    await page.goto('/community/posts/some-id');
    await expect(page.locator('cce-post-detail-page')).toBeAttached({ timeout: 10_000 });
  });
});
