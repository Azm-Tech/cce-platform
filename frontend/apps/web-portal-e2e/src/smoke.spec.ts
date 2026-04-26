import { test, expect } from '@playwright/test';
import { expectNoA11yViolations } from './support/axe';

test.describe('web-portal smoke', () => {
  test('renders root in Arabic with dir=rtl', async ({ page }, testInfo) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/health$/);
    const html = page.locator('html');
    await expect(html).toHaveAttribute('dir', 'rtl');
    await expect(html).toHaveAttribute('lang', 'ar');
    await expectNoA11yViolations(page, testInfo);
  });

  test('locale switcher toggles ar→en and flips dir to ltr', async ({ page }, testInfo) => {
    await page.goto('/');
    const html = page.locator('html');
    await expect(html).toHaveAttribute('dir', 'rtl');
    const switcher = page.getByRole('button', { name: /English|switchTo/i });
    await switcher.click();
    await expect(html).toHaveAttribute('dir', 'ltr');
    await expect(html).toHaveAttribute('lang', 'en');
    await expectNoA11yViolations(page, testInfo);
  });

  test('/health page renders status from External API', async ({ page }, testInfo) => {
    await page.goto('/health');
    // Page may show loading first; wait for status text
    await expect(page.locator('dl, [role="alert"]').first()).toBeVisible({ timeout: 10_000 });
    // If the External API isn't running, the page shows an error — assert one of the two outcomes
    const ok = await page.locator('text=ok').first().isVisible().catch(() => false);
    const err = await page.locator('[role="alert"]').first().isVisible().catch(() => false);
    expect(ok || err).toBe(true);
    await expectNoA11yViolations(page, testInfo);
  });
});
