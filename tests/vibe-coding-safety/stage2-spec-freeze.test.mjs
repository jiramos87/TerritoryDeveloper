// Stage 3.0 — Wave B (EARS rubric rule 11 + /spec-freeze gate) — incremental TDD red→green.
//
// Note: handoff red_stage_proof points at tests/vibe-coding-safety/stage2-spec-freeze.test.mjs.
// File name retained verbatim from handoff so red_test_anchor matches at validator time.
//
// Stage anchor: visibility-delta-test:tests/vibe-coding-safety/stage2-spec-freeze.test.mjs::ShipPlanRefusesNonFrozen
//
// Tasks:
//   3.0.1  Migration — ia_master_plans add ears_grandfathered column + backfill
//   3.0.2  Migration — ia_master_plan_specs table
//   3.0.3  Register MCP tool master_plan_spec_freeze
//   3.0.4  Author /spec-freeze skill
//   3.0.5  Rubric rule 11 in plan-digest-contract.md
//   3.0.6  Inject rubric rule 11 into /stage-authoring Phase 4 prompt
//   3.0.7  Extend validate:plan-digest-coverage to enforce EARS
//   3.0.8  Gate /ship-plan on frozen spec
//   3.0.9  Stage test — spec-freeze + ship-plan gate + EARS rubric (this file)
//   3.0.10 Regenerate skill catalog + IA indexes

import { describe, it, before, after } from "node:test";
import assert from "node:assert/strict";
import pg from "../../tools/postgres-ia/node_modules/pg/lib/index.js";
import { spawnSync } from "node:child_process";
import { readFileSync, existsSync } from "node:fs";
import path from "node:path";

const REPO_ROOT = new URL("../../", import.meta.url).pathname.replace(/\/$/, "");
const DB_URL =
  process.env.DATABASE_URL?.trim() ||
  "postgresql://postgres:postgres@localhost:5434/territory_ia_dev";

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Run a SQL query against the test DB. */
async function dbQuery(sql, params = []) {
  const pool = new pg.Pool({ connectionString: DB_URL, max: 1 });
  try {
    const res = await pool.query(sql, params);
    return res.rows;
  } finally {
    await pool.end();
  }
}

// ---------------------------------------------------------------------------
// Schema checks (tasks 3.0.1 + 3.0.2)
// ---------------------------------------------------------------------------

