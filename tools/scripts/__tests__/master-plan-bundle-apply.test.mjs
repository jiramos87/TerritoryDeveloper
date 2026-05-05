// TECH-12632 / ship-protocol Stage 1.0 — red-stage proof test.
//
// Locks `master_plan_bundle_apply` Postgres function behavior in the tracer
// chain:
//   - red phase (before TECH-12630 lands): function does not exist → Postgres
//     surfaces 42883 undefined_function; test exits non-zero with stderr
//     containing the substring `function master_plan_bundle_apply`.
//   - green phase (after TECH-12630 lands): function applies the bundle
//     atomically; row-count deltas across `ia_master_plans`, `ia_stages`,
//     `ia_tasks` are exactly +1 each; teardown returns DB to baseline.
//
// Fixture: tools/scripts/__tests__/fixtures/ship-protocol-tracer-toy.json
// (1 plan, 1 stage, 1 task — slug = ship-protocol-tracer-toy, stage_id = 1.0,
// task_key = T1.0.1) — authored by TECH-12631.
//
// Runner: `node --test tools/scripts/__tests__/master-plan-bundle-apply.test.mjs`.

import assert from "node:assert";
import { readFileSync } from "node:fs";
import * as path from "node:path";
import { test } from "node:test";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
import { resolveDatabaseUrl } from "../../postgres-ia/resolve-database-url.mjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..", "..");

const pgRequire = createRequire(
  path.join(REPO_ROOT, "tools/postgres-ia/package.json"),
);
const pg = pgRequire("pg");

const FIXTURE_PATH = path.join(
  __dirname,
  "fixtures",
  "ship-protocol-tracer-toy.json",
);

// ---------------------------------------------------------------------------
// TracerInsertsAtomically — red-stage proof + green-phase atomicity check.
// ---------------------------------------------------------------------------

test("TracerInsertsAtomically", async () => {
  const databaseUrl = resolveDatabaseUrl(REPO_ROOT);
  if (!databaseUrl) {
    // CI without DB → soft skip (red proof is local-only artifact).
    console.warn(
      "TracerInsertsAtomically: no DATABASE_URL; skipping (CI soft-skip).",
    );
    return;
  }

  const fixtureRaw = readFileSync(FIXTURE_PATH, "utf8");
  const bundle = JSON.parse(fixtureRaw);

  const pool = new pg.Pool({ connectionString: databaseUrl });

  try {
    // Best-effort idempotent teardown of any prior leak before measuring
    // baseline so re-runs start clean.
    await pool.query(
      `DELETE FROM ia_tasks       WHERE slug = $1`,
      [bundle.plan.slug],
    );
    await pool.query(
      `DELETE FROM ia_stages      WHERE slug = $1`,
      [bundle.plan.slug],
    );
    await pool.query(
      `DELETE FROM ia_master_plans WHERE slug = $1`,
      [bundle.plan.slug],
    );

    const baseline = await snapshotCounts(pool);

    // Invoke function — red phase: throws 42883 undefined_function.
    //                  green phase: returns {plan_slug, stages_inserted, tasks_inserted}.
    const callResult = await pool.query(
      "SELECT master_plan_bundle_apply($1::jsonb) AS result",
      [JSON.stringify(bundle)],
    );

    const ret = callResult.rows[0].result;
    assert.strictEqual(
      ret.plan_slug,
      bundle.plan.slug,
      "return.plan_slug must match fixture slug",
    );
    assert.strictEqual(
      ret.stages_inserted,
      1,
      "return.stages_inserted must be 1",
    );
    assert.strictEqual(
      ret.tasks_inserted,
      1,
      "return.tasks_inserted must be 1",
    );

    const post = await snapshotCounts(pool);
    assert.strictEqual(
      post.plans - baseline.plans,
      1,
      "ia_master_plans delta must be +1",
    );
    assert.strictEqual(
      post.stages - baseline.stages,
      1,
      "ia_stages delta must be +1",
    );
    assert.strictEqual(
      post.tasks - baseline.tasks,
      1,
      "ia_tasks delta must be +1",
    );

    // Teardown — child-to-parent FK order.
    await pool.query(`DELETE FROM ia_tasks       WHERE slug = $1`, [bundle.plan.slug]);
    await pool.query(`DELETE FROM ia_stages      WHERE slug = $1`, [bundle.plan.slug]);
    await pool.query(`DELETE FROM ia_master_plans WHERE slug = $1`, [bundle.plan.slug]);

    const teardown = await snapshotCounts(pool);
    assert.strictEqual(
      teardown.plans,
      baseline.plans,
      "post-teardown plans must match baseline",
    );
    assert.strictEqual(
      teardown.stages,
      baseline.stages,
      "post-teardown stages must match baseline",
    );
    assert.strictEqual(
      teardown.tasks,
      baseline.tasks,
      "post-teardown tasks must match baseline",
    );
  } finally {
    await pool.end();
  }
});

async function snapshotCounts(pool) {
  const [{ rows: p }, { rows: s }, { rows: t }] = await Promise.all([
    pool.query("SELECT count(*)::int AS n FROM ia_master_plans"),
    pool.query("SELECT count(*)::int AS n FROM ia_stages"),
    pool.query("SELECT count(*)::int AS n FROM ia_tasks"),
  ]);
  return { plans: p[0].n, stages: s[0].n, tasks: t[0].n };
}
