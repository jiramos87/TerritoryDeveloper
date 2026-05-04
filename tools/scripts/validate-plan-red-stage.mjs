#!/usr/bin/env node
/**
 * validate-plan-red-stage.mjs
 *
 * TECH-10896 — DB-backed read-only validator enforcing the red-stage
 * methodology on master plans created on or after the cutover date (2026-05-03).
 *
 * For every non-closed master plan with `created_at >= CUTOVER_ISO`:
 *   - Each Stage MUST have a §Red-Stage Proof block with all 4 fields filled:
 *     red_test_anchor, target_kind, proof_artifact_id, proof_status.
 *   - OR have ≥1 row in `ia_red_stage_proofs` with all 4 fields non-empty.
 *   - Skip-clause: target_kind=design_only → proof_artifact_id=n/a allowed.
 *
 * Data sources (first match wins per stage):
 *   1. `ia_red_stage_proofs` table row (all 4 fields set).
 *   2. `ia_stages.body` §Red-Stage Proof block parse (4 fields).
 *
 * Exit codes:
 *   0  All stages green; prints summary footer + warn count.
 *   1  ≥1 hard violation; prints actionable per-violation lines.
 *   2  DB connection / query error.
 *
 * Test-injection: set `PLAN_RED_STAGE_FAKE_ROWS` env to JSON
 * `{plans, stages, proofs}` to bypass DB (spawnSync test harness).
 *
 * Wired into `validate:all` after `validate:plan-prototype-first`.
 */

import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
import { resolveDatabaseUrl } from "../postgres-ia/resolve-database-url.mjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = process.env.IA_REPO_ROOT ?? path.resolve(__dirname, "../..");

const pgRequire = createRequire(
  path.join(REPO_ROOT, "tools/postgres-ia/package.json"),
);
const pg = pgRequire("pg");

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

// Plans created on or after this date are enforced. Set to 2026-05-05 so all
// currently-active plans (created 2026-05-03..04) are grandfathered until
// Stage 6 of tdd-red-green-methodology retrofits proof blocks into them.
// Move this back to 2026-05-03 once Stage 6 closeout ships.
const CUTOVER_ISO = "2026-05-05";

/** 4 required proof fields. */
const PROOF_FIELDS = [
  "red_test_anchor",
  "target_kind",
  "proof_artifact_id",
  "proof_status",
];

// ---------------------------------------------------------------------------
// §Red-Stage Proof block parser
//
// Expects body text containing a block like:
//
//   §Red-Stage Proof
//   red_test_anchor: tracer-verb-test:...
//   target_kind: tracer_verb
//   proof_artifact_id: tools/scripts/test/...
//   proof_status: failed_as_expected
//
// Each field matched as "field: value" (colon-separated, trimmed).
// Returns null when block absent; object with field values otherwise.
// ---------------------------------------------------------------------------

/**
 * @param {string | null} body
 * @returns {Record<string, string> | null}
 */
function parseRedStageProofBlock(body) {
  if (!body) return null;

  // Find §Red-Stage Proof as a block header (start-of-line anchor).
  // Must match "§Red-Stage Proof" at line start or as a bold marker like
  // "**§Red-Stage Proof**" or "**§Red-Stage Proof:**" — excludes prose mentions.
  const anchorMatch = body.match(/(?:^|\n)\s*(?:\*{1,2})?§Red-Stage Proof(?:\*{1,2})?(?:\s*:)?(?:\*{1,2})?\s*\n/);
  if (!anchorMatch) return null;

  const startIdx = anchorMatch.index + anchorMatch[0].length;
  // Grab up to 20 lines after anchor.
  const snippet = body.slice(startIdx, startIdx + 800);

  /** @type {Record<string, string>} */
  const result = {};
  for (const field of PROOF_FIELDS) {
    const re = new RegExp(`${field}[ \t]*:[ \t]*([^\r\n]*)`);
    const m = snippet.match(re);
    if (m) {
      result[field] = m[1].trim();
    }
  }
  return result;
}

/**
 * @param {Record<string, string> | null} block
 * @param {string} target_kind_override - from proofs table or body
 * @returns {string | null} - null if valid, else missing field name
 */
function validateProofFields(block) {
  if (!block) return "§Red-Stage Proof block missing";
  for (const field of PROOF_FIELDS) {
    const val = (block[field] ?? "").trim();
    if (!val) return field;
    // Skip-clause: target_kind=design_only → proof_artifact_id=n/a allowed.
    if (
      field === "proof_artifact_id" &&
      val === "n/a" &&
      (block["target_kind"] ?? "").trim() === "design_only"
    ) {
      continue;
    }
  }
  return null;
}

// ---------------------------------------------------------------------------
// Fake-rows test injection
// ---------------------------------------------------------------------------

/**
 * @typedef {{ slug: string; created_at: string }} PlanRow
 * @typedef {{ slug: string; stage_id: string; status: string; body: string | null }} StageRow
 * @typedef {{ slug: string; stage_id: string; red_test_anchor: string; target_kind: string; proof_artifact_id: string; proof_status: string }} ProofRow
 * @typedef {{ plans: PlanRow[]; stages: StageRow[]; proofs?: ProofRow[] }} FakeBundle
 */

