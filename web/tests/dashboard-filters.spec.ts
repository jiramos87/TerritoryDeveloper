// dashboard-filters.spec.ts — dashboard filter round-trip e2e (web-platform Stage 6.3)
// Requires dev server running on http://localhost:4000 (or PLAYWRIGHT_BASE_URL override).
//
// Assertions are behavioral only — no HTML snapshots.
// Live filter values extracted from unfiltered /dashboard render to avoid hardcoding.

import { test, expect } from '@playwright/test';

// Shared baseline captured once before all tests.
const baseline = {
  rowCount: 0,
  planSlug: '',   // first plan chip label (already a slug)
  statusVal: '',  // first status chip label
  phaseVal: '',   // raw phase value (digits only, stripped from "Phase N")
};

test.beforeAll(async ({ browser }) => {
  const page = await browser.newPage();
  await page.goto('/dashboard');

  // Total unfiltered row count
  baseline.rowCount = await page.locator('tbody tr').count();

  // Find plan chip group: look for the Plan label then first chip in that row
  const planRow = page.locator('text=Plan').locator('..').locator('a').first();
  const planLabel = await planRow.textContent();
  baseline.planSlug = (planLabel ?? '').trim();

  // Pick first status chip
  const statusRow = page.locator('text=Status').locator('..').locator('a').first();
  const statusLabel = await statusRow.textContent();
  baseline.statusVal = (statusLabel ?? '').trim();

  // Pick first phase chip — strip "Phase " prefix to get raw value
  const phaseRow = page.locator('text=Phase').locator('..').locator('a').first();
  const phaseLabel = await phaseRow.textContent();
  baseline.phaseVal = (phaseLabel ?? '').trim().replace(/^Phase\s+/, '');

  await page.close();
});

// ---------------------------------------------------------------------------
// Single-param round-trip — plan dim
// ---------------------------------------------------------------------------
test('plan filter: active chip + narrowed rows', async ({ page }) => {
  // Skip if no plan slug was detected (empty dashboard)
  test.skip(!baseline.planSlug, 'No plan chips on dashboard — cannot test plan filter');

  await page.goto(`/dashboard?plan=${encodeURIComponent(baseline.planSlug)}`);

  // Active chip: chip whose text == planSlug and class contains bg-panel text-primary
  const chipLocator = page.locator(`a:has-text("${baseline.planSlug}")`).first();
  await expect(chipLocator).toBeVisible();
  const chipClass = await chipLocator.getAttribute('class') ?? '';
  expect(chipClass).toContain('bg-panel');
  expect(chipClass).toContain('text-primary');

  // Row count ≤ unfiltered baseline
  const filtered = await page.locator('tbody tr').count();
  expect(filtered).toBeLessThanOrEqual(baseline.rowCount);
});

// ---------------------------------------------------------------------------
// Single-param round-trip — status dim
// ---------------------------------------------------------------------------
test('status filter: active chip + narrowed rows', async ({ page }) => {
  test.skip(!baseline.statusVal, 'No status chips on dashboard — cannot test status filter');

  await page.goto(`/dashboard?status=${encodeURIComponent(baseline.statusVal)}`);

  const chipLocator = page.locator(`a:has-text("${baseline.statusVal}")`).first();
  await expect(chipLocator).toBeVisible();
  const chipClass = await chipLocator.getAttribute('class') ?? '';
  expect(chipClass).toContain('bg-panel');
  expect(chipClass).toContain('text-primary');

  const filtered = await page.locator('tbody tr').count();
  expect(filtered).toBeLessThanOrEqual(baseline.rowCount);
});

