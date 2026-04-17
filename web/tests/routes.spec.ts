// routes.spec.ts — public route smoke tests (web-platform Stage 6.2)
// Requires dev server running on http://localhost:4000 (or PLAYWRIGHT_BASE_URL override).

import { test, expect } from '@playwright/test';

const STATIC_ROUTES = ['/', '/about', '/install', '/history', '/wiki', '/devlog'];

for (const route of STATIC_ROUTES) {
  test(`${route} returns 200 and has a visible heading`, async ({ page }) => {
    const response = await page.goto(route);
    expect(response?.status()).toBe(200);
    await expect(page.locator('h1, h2').first()).toBeVisible();
  });
}

test('devlog first slug navigates to 200 with a visible heading', async ({ page }) => {
  const response = await page.goto('/devlog');
  expect(response?.status()).toBe(200);

  // Pick first anchor whose href starts with /devlog/ (i.e. a post slug, not the listing itself).
  const slugLink = page.locator('a[href^="/devlog/"]').first();
  await expect(slugLink).toBeVisible();

  await Promise.all([
    page.waitForURL(/\/devlog\/.+/),
    slugLink.click(),
  ]);

  expect(page.url()).toMatch(/\/devlog\//);
  await expect(page.locator('h1, h2').first()).toBeVisible();
});
