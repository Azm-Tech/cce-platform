import { test, expect } from '@playwright/test';
import { expectNoA11yViolations } from './support/axe';

test.describe('web-portal smoke', () => {
  test('anonymous land — header + footer render and axe is clean', async ({ page }, testInfo) => {
    await page.goto('/');
    await expect(page.locator('cce-header')).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('cce-footer')).toBeVisible();
    await expect(page.getByRole('button', { name: /sign in|تسجيل الدخول/i })).toBeVisible();
    await expectNoA11yViolations(page, testInfo);
  });
});