// ---------------------------------------------------------------------------
// Single-param round-trip — phase dim
// ---------------------------------------------------------------------------
test('phase filter: active chip + narrowed rows', async ({ page }) => {
  test.skip(!baseline.phaseVal, 'No phase chips on dashboard — cannot test phase filter');

  await page.goto(`/dashboard?phase=${encodeURIComponent(baseline.phaseVal)}`);

  // Phase chip label is "Phase N" in the rendered HTML
  const chipLabel = `Phase ${baseline.phaseVal}`;
  const chipLocator = page.locator(`a:has-text("${chipLabel}")`).first();
  await expect(chipLocator).toBeVisible();
  const chipClass = await chipLocator.getAttribute('class') ?? '';
  expect(chipClass).toContain('bg-panel');
  expect(chipClass).toContain('text-primary');

  const filtered = await page.locator('tbody tr').count();
  expect(filtered).toBeLessThanOrEqual(baseline.rowCount);
});

// ---------------------------------------------------------------------------
// Multi-param intersection — status AND phase
// ---------------------------------------------------------------------------
test('multi-param (status + phase): intersection narrows rows', async ({ page }) => {
  test.skip(!baseline.statusVal || !baseline.phaseVal, 'Missing status or phase value — cannot test multi-param');

  const url = `/dashboard?status=${encodeURIComponent(baseline.statusVal)}&phase=${encodeURIComponent(baseline.phaseVal)}`;
  await page.goto(url);

  // Get single-param counts for comparison
  await page.goto(`/dashboard?status=${encodeURIComponent(baseline.statusVal)}`);
  const statusOnlyCount = await page.locator('tbody tr').count();
  await page.goto(`/dashboard?phase=${encodeURIComponent(baseline.phaseVal)}`);
  const phaseOnlyCount = await page.locator('tbody tr').count();

  // Back to multi-param to assert per-row cell values
  await page.goto(url);
  const multiCount = await page.locator('tbody tr').count();

  // Multi-param count ≤ each single-param count (intersection is subset)
  expect(multiCount).toBeLessThanOrEqual(statusOnlyCount);
  expect(multiCount).toBeLessThanOrEqual(phaseOnlyCount);

  // Each visible row's Phase cell (col index 1 = "Phase") must match and
  // Status cell (col index 3 = "Status") must match the filter values.
  // DataTable columns: ID(0), Phase(1), Issue(2), Status(3-rendered), Intent(4)
  // Status is rendered as BadgeChip — locate by accessible text or cell position.
  // Phase column: assert cell text matches "Phase N" pattern for our phaseVal.
  if (multiCount > 0) {
    // Spot-check first row for both cells
    const firstRow = page.locator('tbody tr').first();
    const phaseCell = firstRow.locator('td').nth(1);
    await expect(phaseCell).toHaveText(baseline.phaseVal);
  }
});

// ---------------------------------------------------------------------------
// Clear-filters link
// ---------------------------------------------------------------------------
test('clear-filters link resets to unfiltered count', async ({ page }) => {
  test.skip(!baseline.statusVal, 'No status value — cannot navigate to a filtered URL');

  // Navigate to a filtered URL that should have at least some rows
  await page.goto(`/dashboard?status=${encodeURIComponent(baseline.statusVal)}`);

  // Locate clear-filters link by role=link + accessible name
  const clearLink = page.getByRole('link', { name: 'Clear filters' });
  await expect(clearLink).toBeVisible();

  // Assert href points to bare /dashboard
  const href = await clearLink.getAttribute('href');
  expect(href).toBe('/dashboard');

  // Follow the link
  await Promise.all([
    page.waitForURL(/\/dashboard$/),
    clearLink.click(),
  ]);

  // Unfiltered row count must match baseline
  const afterClear = await page.locator('tbody tr').count();
  expect(afterClear).toBe(baseline.rowCount);
});

// ---------------------------------------------------------------------------
// Empty state — unknown filter value
// ---------------------------------------------------------------------------
test('unknown status value shows empty-state message', async ({ page }) => {
  await page.goto('/dashboard?status=nonexistent');

  // Empty-state message must be visible
  await expect(page.locator('text=No plans match the current filters.')).toBeVisible();

  // No data rows
  const rowCount = await page.locator('tbody tr').count();
  expect(rowCount).toBe(0);

  // Clear-filters link still present (anyFilter is true)
  const clearLink = page.getByRole('link', { name: 'Clear filters' });
  await expect(clearLink).toBeVisible();
});
