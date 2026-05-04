// TECH-10896 unit tests for validate-plan-red-stage.mjs.
//
// Spawns `node` against the validator with `PLAN_RED_STAGE_FAKE_ROWS` env
// to bypass Postgres. Covers all §Test Blueprint cases.

import assert from "node:assert";
import { spawnSync } from "node:child_process";
import * as path from "node:path";
import { test } from "node:test";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..", "..");
const SCRIPT = path.join(
  REPO_ROOT,
  "tools/scripts/validate-plan-red-stage.mjs",
);

/** Proof body block helper. */
function proofBody(fields = {}) {
  const {
    red_test_anchor = "tracer-verb-test:tools/lib/red-stage-anchor-resolver.ts::someTest",
    target_kind = "tracer_verb",
    proof_artifact_id = "tools/scripts/test/validate-plan-red-stage.test.mjs",
    proof_status = "failed_as_expected",
  } = fields;
  return `§Red-Stage Proof\nred_test_anchor: ${red_test_anchor}\ntarget_kind: ${target_kind}\nproof_artifact_id: ${proof_artifact_id}\nproof_status: ${proof_status}\n`;
}

function runValidator(bundle) {
  return spawnSync("node", [SCRIPT], {
    cwd: REPO_ROOT,
    env: {
      ...process.env,
      PLAN_RED_STAGE_FAKE_ROWS: JSON.stringify(bundle),
    },
    encoding: "utf8",
  });
}

// ---------------------------------------------------------------------------
// greenPlanExitsZero
// ---------------------------------------------------------------------------
test("greenPlanExitsZero — valid 4-field block → exit 0", () => {
  const res = runValidator({
    plans: [{ slug: "fx-green", created_at: "2026-06-01" }],
    stages: [
      {
        slug: "fx-green",
        stage_id: "1",
        status: "in_progress",
        body: proofBody(),
      },
    ],
    proofs: [],
  });
  assert.strictEqual(
    res.status,
    0,
    `expected 0; stderr=${res.stderr}; stdout=${res.stdout}`,
  );
  assert.match(res.stdout, /✓/);
});

// ---------------------------------------------------------------------------
// missingBlockExitsOne
// ---------------------------------------------------------------------------
test("missingBlockExitsOne — stage lacking §Red-Stage Proof → exit 1", () => {
  const res = runValidator({
    plans: [{ slug: "fx-missing-block", created_at: "2026-06-01" }],
    stages: [
      {
        slug: "fx-missing-block",
        stage_id: "2",
        status: "pending",
        body: "no proof block here",
      },
    ],
    proofs: [],
  });
  assert.strictEqual(
    res.status,
    1,
    `expected 1; stderr=${res.stderr}; stdout=${res.stdout}`,
  );
  assert.match(res.stderr, /red_stage_proof_required/);
  assert.match(res.stderr, /fx-missing-block\/2/);
});

// ---------------------------------------------------------------------------
// blankFieldExitsOne
// ---------------------------------------------------------------------------
test("blankFieldExitsOne — empty proof_artifact_id (non-design_only) → exit 1", () => {
  const res = runValidator({
    plans: [{ slug: "fx-blank-field", created_at: "2026-06-01" }],
    stages: [
      {
        slug: "fx-blank-field",
        stage_id: "3",
        status: "pending",
        body: proofBody({ proof_artifact_id: "" }),
      },
    ],
    proofs: [],
  });
  assert.strictEqual(
    res.status,
    1,
    `expected 1; stderr=${res.stderr}; stdout=${res.stdout}`,
  );
  assert.match(res.stderr, /proof_artifact_id/);
});

// ---------------------------------------------------------------------------
// designOnlySkipClause
// ---------------------------------------------------------------------------
test("designOnlySkipClause — target_kind=design_only + proof_artifact_id=n/a → exit 0", () => {
  const res = runValidator({
    plans: [{ slug: "fx-design-only", created_at: "2026-06-01" }],
    stages: [
      {
        slug: "fx-design-only",
        stage_id: "4",
        status: "pending",
        body: proofBody({
          target_kind: "design_only",
          proof_artifact_id: "n/a",
          proof_status: "not_applicable",
        }),
      },
    ],
    proofs: [],
  });
  assert.strictEqual(
    res.status,
    0,
    `expected 0; stderr=${res.stderr}; stdout=${res.stdout}`,
  );
});

// ---------------------------------------------------------------------------
// closedPlanSkipped — closed plans not in plansRows (validator queries WHERE status<>'closed')
// ---------------------------------------------------------------------------
test("closedPlanSkipped — plan absent from rows (closed) → exit 0", () => {
  // closed plan not included in fake rows (validator only receives non-closed plans)
  const res = runValidator({
    plans: [],
    stages: [],
    proofs: [],
  });
  assert.strictEqual(
    res.status,
    0,
    `expected 0; stderr=${res.stderr}; stdout=${res.stdout}`,
  );
});

// ---------------------------------------------------------------------------
// preCutoverGrandfathered
// ---------------------------------------------------------------------------
test("preCutoverGrandfathered — plan created before 2026-05-03 → exit 0", () => {
  const res = runValidator({
    plans: [{ slug: "fx-old", created_at: "2026-04-01" }],
    stages: [
      {
        slug: "fx-old",
        stage_id: "1",
        status: "pending",
        body: "no proof block",
      },
    ],
    proofs: [],
  });
  assert.strictEqual(
    res.status,
    0,
    `expected 0; stderr=${res.stderr}; stdout=${res.stdout}`,
  );
  assert.match(res.stdout, /grandfathered/);
});

// ---------------------------------------------------------------------------
// CIRedOnEmptyRedStageProofBlock — visibility-delta-test anchor
// Proves the CI gate fires. This is the anchor cited in TECH-10898 wire-in.
// ---------------------------------------------------------------------------
test("CIRedOnEmptyRedStageProofBlock — empty body → exit 1 (CI gate proof)", () => {
  const res = runValidator({
    plans: [{ slug: "fx-ci-red", created_at: "2026-06-01" }],
    stages: [
      {
        slug: "fx-ci-red",
        stage_id: "1",
        status: "in_progress",
        body: null,
      },
    ],
    proofs: [],
  });
  assert.strictEqual(
    res.status,
    1,
    `expected 1; stderr=${res.stderr}; stdout=${res.stdout}`,
  );
});
