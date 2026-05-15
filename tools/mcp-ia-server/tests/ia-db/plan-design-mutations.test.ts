/**
 * Mutation + query tests for ia_plan_designs (mig 0158).
 *
 * Skip when no IA DB pool is available. Exercises the round-trip:
 *
 *   - insert (status='draft', priority='P2' default)
 *   - update body_md + stages_yaml + status='ready' + priority='P0'
 *   - get round-trip (priority/status/body_md/stages_yaml read back)
 *   - list with status/priority filter (sorted P0 first)
 *   - promote → status='consumed' (idempotent on second call)
 *   - master_plan_bundle_apply with design_id auto-flips seed to consumed
 *
 * Uses sandbox seed slug `plan-design-test-seed`. Cleanup in after()
 * removes the seed row + any linked master plan row.
 */

import assert from "node:assert/strict";
import { after, before, describe, it } from "node:test";
import { getIaDatabasePool } from "../../src/ia-db/pool.js";
import {
  mutatePlanDesignInsert,
  mutatePlanDesignPromote,
  mutatePlanDesignUpdate,
  queryPlanDesignGet,
  queryPlanDesignList,
} from "../../src/ia-db/mutations/plan-design.js";

const SEED_SLUG = "plan-design-test-seed";
const BUNDLE_SEED_SLUG = "plan-design-test-bundle-seed";
const BUNDLE_PLAN_SLUG = "plan-design-test-bundle-plan";

const pool = getIaDatabasePool();
const skip = pool === null ? { skip: "DB pool unavailable" } : {};

async function cleanup(): Promise<void> {
  if (!pool) return;
  await pool.query(
    `DELETE FROM ia_master_plans WHERE slug = ANY($1::text[])`,
    [[BUNDLE_PLAN_SLUG]],
  );
  await pool.query(
    `DELETE FROM ia_plan_designs WHERE slug = ANY($1::text[])`,
    [[SEED_SLUG, BUNDLE_SEED_SLUG]],
  );
}

describe("ia_plan_designs mutations (mig 0158)", skip, () => {
  before(async () => {
    await cleanup();
  });

  after(async () => {
    await cleanup();
  });

  it("insert + get round-trip with defaults", async () => {
    const ins = await mutatePlanDesignInsert({
      slug: SEED_SLUG,
      title: "Test Seed",
    });
    assert.equal(ins.slug, SEED_SLUG);
    assert.equal(ins.status, "draft");
    assert.equal(ins.priority, "P2");
    assert.ok(ins.id > 0);

    const row = await queryPlanDesignGet(SEED_SLUG);
    assert.ok(row);
    assert.equal(row!.slug, SEED_SLUG);
    assert.equal(row!.title, "Test Seed");
    assert.equal(row!.status, "draft");
    assert.equal(row!.priority, "P2");
    assert.equal(row!.body_md, null);
    assert.equal(row!.stages_yaml, null);
    assert.equal(row!.target_version, 1);
  });

  it("insert duplicate slug rejected", async () => {
    await assert.rejects(
      () =>
        mutatePlanDesignInsert({
          slug: SEED_SLUG,
          title: "Duplicate",
        }),
      /plan_design_slug_collision/,
    );
  });

  it("update priority + status + body_md + stages_yaml", async () => {
    const stagesYaml = {
      stages: [{ stage_id: "1", title: "S1", status: "pending" }],
      tasks: [
        {
          prefix: "TECH",
          depends_on: [],
          digest_outline: "outline",
          touched_paths: [],
          kind: "implementation",
        },
      ],
    };
    const upd = await mutatePlanDesignUpdate({
      slug: SEED_SLUG,
      priority: "P0",
      status: "ready",
      body_md: "# Test\n\nBody.",
      stages_yaml: stagesYaml,
    });
    assert.equal(upd.priority, "P0");
    assert.equal(upd.status, "ready");

    const row = await queryPlanDesignGet(SEED_SLUG);
    assert.ok(row);
    assert.equal(row!.priority, "P0");
    assert.equal(row!.status, "ready");
    assert.equal(row!.body_md, "# Test\n\nBody.");
    assert.deepEqual(row!.stages_yaml, stagesYaml);
  });

  it("update rejects invalid priority", async () => {
    await assert.rejects(
      () =>
        mutatePlanDesignUpdate({
          slug: SEED_SLUG,
          priority: "P99",
        }),
      /priority must be one of P0/,
    );
  });

  it("list filters by priority + status; sorted P0 first", async () => {
    const rows = await queryPlanDesignList({ status: "ready", priority: "P0" });
    assert.ok(rows.length >= 1);
    assert.ok(rows.find((r) => r.slug === SEED_SLUG));
    assert.equal(rows[0]!.priority, "P0");
  });

  it("promote flips to consumed (idempotent on second call)", async () => {
    const first = await mutatePlanDesignPromote(SEED_SLUG);
    assert.equal(first.status, "consumed");
    assert.equal(first.already_consumed, false);

    const second = await mutatePlanDesignPromote(SEED_SLUG);
    assert.equal(second.status, "consumed");
    assert.equal(second.already_consumed, true);

    const row = await queryPlanDesignGet(SEED_SLUG);
    assert.equal(row!.status, "consumed");
  });

  it("get returns null for missing slug", async () => {
    const row = await queryPlanDesignGet("does-not-exist-anywhere");
    assert.equal(row, null);
  });

  it("master_plan_bundle_apply auto-flips seed to consumed (mig 0159)", async () => {
    if (!pool) return;
    const ins = await mutatePlanDesignInsert({
      slug: BUNDLE_SEED_SLUG,
      title: "Bundle Test Seed",
      priority: "P1",
    });
    await mutatePlanDesignUpdate({
      slug: BUNDLE_SEED_SLUG,
      status: "ready",
    });

    const bundle = {
      plan: {
        slug: BUNDLE_PLAN_SLUG,
        title: "Bundle Test Plan",
        version: 1,
        priority: "P1",
        design_id: ins.id,
      },
      stages: [],
      tasks: [],
    };

    const res = await pool.query<{ result: unknown }>(
      `SELECT master_plan_bundle_apply($1::jsonb) AS result`,
      [JSON.stringify(bundle)],
    );
    const result = res.rows[0]!.result as {
      plan_slug: string;
      apply_path: string;
      priority: string;
      design_id_linked: number;
      design_seed_promoted: boolean;
    };
    assert.equal(result.plan_slug, BUNDLE_PLAN_SLUG);
    assert.equal(result.apply_path, "insert_new");
    assert.equal(result.priority, "P1");
    assert.equal(result.design_id_linked, ins.id);
    assert.equal(result.design_seed_promoted, true);

    const seedRow = await queryPlanDesignGet(BUNDLE_SEED_SLUG);
    assert.equal(seedRow!.status, "consumed");

    const planRow = await pool.query<{
      priority: string;
      design_id: string | null;
    }>(`SELECT priority, design_id::text AS design_id FROM ia_master_plans WHERE slug = $1`, [
      BUNDLE_PLAN_SLUG,
    ]);
    assert.equal(planRow.rows[0]!.priority, "P1");
    assert.equal(parseInt(planRow.rows[0]!.design_id!, 10), ins.id);
  });
});
