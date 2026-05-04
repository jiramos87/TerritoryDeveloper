/**
 * red_stage_proof_* — full test suite (Stage 2).
 *
 * anchor: tracer-verb-test:tools/mcp-ia-server/tests/tools/red-stage-proof.test.ts::TracerCapturesUnexpectedPass
 *
 * Covers:
 *   TECH-10892 — schema introspection (table, PK, FK, CHECK constraints)
 *   TECH-10893 — capture: happy paths + all 5 rejection paths
 *   TECH-10894 — get + list read-side tools
 *   TECH-10895 — finalize: happy paths + all 3 rejection paths
 *
 * Requires dev DB (skips gracefully when pool unavailable).
 */

import { before, after, describe, it } from "node:test";
import assert from "node:assert/strict";
import { randomUUID } from "node:crypto";
import { getIaDatabasePool } from "../../src/ia-db/pool.js";
import {
  handleRedStageProofCapture,
  handleRedStageProofGet,
  handleRedStageProofList,
  handleRedStageProofFinalize,
} from "../../src/red-stage-proof/index.js";

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

const pool = getIaDatabasePool();
const SKIP = pool === null ? { skip: "DB pool unavailable" } : {};

const SANDBOX_SLUG = `__test_rsp_${randomUUID().slice(0, 8)}__`;
const SANDBOX_STAGE = "t1";

function parseResult(toolResult: Awaited<ReturnType<typeof handleRedStageProofCapture>>) {
  const text = toolResult.content[0].text;
  return JSON.parse(text);
}

function makeAnchor(suffix: string) {
  return `tracer-verb-test:tools/mcp-ia-server/tests/tools/red-stage-proof.test.ts::${suffix}`;
}

async function seedRow(anchor: string, proof_status: string) {
  await pool!.query(
    `INSERT INTO ia_red_stage_proofs
       (slug, stage_id, target_kind, anchor, proof_artifact_id, proof_status)
     VALUES ($1, $2, 'tracer_verb', $3, $4, $5)`,
    [SANDBOX_SLUG, SANDBOX_STAGE, anchor, randomUUID(), proof_status],
  );
}

async function cleanupRows() {
  await pool!.query(`DELETE FROM ia_red_stage_proofs WHERE slug = $1`, [SANDBOX_SLUG]);
}

// ---------------------------------------------------------------------------
// Setup / teardown
// ---------------------------------------------------------------------------

