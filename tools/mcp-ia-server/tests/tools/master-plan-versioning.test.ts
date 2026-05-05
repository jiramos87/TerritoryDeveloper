/**
 * master_plan_close + master_plan_version_create — TECH-12644.
 *
 * Versioning helpers paired with ship-final (Phase 6) and the start of a new
 * version cycle. `master_plan_close` flips `ia_master_plans.closed_at = now()`;
 * `master_plan_version_create` emits a v(N+1) row chained to the parent via
 * `parent_plan_slug` + monotonic `version` (migration 0066).
 *
 * Test matrix:
 *   - close H1: closed_at NULL → flips; result.already_closed=false.
 *   - close H2: closed_at non-NULL → idempotent; result.already_closed=true,
 *     timestamp preserved (no second flip).
 *   - close E1: missing slug → invalid_input.
 *   - version E1: parent closed_at NULL → IaDbValidationError code=parent_not_closed.
 *   - version H1: parent closed → child row with default slug `{parent}-v2`,
 *     version=2, parent_plan_slug=parent, closed_at NULL.
 *   - version E2: child slug collision → IaDbValidationError code=slug_collision.
 *   - version H2: explicit child_slug + title overrides honored.
 *
 * Sandbox slug `xtest-master-plan-versioning` (kebab-case for child_slug validator).
 */

import test from "node:test";
import assert from "node:assert/strict";
import { getIaDatabasePool } from "../../src/ia-db/pool.js";
import {
  IaDbValidationError,
  mutateMasterPlanClose,
  mutateMasterPlanVersionCreate,
} from "../../src/ia-db/mutations.js";

const pool = getIaDatabasePool();
const skip = pool === null ? { skip: "DB pool unavailable" } : {};

const SLUG = "xtest-master-plan-versioning";
const CHILD_DEFAULT = `${SLUG}-v2`;
const CHILD_EXPLICIT = "xtest-master-plan-versioning-v3";

async function teardown(): Promise<void> {
  if (!pool) return;
  // Children first (FK to parent), then parent.
  await pool.query(
    `DELETE FROM ia_master_plans WHERE parent_plan_slug = $1`,
    [SLUG],
  );
  await pool.query(
    `DELETE FROM ia_master_plans WHERE slug IN ($1, $2, $3)`,
    [SLUG, CHILD_DEFAULT, CHILD_EXPLICIT],
  );
}

async function seedParent(closed: boolean): Promise<void> {
  if (!pool) return;
  await teardown();
  await pool.query(
    `INSERT INTO ia_master_plans (slug, title, version)
       VALUES ($1, 'sandbox versioning plan', 1)`,
    [SLUG],
  );
  if (closed) {
    await pool.query(
      `UPDATE ia_master_plans SET closed_at = now() WHERE slug = $1`,
      [SLUG],
    );
  }
}

async function readClosedAt(slug: string): Promise<string | null> {
  if (!pool) return null;
  const r = await pool.query<{ closed_at: string | null }>(
    `SELECT closed_at::text AS closed_at FROM ia_master_plans WHERE slug = $1`,
    [slug],
  );
  return r.rows[0]?.closed_at ?? null;
}

// ---------------------------------------------------------------------------
// master_plan_close
// ---------------------------------------------------------------------------

test("master_plan_close H1: open plan → flips closed_at, already_closed=false", skip, async () => {
  if (!pool) return;
  try {
    await seedParent(false);
    const before = await readClosedAt(SLUG);
    assert.equal(before, null);

    const res = await mutateMasterPlanClose(SLUG);
    assert.equal(res.slug, SLUG);
    assert.equal(res.already_closed, false);
    assert.notEqual(res.closed_at, null);

    const after = await readClosedAt(SLUG);
    assert.notEqual(after, null);
  } finally {
    await teardown();
  }
});

