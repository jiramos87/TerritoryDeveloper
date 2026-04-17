// meta.spec.ts — robots / sitemap / RSS contract tests (web-platform Stage 6.2)
// Requires dev server running on http://localhost:4000 (or PLAYWRIGHT_BASE_URL override).

import { test, expect } from '@playwright/test';

test('robots.txt disallows /dashboard', async ({ request }) => {
  const res = await request.get('/robots.txt');
  expect(res.status()).toBe(200);
  const body = await res.text();
  expect(body).toContain('Disallow: /dashboard');
});

test('sitemap.xml lists at least one devlog URL', async ({ request }) => {
  const res = await request.get('/sitemap.xml');
  expect(res.status()).toBe(200);
  const body = await res.text();
  expect(body).toContain('/devlog/');
});

test('feed.xml content-type is application/rss+xml', async ({ request }) => {
  const res = await request.get('/feed.xml');
  expect(res.status()).toBe(200);
  const contentType = res.headers()['content-type'] ?? '';
  expect(contentType).toMatch(/application\/rss\+xml/);
});
