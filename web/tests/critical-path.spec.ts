// TECH-8609 / Stage 19.1 — Critical-path E2E smoke.
//
// Flow: seed admin → login (dev cookie fallback) → land on /dashboard →
// assert authenticated render (table or empty-state visible). Single happy
// path; selectors mirror the existing dashboard-filters.spec.ts row-locator
// pattern. Publish + sprite-list drilldown are out-of-scope for this smoke
// (would require seeded catalog content + dashboard sprite section that
// doesn't exist yet) — the smoke proves login + dashboard render only.
//
// Spec self-boots dev server via `webServer` block in playwright.config.ts.

import { expect, test } from "@playwright/test";

import { seedPlaywrightAdmin } from "./_fixtures/seed-admin";
import { loginAsPlaywrightAdmin } from "./_fixtures/login";

test("critical path: admin login lands on dashboard", async ({ page, context }) => {
  // First-compile cost on `next dev` can exceed Playwright's default 30s test
  // timeout when the dashboard route is hit cold. 90s covers compile + render
  // for local dev runs; CI uses a fresh build.
  test.setTimeout(90_000);

  await seedPlaywrightAdmin();
  await loginAsPlaywrightAdmin(context);

  await page.goto("/dashboard", { timeout: 60_000 });

  // Authenticated render: dashboard renders the "Master plan" section heading
  // once the proxy capability gate passes + plans load. Anonymous /
  // unauthorized renders return either a redirect or a 403 envelope — neither
  // surfaces this heading. Stable selector across populated/empty list since
  // the section heading renders unconditionally on success.
  await expect(
    page.getByRole("heading", { name: "Master plan", level: 1 }).or(
      page.getByText("Master plan", { exact: true }).first(),
    ),
  ).toBeVisible({ timeout: 15_000 });
});