test("master_plan_close H2: already-closed plan → idempotent, already_closed=true, ts preserved", skip, async () => {
  if (!pool) return;
  try {
    await seedParent(true);
    const initial = await readClosedAt(SLUG);
    assert.notEqual(initial, null);

    const res = await mutateMasterPlanClose(SLUG);
    assert.equal(res.already_closed, true);
    assert.equal(res.closed_at, initial, "closed_at must not advance on re-close");
  } finally {
    await teardown();
  }
});

test("master_plan_close E1: missing slug → IaDbValidationError", async () => {
  await assert.rejects(
    () => mutateMasterPlanClose(""),
    (e: unknown) => e instanceof IaDbValidationError,
  );
});

test("master_plan_close E2: unknown slug → IaDbValidationError", skip, async () => {
  if (!pool) return;
  await assert.rejects(
    () => mutateMasterPlanClose("__nope__"),
    (e: unknown) =>
      e instanceof IaDbValidationError && /not found/.test((e as Error).message),
  );
});

// ---------------------------------------------------------------------------
// master_plan_version_create
// ---------------------------------------------------------------------------

test("master_plan_version_create E1: parent not closed → parent_not_closed", skip, async () => {
  if (!pool) return;
  try {
    await seedParent(false);
    await assert.rejects(
      () => mutateMasterPlanVersionCreate({ parent_slug: SLUG }),
      (e: unknown) =>
        e instanceof IaDbValidationError &&
        (e as IaDbValidationError).code === "parent_not_closed",
    );
  } finally {
    await teardown();
  }
});

test("master_plan_version_create H1: closed parent → emits v2 with default slug", skip, async () => {
  if (!pool) return;
  try {
    await seedParent(true);
    const res = await mutateMasterPlanVersionCreate({ parent_slug: SLUG });
    assert.equal(res.parent_slug, SLUG);
    assert.equal(res.parent_version, 1);
    assert.equal(res.child_version, 2);
    assert.equal(res.child_slug, CHILD_DEFAULT);

    // Verify child row landed with parent linkage + closed_at NULL.
    const child = await pool.query<{
      parent_plan_slug: string | null;
      version: number;
      closed_at: string | null;
    }>(
      `SELECT parent_plan_slug, version, closed_at::text AS closed_at
         FROM ia_master_plans
        WHERE slug = $1`,
      [CHILD_DEFAULT],
    );
    assert.equal(child.rowCount, 1);
    assert.equal(child.rows[0]!.parent_plan_slug, SLUG);
    assert.equal(child.rows[0]!.version, 2);
    assert.equal(child.rows[0]!.closed_at, null);
  } finally {
    await teardown();
  }
});

test("master_plan_version_create E2: child slug collision → slug_collision", skip, async () => {
  if (!pool) return;
  try {
    await seedParent(true);
    await mutateMasterPlanVersionCreate({ parent_slug: SLUG });
    await assert.rejects(
      () => mutateMasterPlanVersionCreate({ parent_slug: SLUG }),
      (e: unknown) =>
        e instanceof IaDbValidationError &&
        (e as IaDbValidationError).code === "slug_collision",
    );
  } finally {
    await teardown();
  }
});

test("master_plan_version_create H2: explicit child_slug + title honored", skip, async () => {
  if (!pool) return;
  try {
    await seedParent(true);
    const res = await mutateMasterPlanVersionCreate({
      parent_slug: SLUG,
      child_slug: CHILD_EXPLICIT,
      title: "explicit override title",
    });
    assert.equal(res.child_slug, CHILD_EXPLICIT);
    assert.equal(res.child_version, 2);

    const r = await pool.query<{ title: string }>(
      `SELECT title FROM ia_master_plans WHERE slug = $1`,
      [CHILD_EXPLICIT],
    );
    assert.equal(r.rows[0]!.title, "explicit override title");
  } finally {
    await teardown();
  }
});

test("master_plan_version_create E3: missing parent_slug → invalid_input", async () => {
  await assert.rejects(
    () => mutateMasterPlanVersionCreate({ parent_slug: "" }),
    (e: unknown) => e instanceof IaDbValidationError,
  );
});
