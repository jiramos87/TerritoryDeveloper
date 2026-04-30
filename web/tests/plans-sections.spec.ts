// plans-sections.spec.ts — `/plans/[slug]/sections` E2E render gate.
//
// Stage 2.2 / TECH-5249 of `parallel-carcass-rollout`.
//
// Seeds the synthetic sections-visibility fixture (1 plan + 2 section stages
// + 1 carcass stage + 1 active claim row), boots the dev server target
// (PLAYWRIGHT_BASE_URL or :4000), then asserts:
//   1. BFF GET `/api/plans/{slug}/sections` returns 200 + `PlanSectionsBundle`
//      shape (sections[], carcass_stages[], warnings[],
//      claim_heartbeat_timeout_minutes, carcass_done).
//   2. `/plans/{slug}/sections` page renders 2 cards (one per section_id).
//   3. Held section A pill = `data-claim-state="active"`; free section B pill
//      = `data-claim-state="free"`.
//
// Cleanup hook truncates plan via cascade on the sandbox slug. Fixture pool
// is the same `IA_DATABASE_URL` the dev server reads — no separate test DB.
// DB-less CI: fixture short-circuits (returns handle without inserts) and
// these tests skip via `test.skip()` when the seed pool is null.

import { test, expect } from "@playwright/test";
import {
  seedSectionsVisibilityFixture,
  teardownSectionsVisibilityFixture,
  SECTIONS_VISIBILITY_FIXTURE_SLUG,
  type SectionsVisibilityFixtureHandle,
} from "../../tools/mcp-ia-server/tests/fixtures/parallel-carcass-sections-visibility.fixture";
import { getIaDatabasePool } from "../../tools/mcp-ia-server/src/ia-db/pool";

let fixture: SectionsVisibilityFixtureHandle | null = null;
let dbAvailable = false;

test.beforeAll(async () => {
  dbAvailable = getIaDatabasePool() !== null;
  if (!dbAvailable) return;
  fixture = await seedSectionsVisibilityFixture();
});

test.afterAll(async () => {
  if (!dbAvailable) return;
  await teardownSectionsVisibilityFixture();
});

// ---------------------------------------------------------------------------
// BFF JSON shape — 200 + PlanSectionsBundle
// ---------------------------------------------------------------------------
test("BFF /api/plans/[slug]/sections returns 200 + PlanSectionsBundle shape", async ({
  request,
}) => {
  test.skip(!dbAvailable, "IA_DATABASE_URL not configured — skipping E2E");
  const slug = fixture!.slug;

  const res = await request.get(`/api/plans/${slug}/sections`);
  expect(res.status()).toBe(200);

  const body = await res.json();
  expect(body.slug).toBe(slug);
  expect(Array.isArray(body.sections)).toBe(true);
  expect(Array.isArray(body.carcass_stages)).toBe(true);
  expect(Array.isArray(body.warnings)).toBe(true);
  expect(typeof body.claim_heartbeat_timeout_minutes).toBe("number");
  expect(typeof body.carcass_done).toBe("boolean");

  // Sections must contain section A + section B from fixture.
  const sectionIds = body.sections.map((s: { section_id: string }) => s.section_id).sort();
  expect(sectionIds).toEqual(["A", "B"]);

  // Section A claim populated; section B claim null.
  const sectionA = body.sections.find(
    (s: { section_id: string }) => s.section_id === "A",
  );
  const sectionB = body.sections.find(
    (s: { section_id: string }) => s.section_id === "B",
  );
  expect(sectionA?.claim).not.toBeNull();
  expect(sectionA?.claim?.last_heartbeat).toBeTruthy();
  expect(sectionB?.claim).toBeNull();
});

// ---------------------------------------------------------------------------
// Page renders 2 cards + correct claim pills (held / free)
// ---------------------------------------------------------------------------
test("/plans/[slug]/sections renders 2 cards w/ held + free pills", async ({
  page,
}) => {
  test.skip(!dbAvailable, "IA_DATABASE_URL not configured — skipping E2E");
  const slug = fixture!.slug;

  await page.goto(`/plans/${slug}/sections`);

  // Two section cards visible.
  const cards = page.locator('[data-testid="section-card"]');
  await expect(cards).toHaveCount(2);

  // Section A card: claim pill in `active` state (heartbeat fresh in fixture).
  const cardA = page.locator('[data-section-id="A"]');
  await expect(cardA).toBeVisible();
  const pillA = cardA.locator('[data-testid="claim-pill"]');
  await expect(pillA).toHaveAttribute("data-claim-state", "active");

  // Section B card: pill in `free` state (no claim row).
  const cardB = page.locator('[data-section-id="B"]');
  await expect(cardB).toBeVisible();
  const pillB = cardB.locator('[data-testid="claim-pill"]');
  await expect(pillB).toHaveAttribute("data-claim-state", "free");
});

// ---------------------------------------------------------------------------
// Sanity — fixture slug constant exported for downstream consumers
// ---------------------------------------------------------------------------
test("fixture slug constant matches seeded plan", async () => {
  test.skip(!dbAvailable, "IA_DATABASE_URL not configured — skipping E2E");
  expect(fixture!.slug).toBe(SECTIONS_VISIBILITY_FIXTURE_SLUG);
});
