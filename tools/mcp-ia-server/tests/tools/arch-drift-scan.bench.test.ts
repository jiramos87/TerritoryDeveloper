/**
 * Perf bench for arch_drift_scan(scope='intra-plan') — TECH-5250.
 *
 * Seeds a synthetic 50-stage / 5-section / 200-changelog-row fixture,
 * runs the scan 20 times, computes P95, asserts < 200ms, and persists
 * the result to ia_arch_drift_bench so ia_master_plan_health can surface it.
 *
 * Changelog rows are seeded with created_at one year in the past so the
 * fixture exercises the full O(n_stages) query path (1 stageSql +
 * N_STAGES × driftSql) without triggering drift events — keeping the
 * hot path measurable without N²-style xs fanout.
 */

import test, { after, before, describe, it } from "node:test";
import assert from "node:assert/strict";
import { getIaDatabasePool } from "../../src/ia-db/pool.js";
import { runArchDriftScan } from "../../src/tools/arch.js";

const pool = getIaDatabasePool();
const skip = pool === null ? { skip: "DB pool unavailable" } : {};

const BENCH_SLUG = `pcr-drift-bench-${Date.now()}`;
const N_STAGES = 50;
const N_SECTIONS = 5;
const N_CHANGELOG = 200;
const ITERATIONS = 20;
const P95_BUDGET_MS = 200;

function surfaceSlug(i: number): string {
  return `__bench_${BENCH_SLUG}_surf_${i}__`;
}

async function seedBenchFixture(): Promise<void> {
  if (!pool) return;
  await pool.query(
    `INSERT INTO ia_master_plans (slug, title) VALUES ($1, $2) ON CONFLICT (slug) DO NOTHING`,
    [BENCH_SLUG, "arch_drift_scan perf bench fixture"],
  );
  for (let i = 0; i < N_STAGES; i++) {
    const stageId = `bench.${i + 1}`;
    const sectionId = `bench-sec-${(i % N_SECTIONS) + 1}`;
    await pool.query(
      `INSERT INTO ia_stages (slug, stage_id, title, status, section_id)
         VALUES ($1, $2, $3, 'pending', $4)
         ON CONFLICT (slug, stage_id) DO NOTHING`,
      [BENCH_SLUG, stageId, `bench stage ${stageId}`, sectionId],
    );
    const surf = surfaceSlug(i);
    await pool.query(
      `INSERT INTO arch_surfaces (slug, kind, spec_path)
         VALUES ($1, 'contract', 'bench/bench.md')
         ON CONFLICT (slug) DO NOTHING`,
      [surf],
    );
    await pool.query(
      `INSERT INTO stage_arch_surfaces (slug, stage_id, surface_slug)
         VALUES ($1, $2, $3)
         ON CONFLICT DO NOTHING`,
      [BENCH_SLUG, stageId, surf],
    );
  }
  // Changelog rows one year in the past — below every stage cutoff so
  // driftSql always returns [] (exercises per-stage query overhead only).
  for (let i = 0; i < N_CHANGELOG; i++) {
    const surf = surfaceSlug(i % N_STAGES);
    await pool.query(
      `INSERT INTO arch_changelog (kind, surface_slug, created_at)
         VALUES ('edit', $1, NOW() - INTERVAL '1 year')`,
      [surf],
    );
  }
}

async function teardownBenchFixture(): Promise<void> {
  if (!pool) return;
  for (let i = 0; i < N_STAGES; i++) {
    await pool.query(
      `DELETE FROM arch_changelog WHERE surface_slug = $1`,
      [surfaceSlug(i)],
    );
  }
  await pool.query(
    `DELETE FROM arch_surfaces WHERE slug LIKE $1`,
    [`__bench_${BENCH_SLUG}_%`],
  );
  await pool.query(`DELETE FROM ia_master_plans WHERE slug = $1`, [BENCH_SLUG]);
}

describe("arch_drift_scan.bench — P95 < 200ms on 50-stage 5-section fixture (TECH-5250)", skip, () => {
  before(async () => {
    await teardownBenchFixture();
    await seedBenchFixture();
  });

  after(async () => {
    await teardownBenchFixture();
  });

  it("arch_drift_scan.bench: P95 under 200ms on 50-stage 5-section 200-changelog fixture", async () => {
    if (!pool) return;
    const timings: number[] = [];
    for (let i = 0; i < ITERATIONS; i++) {
      const t0 = process.hrtime.bigint();
      await runArchDriftScan(pool, { plan_id: BENCH_SLUG, scope: "intra-plan" });
      const t1 = process.hrtime.bigint();
      timings.push(Number(t1 - t0) / 1_000_000);
    }
    timings.sort((a, b) => a - b);
    const p95Idx = Math.ceil(ITERATIONS * 0.95) - 1;
    const p95Ms = timings[p95Idx]!;

    await pool.query(
      `INSERT INTO ia_arch_drift_bench (p95_ms) VALUES ($1)`,
      [p95Ms],
    );
    await pool.query(`REFRESH MATERIALIZED VIEW ia_master_plan_health`);

    assert.ok(
      p95Ms < P95_BUDGET_MS,
      `arch_drift_scan P95 ${p95Ms.toFixed(1)}ms exceeds ${P95_BUDGET_MS}ms budget`,
    );
  });

  it("arch_drift_scan.bench: P95 measurement persists to ia_master_plan_health.arch_drift_scan_p95_ms", async () => {
    if (!pool) return;
    const res = await pool.query<{ arch_drift_scan_p95_ms: number | null }>(
      `SELECT arch_drift_scan_p95_ms FROM ia_master_plan_health WHERE slug = $1`,
      [BENCH_SLUG],
    );
    assert.ok(res.rows[0] !== undefined, "MV row missing for bench fixture slug");
    // pg returns `numeric` as string — check non-null and parseable, not typeof number
    const raw = res.rows[0].arch_drift_scan_p95_ms;
    assert.ok(
      raw !== null && !isNaN(parseFloat(String(raw))),
      `arch_drift_scan_p95_ms should be non-null numeric after bench run, got ${raw}`,
    );
  });
});