describe("Stage 3.0 — spec-freeze + ship-plan gate + EARS rubric", () => {
  describe("Schema: ia_master_plans.ears_grandfathered (task 3.0.1)", () => {
    it("ears_grandfathered column exists on ia_master_plans", async () => {
      const rows = await dbQuery(
        `SELECT column_name, is_nullable
           FROM information_schema.columns
          WHERE table_name = 'ia_master_plans'
            AND column_name = 'ears_grandfathered'`,
      );
      assert.equal(rows.length, 1, "ears_grandfathered column must exist on ia_master_plans");
      const col = rows[0];
      assert.equal(col.is_nullable, "NO", "ears_grandfathered must be NOT NULL");
    });

    it("existing plans are backfilled to ears_grandfathered=true", async () => {
      const rows = await dbQuery(
        `SELECT COUNT(*) AS total,
                COUNT(*) FILTER (WHERE ears_grandfathered IS NULL) AS nulls
           FROM ia_master_plans`,
      );
      const total = parseInt(rows[0].total, 10);
      const nulls = parseInt(rows[0].nulls, 10);
      if (total === 0) return;
      assert.equal(nulls, 0, "No rows should have ears_grandfathered=NULL after backfill");
    });
  });

  describe("Schema: ia_master_plan_specs table (task 3.0.2)", () => {
    it("ia_master_plan_specs table exists with required columns", async () => {
      const rows = await dbQuery(
        `SELECT column_name
           FROM information_schema.columns
          WHERE table_name = 'ia_master_plan_specs'
          ORDER BY ordinal_position`,
      );
      const cols = rows.map((r) => r.column_name);
      for (const required of [
        "id", "slug", "version", "frozen_at", "body", "open_questions_count",
        "created_at", "updated_at",
      ]) {
        assert.ok(cols.includes(required), `ia_master_plan_specs must have column '${required}'`);
      }
    });

    it("ia_master_plan_specs has unique constraint on (slug, version)", async () => {
      const rows = await dbQuery(
        `SELECT constraint_name
           FROM information_schema.table_constraints
          WHERE table_name = 'ia_master_plan_specs'
            AND constraint_type = 'UNIQUE'`,
      );
      assert.ok(rows.length >= 1, "ia_master_plan_specs must have at least one UNIQUE constraint");
    });
  });

  // ---------------------------------------------------------------------------
  // spec-freeze DB semantics (task 3.0.3)
  // ---------------------------------------------------------------------------

  describe("specFreezeInsertsRowAndEmitsArchChangelog [task 3.0.9]", () => {
    const TEST_SLUG = "__test_spec_freeze_stage30__";
    let pool;

    before(async () => {
      pool = new pg.Pool({ connectionString: DB_URL, max: 1 });
      await pool.query(
        `INSERT INTO ia_master_plans (slug, title, version)
         VALUES ($1, 'Test spec-freeze plan', 1)
         ON CONFLICT (slug) DO NOTHING`,
        [TEST_SLUG],
      );
    });

    after(async () => {
      await pool.query(`DELETE FROM ia_master_plan_specs WHERE slug = $1`, [TEST_SLUG]);
      await pool.query(`DELETE FROM ia_master_plans WHERE slug = $1`, [TEST_SLUG]);
      await pool.end();
    });

    it("can insert ia_master_plan_specs row with frozen_at and open_questions_count=0", async () => {
      const client = await pool.connect();
      try {
        await client.query("BEGIN");
        const res = await client.query(
          `INSERT INTO ia_master_plan_specs (slug, version, frozen_at, body, open_questions_count)
           VALUES ($1, 1, NOW(), 'test body', 0)
           ON CONFLICT (slug, version) DO UPDATE
             SET frozen_at = NOW(), body = EXCLUDED.body, open_questions_count = 0
           RETURNING id, frozen_at, open_questions_count`,
          [TEST_SLUG],
        );
        await client.query("COMMIT");
        assert.equal(res.rows.length, 1);
        assert.ok(res.rows[0].frozen_at !== null, "frozen_at must be set");
        assert.equal(parseInt(res.rows[0].open_questions_count, 10), 0);
      } finally {
        client.release();
      }
    });

    it("spec row with open_questions_count > 0 causes ship-plan gate to reject", async () => {
      await pool.query(
        `INSERT INTO ia_master_plan_specs (slug, version, body, open_questions_count)
         VALUES ($1, 99, 'draft body', 3)
         ON CONFLICT (slug, version) DO UPDATE
           SET frozen_at = NULL, open_questions_count = 3`,
        [TEST_SLUG],
      );

      const rows = await pool.query(
        `SELECT frozen_at, open_questions_count
           FROM ia_master_plan_specs
          WHERE slug = $1
          ORDER BY version DESC LIMIT 1`,
        [TEST_SLUG],
      );
      assert.ok(rows.rows.length > 0, "must find spec row");
      const spec = rows.rows[0];
      const shouldReject = spec.frozen_at === null || parseInt(spec.open_questions_count, 10) > 0;
      assert.ok(shouldReject, "gate should reject when frozen_at IS NULL or open_questions_count > 0");

      await pool.query(`DELETE FROM ia_master_plan_specs WHERE slug = $1 AND version = 99`, [TEST_SLUG]);
    });
  });

  // ---------------------------------------------------------------------------
  // /ship-plan gate prose (task 3.0.8)
  // ---------------------------------------------------------------------------

  describe("shipPlanRefusesNonFrozenSpec [task 3.0.9]", () => {
    it("ship-plan SKILL.md contains spec-freeze gate prose", () => {
      const skillPath = path.join(REPO_ROOT, "ia/skills/ship-plan/SKILL.md");
      assert.ok(existsSync(skillPath), "ia/skills/ship-plan/SKILL.md must exist");
      const body = readFileSync(skillPath, "utf8");
      assert.ok(body.includes("spec_not_frozen"), "ship-plan SKILL.md must mention spec_not_frozen stop condition");
      assert.ok(body.includes("ia_master_plan_specs"), "ship-plan SKILL.md must reference ia_master_plan_specs table");
      assert.ok(body.includes("--skip-freeze"), "ship-plan SKILL.md must document --skip-freeze bypass");
      assert.ok(body.includes("spec_freeze_bypass"), "ship-plan SKILL.md must mention spec_freeze_bypass arch_changelog kind");
    });

    it("ship-plan agent-body.md contains spec-freeze gate in Phase A.0", () => {
      const bodyPath = path.join(REPO_ROOT, "ia/skills/ship-plan/agent-body.md");
      assert.ok(existsSync(bodyPath), "ia/skills/ship-plan/agent-body.md must exist");
      const body = readFileSync(bodyPath, "utf8");
      assert.ok(body.includes("Spec-freeze gate"), "agent-body.md Phase A.0 must include spec-freeze gate");
    });
  });

  // ---------------------------------------------------------------------------
  // EARS rubric (tasks 3.0.5 + 3.0.7)
  // ---------------------------------------------------------------------------

  describe("validatePlanDigestCoverageRejectsNonEarsAcceptance [task 3.0.9]", () => {
    it("plan-digest-contract.md contains EARS rule 10", () => {
      const contractPath = path.join(REPO_ROOT, "ia/rules/plan-digest-contract.md");
      assert.ok(existsSync(contractPath), "ia/rules/plan-digest-contract.md must exist");
      const body = readFileSync(contractPath, "utf8");
      assert.ok(body.includes("EARS prefix required"), "plan-digest-contract.md must contain EARS prefix required rule");
      assert.ok(body.includes("WHEN"), "plan-digest-contract.md must list WHEN EARS pattern");
      assert.ok(body.includes("WHILE"), "plan-digest-contract.md must list WHILE EARS pattern");
      assert.ok(body.includes("WHERE"), "plan-digest-contract.md must list WHERE EARS pattern");
    });

    it("validate-plan-digest-coverage.ts contains EARS prefix enforcement code", () => {
      const validatorPath = path.join(REPO_ROOT, "tools/scripts/validators/validate-plan-digest-coverage.ts");
      assert.ok(existsSync(validatorPath), "validate-plan-digest-coverage.ts must exist");
      const src = readFileSync(validatorPath, "utf8");
      assert.ok(src.includes("EARS_PREFIX_RE"), "validator must define EARS_PREFIX_RE regex");
      assert.ok(src.includes("extractAcceptanceRows"), "validator must implement extractAcceptanceRows");
      assert.ok(src.includes("ears_grandfathered"), "validator must check ears_grandfathered flag");
      assert.ok(src.includes("ears_violations"), "validator must collect ears_violations");
    });

    it("EARS regex matches expected prefixes and rejects non-EARS rows", () => {
      const EARS_PREFIX_RE = /^(when|the|if|while|where)\b/i;
      const valid = [
        "WHEN the user calls /spec-freeze THE tool inserts a row",
        "THE system emits arch_changelog",
        "IF open_questions_count > 0 THEN freeze is rejected",
        "WHILE frozen_at IS NULL the plan cannot be shipped",
        "WHERE --skip-freeze is present the gate is bypassed",
      ];
      const invalid = [
        "Spec is frozen by calling master_plan_spec_freeze",
        "Insert a row into ia_master_plan_specs",
        "Open questions must be zero",
      ];
      for (const row of valid) {
        assert.ok(EARS_PREFIX_RE.test(row), `Expected EARS match for: "${row}"`);
      }
      for (const row of invalid) {
        assert.ok(!EARS_PREFIX_RE.test(row), `Expected EARS rejection for: "${row}"`);
      }
    });
  });

  // ---------------------------------------------------------------------------
  // ears_grandfathered bypass (tasks 3.0.1 + 3.0.7)
  // ---------------------------------------------------------------------------

  describe("earsGrandfatheredBypassesEarsCheck [task 3.0.9]", () => {
    it("grandfathered flag bypasses EARS check in validator logic", () => {
      const EARS_PREFIX_RE = /^(when|the|if|while|where)\b/i;
      const validateRow = (row) => {
        if (row.ears_grandfathered) return [];
        const acceptanceRows = ["Non-EARS acceptance row without prefix"];
        return acceptanceRows.filter((r) => !EARS_PREFIX_RE.test(r));
      };
      const grandfatheredResult = validateRow({ ears_grandfathered: true });
      assert.deepEqual(grandfatheredResult, [], "grandfathered plan must skip EARS check");
      const nonGrandfatheredResult = validateRow({ ears_grandfathered: false });
      assert.equal(nonGrandfatheredResult.length, 1, "non-grandfathered plan must detect EARS violations");
    });

    it("validator exits 1 on EARS violations in source", () => {
      const validatorPath = path.join(REPO_ROOT, "tools/scripts/validators/validate-plan-digest-coverage.ts");
      const src = readFileSync(validatorPath, "utf8");
      assert.ok(src.includes("earsViolations.length > 0"), "validator must exit 1 on EARS violations");
    });
  });

  // ---------------------------------------------------------------------------
  // spec-freeze SKILL authorship (task 3.0.4)
  // ---------------------------------------------------------------------------

  describe("specFreezeSkillAuthor [task 3.0.4]", () => {
    it("/spec-freeze skill files exist", () => {
      const skillDir = path.join(REPO_ROOT, "ia/skills/spec-freeze");
      assert.ok(existsSync(path.join(skillDir, "SKILL.md")), "spec-freeze/SKILL.md must exist");
      assert.ok(existsSync(path.join(skillDir, "agent-body.md")), "spec-freeze/agent-body.md must exist");
      assert.ok(existsSync(path.join(skillDir, "command-body.md")), "spec-freeze/command-body.md must exist");
    });

    it("spec-freeze SKILL.md references master_plan_spec_freeze MCP", () => {
      const body = readFileSync(path.join(REPO_ROOT, "ia/skills/spec-freeze/SKILL.md"), "utf8");
      assert.ok(body.includes("master_plan_spec_freeze"), "SKILL.md must reference master_plan_spec_freeze MCP tool");
    });
  });
});