describe("red_stage_proof_* — Stage 2", SKIP, () => {
  before(async () => {
    await pool!.query(
      `INSERT INTO ia_master_plans (slug, title)
       VALUES ($1, 'sandbox red stage proof test')
       ON CONFLICT (slug) DO NOTHING`,
      [SANDBOX_SLUG],
    );
    await pool!.query(
      `INSERT INTO ia_stages (slug, stage_id, title, status)
       VALUES ($1, $2, 'sandbox stage', 'in_progress')
       ON CONFLICT (slug, stage_id) DO NOTHING`,
      [SANDBOX_SLUG, SANDBOX_STAGE],
    );
  });

  after(async () => {
    await pool!.query(`DELETE FROM ia_red_stage_proofs WHERE slug = $1`, [SANDBOX_SLUG]);
    await pool!.query(`DELETE FROM ia_stages WHERE slug = $1`, [SANDBOX_SLUG]);
    await pool!.query(`DELETE FROM ia_master_plans WHERE slug = $1`, [SANDBOX_SLUG]);
    await pool!.end();
  });

  // -------------------------------------------------------------------------
  // TECH-10892 — Schema introspection
  // -------------------------------------------------------------------------

  describe("TECH-10892 schema", () => {
    it("ia_red_stage_proofs table exists", async () => {
      const r = await pool!.query(
        `SELECT table_name FROM information_schema.tables
         WHERE table_schema = 'public' AND table_name = 'ia_red_stage_proofs'`,
      );
      assert.equal(r.rowCount, 1);
    });

    it("all expected columns present", async () => {
      const r = await pool!.query(
        `SELECT column_name FROM information_schema.columns
         WHERE table_schema = 'public' AND table_name = 'ia_red_stage_proofs'
         ORDER BY column_name`,
      );
      const cols = r.rows.map((row: { column_name: string }) => row.column_name);
      for (const c of ["slug", "stage_id", "target_kind", "anchor", "proof_artifact_id", "proof_status", "green_status", "captured_at"]) {
        assert.ok(cols.includes(c), `missing column: ${c}`);
      }
    });

    it("PK is (slug, stage_id, anchor)", async () => {
      const r = await pool!.query(
        `SELECT kcu.column_name
         FROM information_schema.table_constraints tc
         JOIN information_schema.key_column_usage kcu
           ON tc.constraint_name = kcu.constraint_name
          AND tc.table_schema = kcu.table_schema
         WHERE tc.constraint_type = 'PRIMARY KEY'
           AND tc.table_name = 'ia_red_stage_proofs'
           AND tc.table_schema = 'public'
         ORDER BY kcu.ordinal_position`,
      );
      const pkCols = r.rows.map((row: { column_name: string }) => row.column_name);
      assert.deepEqual(pkCols, ["slug", "stage_id", "anchor"]);
    });

    it("FK to ia_stages has ON DELETE CASCADE", async () => {
      const r = await pool!.query(
        `SELECT rc.delete_rule
         FROM information_schema.referential_constraints rc
         JOIN information_schema.table_constraints tc
           ON rc.constraint_name = tc.constraint_name
          AND rc.constraint_schema = tc.table_schema
         WHERE tc.table_name = 'ia_red_stage_proofs'
           AND tc.table_schema = 'public'
           AND tc.constraint_type = 'FOREIGN KEY'`,
      );
      assert.ok(r.rowCount! > 0, "no FK found");
      const deleteRules = r.rows.map((row: { delete_rule: string }) => row.delete_rule);
      assert.ok(deleteRules.includes("CASCADE"), "FK missing CASCADE delete rule");
    });

    it("CHECK rejects bogus target_kind", async () => {
      await assert.rejects(
        () => pool!.query(
          `INSERT INTO ia_red_stage_proofs
             (slug, stage_id, target_kind, anchor, proof_artifact_id, proof_status)
           VALUES ($1, $2, 'bogus_kind', 'n/a', $3, 'pending')`,
          [SANDBOX_SLUG, SANDBOX_STAGE, randomUUID()],
        ),
        /check/i,
      );
    });

    it("CHECK rejects bogus proof_status", async () => {
      await assert.rejects(
        () => pool!.query(
          `INSERT INTO ia_red_stage_proofs
             (slug, stage_id, target_kind, anchor, proof_artifact_id, proof_status)
           VALUES ($1, $2, 'tracer_verb', 'n/a', $3, 'bogus_status')`,
          [SANDBOX_SLUG, SANDBOX_STAGE, randomUUID()],
        ),
        /check/i,
      );
    });

    it("CHECK rejects bogus green_status", async () => {
      await assert.rejects(
        () => pool!.query(
          `INSERT INTO ia_red_stage_proofs
             (slug, stage_id, target_kind, anchor, proof_artifact_id, proof_status, green_status)
           VALUES ($1, $2, 'tracer_verb', 'n/a', $3, 'pending', 'bogus_green')`,
          [SANDBOX_SLUG, SANDBOX_STAGE, randomUUID()],
        ),
        /check/i,
      );
    });
  });

  // -------------------------------------------------------------------------
  // TECH-10893 — capture
  // -------------------------------------------------------------------------

  describe("TECH-10893 capture", () => {
    after(cleanupRows);

    it("TracerCapturesUnexpectedPass — rejects unexpected_pass", async () => {
      const r = parseResult(await handleRedStageProofCapture({
        slug: SANDBOX_SLUG,
        stage_id: SANDBOX_STAGE,
        target_kind: "tracer_verb",
        anchor: makeAnchor("TracerCapturesUnexpectedPass"),
        proof_artifact_id: randomUUID(),
        command_kind: "npm-test",
        proof_status: "unexpected_pass",
      }));
      assert.equal(r.ok, false);
      assert.equal(r.error.code, "unexpected_pass_blocked");

      const check = await pool!.query(
        `SELECT anchor FROM ia_red_stage_proofs WHERE slug = $1 AND stage_id = $2 AND anchor = $3`,
        [SANDBOX_SLUG, SANDBOX_STAGE, makeAnchor("TracerCapturesUnexpectedPass")],
      );
      assert.equal(check.rowCount, 0);
    });

    it("CaptureFailedAsExpectedInserts", async () => {
      const r = parseResult(await handleRedStageProofCapture({
        slug: SANDBOX_SLUG,
        stage_id: SANDBOX_STAGE,
        target_kind: "tracer_verb",
        anchor: makeAnchor("CaptureFailedAsExpectedInserts"),
        proof_artifact_id: randomUUID(),
        command_kind: "npm-test",
        proof_status: "failed_as_expected",
      }));
      assert.equal(r.ok, true);
      assert.ok(r.payload.captured_at);
    });

    it("CapturePendingInserts", async () => {
      const r = parseResult(await handleRedStageProofCapture({
        slug: SANDBOX_SLUG,
        stage_id: SANDBOX_STAGE,
        target_kind: "tracer_verb",
        anchor: makeAnchor("CapturePendingInserts"),
        proof_artifact_id: randomUUID(),
        command_kind: "npm-test",
        proof_status: "pending",
      }));
      assert.equal(r.ok, true);
      assert.ok(r.payload.captured_at);
    });

    it("CaptureNotApplicableInserts", async () => {
      const r = parseResult(await handleRedStageProofCapture({
        slug: SANDBOX_SLUG,
        stage_id: SANDBOX_STAGE,
        target_kind: "design_only",
        anchor: "n/a",
        proof_artifact_id: randomUUID(),
        command_kind: "npm-test",
        proof_status: "not_applicable",
      }));
      assert.equal(r.ok, true);
      assert.ok(r.payload.captured_at);
    });

    it("CaptureRejectsBogusTargetKind — Zod schema rejects", async () => {
      await assert.rejects(
        () => handleRedStageProofCapture({
          slug: SANDBOX_SLUG,
          stage_id: SANDBOX_STAGE,
          target_kind: "bogus",
          anchor: "n/a",
          proof_artifact_id: randomUUID(),
          command_kind: "npm-test",
          proof_status: "pending",
        }),
        /invalid/i,
      );
    });

    it("CaptureRejectsBogusCommandKind — Zod schema rejects", async () => {
      await assert.rejects(
        () => handleRedStageProofCapture({
          slug: SANDBOX_SLUG,
          stage_id: SANDBOX_STAGE,
          target_kind: "tracer_verb",
          anchor: "n/a",
          proof_artifact_id: randomUUID(),
          command_kind: "bogus",
          proof_status: "pending",
        }),
        /invalid/i,
      );
    });

    it("CaptureRejectsBogusProofStatus — Zod schema rejects", async () => {
      await assert.rejects(
        () => handleRedStageProofCapture({
          slug: SANDBOX_SLUG,
          stage_id: SANDBOX_STAGE,
          target_kind: "tracer_verb",
          anchor: "n/a",
          proof_artifact_id: randomUUID(),
          command_kind: "npm-test",
          proof_status: "bogus",
        }),
        /invalid/i,
      );
    });

    it("CaptureRejectsMalformedAnchor", async () => {
      const r = parseResult(await handleRedStageProofCapture({
        slug: SANDBOX_SLUG,
        stage_id: SANDBOX_STAGE,
        target_kind: "tracer_verb",
        anchor: "this-is-not-valid-grammar",
        proof_artifact_id: randomUUID(),
        command_kind: "npm-test",
        proof_status: "pending",
      }));
      assert.equal(r.ok, false);
      assert.equal(r.error.code, "anchor_grammar_invalid");
    });

    it("CaptureRejectsDuplicateAnchor", async () => {
      const anchor = makeAnchor("DuplicateAnchor");
      await handleRedStageProofCapture({
        slug: SANDBOX_SLUG,
        stage_id: SANDBOX_STAGE,
        target_kind: "tracer_verb",
        anchor,
        proof_artifact_id: randomUUID(),
        command_kind: "npm-test",
        proof_status: "pending",
      });
      const r = parseResult(await handleRedStageProofCapture({
        slug: SANDBOX_SLUG,
        stage_id: SANDBOX_STAGE,
        target_kind: "tracer_verb",
        anchor,
        proof_artifact_id: randomUUID(),
        command_kind: "npm-test",
        proof_status: "pending",
      }));
      assert.equal(r.ok, false);
      assert.equal(r.error.code, "anchor_already_captured");
    });
  });

  // -------------------------------------------------------------------------
  // TECH-10894 — get + list
  // -------------------------------------------------------------------------

  describe("TECH-10894 get + list", () => {
    after(cleanupRows);

    it("GetReturnsProofsSortedByCapturedAt", async () => {
      const anchors = ["anchor-a", "anchor-b", "anchor-c"];
      for (let i = 0; i < anchors.length; i++) {
        await pool!.query(
          `INSERT INTO ia_red_stage_proofs
             (slug, stage_id, target_kind, anchor, proof_artifact_id, proof_status, captured_at)
           VALUES ($1, $2, 'tracer_verb', $3, $4, 'pending', NOW() + ($5 || ' seconds')::interval)`,
          [SANDBOX_SLUG, SANDBOX_STAGE, anchors[i], randomUUID(), String(i)],
        );
      }
      const r = parseResult(await handleRedStageProofGet({ slug: SANDBOX_SLUG, stage_id: SANDBOX_STAGE }));
      assert.equal(r.ok, true);
      const returnedAnchors = r.payload.proofs.map((p: { anchor: string }) => p.anchor);
      assert.deepEqual(returnedAnchors, anchors);
    });

    it("GetEmptyForUnknownStage", async () => {
      const r = parseResult(await handleRedStageProofGet({ slug: SANDBOX_SLUG, stage_id: "nonexistent-stage-xyz" }));
      assert.equal(r.ok, true);
      assert.deepEqual(r.payload.proofs, []);
    });

    it("GetReturnsAllRequiredColumns", async () => {
      await seedRow("anchor-for-columns", "pending");
      const r = parseResult(await handleRedStageProofGet({ slug: SANDBOX_SLUG, stage_id: SANDBOX_STAGE }));
      const row = r.payload.proofs[0];
      for (const col of ["slug", "stage_id", "target_kind", "anchor", "proof_artifact_id", "proof_status", "green_status", "captured_at"]) {
        assert.ok(col in row, `missing column: ${col}`);
      }
    });

    it("ListAggregatesByStageAndStatus", async () => {
      const stage2 = "t2";
      await pool!.query(
        `INSERT INTO ia_stages (slug, stage_id, title, status)
         VALUES ($1, $2, 'sandbox stage 2', 'in_progress')
         ON CONFLICT (slug, stage_id) DO NOTHING`,
        [SANDBOX_SLUG, stage2],
      );
      await seedRow("anchor-t1-pa", "pending");
      await seedRow("anchor-t1-pb", "pending");
      await pool!.query(
        `INSERT INTO ia_red_stage_proofs
           (slug, stage_id, target_kind, anchor, proof_artifact_id, proof_status)
         VALUES ($1, $2, 'tracer_verb', 'anchor-t2-fa', $3, 'failed_as_expected')`,
        [SANDBOX_SLUG, stage2, randomUUID()],
      );

      const r = parseResult(await handleRedStageProofList({ slug: SANDBOX_SLUG }));
      assert.equal(r.ok, true);
      const counts: Array<{ stage_id: string; proof_status: string; count: number }> = r.payload.counts;
      const t1pending = counts.find((c) => c.stage_id === SANDBOX_STAGE && c.proof_status === "pending");
      assert.ok(t1pending, "expected t1/pending aggregate row");
      assert.ok(t1pending.count >= 2, "expected count >= 2 for t1/pending");

      const t2fa = counts.find((c) => c.stage_id === stage2 && c.proof_status === "failed_as_expected");
      assert.ok(t2fa, "expected t2/failed_as_expected aggregate row");
      assert.equal(t2fa.count, 1);

      await pool!.query(`DELETE FROM ia_stages WHERE slug = $1 AND stage_id = $2`, [SANDBOX_SLUG, stage2]);
    });

    it("ListEmptyForUnknownSlug", async () => {
      const r = parseResult(await handleRedStageProofList({ slug: "__no_such_slug_xyz__" }));
      assert.equal(r.ok, true);
      assert.deepEqual(r.payload.counts, []);
    });

    it("ListSortOrderIsDeterministic", async () => {
      const r = parseResult(await handleRedStageProofList({ slug: SANDBOX_SLUG }));
      const counts: Array<{ stage_id: string; proof_status: string }> = r.payload.counts;
      const sorted = [...counts].sort((a, b) =>
        a.stage_id < b.stage_id ? -1 : a.stage_id > b.stage_id ? 1 :
        a.proof_status < b.proof_status ? -1 : 1,
      );
      assert.deepEqual(counts, sorted);
    });
  });

  // -------------------------------------------------------------------------
  // TECH-10895 — finalize
  // -------------------------------------------------------------------------

  describe("TECH-10895 finalize", () => {
    after(cleanupRows);

    it("FinalizePassedFlipsGreenStatus", async () => {
      const anchor = makeAnchor("FinalizePassedFlips");
      await seedRow(anchor, "failed_as_expected");
      const r = parseResult(await handleRedStageProofFinalize({
        slug: SANDBOX_SLUG, stage_id: SANDBOX_STAGE, anchor, green_status: "passed",
      }));
      assert.equal(r.ok, true);
      assert.equal(r.payload.green_status, "passed");
      assert.ok(r.payload.finalized_at);
    });

    it("FinalizeFailedFlipsGreenStatusForAnyPriorStatus", async () => {
      for (const proof_status of ["failed_as_expected", "pending", "not_applicable"]) {
        const anchor = makeAnchor(`FinalizeFailedAny_${proof_status}`);
        await seedRow(anchor, proof_status);
        const r = parseResult(await handleRedStageProofFinalize({
          slug: SANDBOX_SLUG, stage_id: SANDBOX_STAGE, anchor, green_status: "failed",
        }));
        assert.equal(r.ok, true, `expected ok for proof_status=${proof_status}`);
        assert.equal(r.payload.green_status, "failed");
      }
    });

    it("FinalizePassedBlockedAfterUnexpectedPass", async () => {
      const anchor = makeAnchor("FinalizePassedBlocked");
      await pool!.query(
        `INSERT INTO ia_red_stage_proofs
           (slug, stage_id, target_kind, anchor, proof_artifact_id, proof_status)
         VALUES ($1, $2, 'tracer_verb', $3, $4, 'unexpected_pass')`,
        [SANDBOX_SLUG, SANDBOX_STAGE, anchor, randomUUID()],
      );
      const r = parseResult(await handleRedStageProofFinalize({
        slug: SANDBOX_SLUG, stage_id: SANDBOX_STAGE, anchor, green_status: "passed",
      }));
      assert.equal(r.ok, false);
      assert.equal(r.error.code, "green_pass_blocked_unexpected_pass");

      const check = await pool!.query(
        `SELECT green_status FROM ia_red_stage_proofs WHERE slug = $1 AND stage_id = $2 AND anchor = $3`,
        [SANDBOX_SLUG, SANDBOX_STAGE, anchor],
      );
      assert.equal(check.rows[0].green_status, null);
    });

    it("FinalizeFailedAllowedAfterUnexpectedPass", async () => {
      const anchor = makeAnchor("FinalizeFailedAfterUnexpected");
      await pool!.query(
        `INSERT INTO ia_red_stage_proofs
           (slug, stage_id, target_kind, anchor, proof_artifact_id, proof_status)
         VALUES ($1, $2, 'tracer_verb', $3, $4, 'unexpected_pass')`,
        [SANDBOX_SLUG, SANDBOX_STAGE, anchor, randomUUID()],
      );
      const r = parseResult(await handleRedStageProofFinalize({
        slug: SANDBOX_SLUG, stage_id: SANDBOX_STAGE, anchor, green_status: "failed",
      }));
      assert.equal(r.ok, true);
      assert.equal(r.payload.green_status, "failed");
    });

    it("FinalizeRejectsBogusGreenStatus — Zod schema rejects", async () => {
      await assert.rejects(
        () => handleRedStageProofFinalize({
          slug: SANDBOX_SLUG,
          stage_id: SANDBOX_STAGE,
          anchor: "n/a",
          green_status: "bogus",
        }),
        /invalid/i,
      );
    });

    it("FinalizeNotFoundForMissingAnchor", async () => {
      const r = parseResult(await handleRedStageProofFinalize({
        slug: SANDBOX_SLUG, stage_id: SANDBOX_STAGE,
        anchor: "tracer-verb-test:no/such/path::NoSuchMethod",
        green_status: "passed",
      }));
      assert.equal(r.ok, false);
      assert.equal(r.error.code, "proof_not_found");
    });
  });
});