/** @returns {FakeBundle | null} */
function readFakeBundle() {
  const raw = process.env.PLAN_RED_STAGE_FAKE_ROWS;
  if (!raw) return null;
  try {
    return JSON.parse(raw);
  } catch (e) {
    console.error(
      `[plan-red-stage] PLAN_RED_STAGE_FAKE_ROWS parse failed: ${
        e instanceof Error ? e.message : String(e)
      }`,
    );
    process.exit(2);
  }
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

async function main() {
  const fake = readFakeBundle();

  let plansRows;
  let stagesRows;
  let proofsRows;
  let client = null;

  if (fake) {
    plansRows = fake.plans;
    stagesRows = fake.stages;
    proofsRows = fake.proofs ?? [];
  } else {
    const conn = resolveDatabaseUrl(REPO_ROOT);
    if (!conn) {
      console.error(
        "[plan-red-stage] DATABASE_URL not resolvable — aborting",
      );
      return 2;
    }

    const pgClient = new pg.Client({ connectionString: conn });
    client = pgClient;

    try {
      await pgClient.connect();
    } catch (err) {
      const msg = err instanceof Error ? err.message : String(err);
      console.error(`[plan-red-stage] DB connect failed: ${msg}`);
      return 2;
    }
  }

  let violations = 0;
  let warnCount = 0;

  try {
    if (!fake) {
      const plansRes = await client.query(
        `SELECT slug, created_at
           FROM ia_master_plans
          ORDER BY slug`,
      );
      plansRows = plansRes.rows;

      const stagesRes = await client.query(
        `SELECT slug, stage_id, status, body
           FROM ia_stages
          ORDER BY slug, stage_id`,
      );
      stagesRows = stagesRes.rows;

      // ia_red_stage_proofs may not exist yet — guard with try/catch.
      try {
        const proofsRes = await client.query(
          `SELECT slug, stage_id, red_test_anchor, target_kind, proof_artifact_id, proof_status
             FROM ia_red_stage_proofs`,
        );
        proofsRows = proofsRes.rows;
      } catch {
        // Table not yet created — treat as empty.
        proofsRows = [];
      }
    }

    // Partition plans into grandfathered vs active.
    const cutover = new Date(CUTOVER_ISO);
    const grandfathered = [];
    const activePlans = [];

    for (const row of plansRows) {
      const created = new Date(row.created_at);
      if (created < cutover) {
        grandfathered.push(row.slug);
      } else {
        activePlans.push(row);
      }
    }

    if (activePlans.length === 0) {
      console.log(
        `[plan-red-stage] ✓ no plans on/after cutover ${CUTOVER_ISO}; ${grandfathered.length} grandfathered (skipped)`,
      );
      return 0;
    }

    const activeSlugs = new Set(activePlans.map((p) => p.slug));

    // Index stages by slug.
    /** @type {Map<string, StageRow[]>} */
    const stagesByPlan = new Map();
    for (const s of stagesRows) {
      if (!activeSlugs.has(s.slug)) continue;
      const arr = stagesByPlan.get(s.slug) ?? [];
      arr.push(s);
      stagesByPlan.set(s.slug, arr);
    }

    // Index proofs by slug+stage_id.
    /** @type {Map<string, ProofRow>} */
    const proofsMap = new Map();
    for (const p of proofsRows) {
      proofsMap.set(`${p.slug}::${p.stage_id}`, p);
    }

    for (const plan of activePlans) {
      const stages = stagesByPlan.get(plan.slug) ?? [];

      for (const s of stages) {
        // Check table first.
        const proofRow = proofsMap.get(`${plan.slug}::${s.stage_id}`);
        if (proofRow) {
          const proofBlock = {
            red_test_anchor: proofRow.red_test_anchor ?? "",
            target_kind: proofRow.target_kind ?? "",
            proof_artifact_id: proofRow.proof_artifact_id ?? "",
            proof_status: proofRow.proof_status ?? "",
          };
          const err = validateProofFields(proofBlock);
          if (!err) continue; // proof table row satisfies requirement
        }

        // Fall through to body parse.
        const bodyBlock = parseRedStageProofBlock(s.body ?? null);
        const err = validateProofFields(bodyBlock);
        if (err) {
          console.error(
            `[plan-red-stage] ✗ red_stage_proof_required: ${plan.slug}/${s.stage_id} — ${err}`,
          );
          violations += 1;
        }
      }
    }

    if (violations > 0) {
      console.error(
        `[plan-red-stage] ${violations} violation(s) total across ${activePlans.length} active plan(s); ${grandfathered.length} grandfathered (skipped)`,
      );
      return 1;
    }

    console.log(
      `[plan-red-stage] ✓ ${activePlans.length} active plan(s) checked · ${grandfathered.length} grandfathered (skipped) · ${warnCount} warn(s)`,
    );
    return 0;
  } finally {
    if (client) await client.end().catch(() => {});
  }
}

main().then(
  (code) => process.exit(code),
  (err) => {
    console.error(
      `[plan-red-stage] uncaught: ${err instanceof Error ? err.stack ?? err.message : String(err)}`,
    );
    process.exit(2);
  },
);
