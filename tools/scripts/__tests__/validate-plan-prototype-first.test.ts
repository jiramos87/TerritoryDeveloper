// TECH-10308 unit test for validate-plan-prototype-first.ts.
//
// Spawns `npx tsx` against the validator with `PLAN_PROTOTYPE_FIRST_FAKE_ROWS`
// env to bypass Postgres. Three cases exercise the three hard checks +
// grandfathered-skip path.

import assert from "node:assert";
import { spawnSync } from "node:child_process";
import * as path from "node:path";
import { test } from "node:test";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..", "..");
const SCRIPT = path.join(
  REPO_ROOT,
  "tools/scripts/validate-plan-prototype-first.ts",
);

interface FakePlan {
  slug: string;
  created_at: string;
}

interface FakeStage {
  slug: string;
  stage_id: string;
  status: string;
  tracer_slice_block: Record<string, string> | null;
  visibility_delta: string | null;
}

function runValidator(bundle: { plans: FakePlan[]; stages: FakeStage[] }) {
  return spawnSync("npx", ["tsx", SCRIPT], {
    cwd: REPO_ROOT,
    env: {
      ...process.env,
      PLAN_PROTOTYPE_FIRST_FAKE_ROWS: JSON.stringify(bundle),
    },
    encoding: "utf8",
  });
}

const VALID_TRACER = {
  name: "tracer",
  verb: "stub",
  surface: "noop",
  evidence: "log",
  gate: "manual",
};

test("tracer_slice_required — missing block on Stage 1.0 → exit 1", () => {
  const res = runValidator({
    plans: [{ slug: "fx-missing-tracer", created_at: "2026-06-01" }],
    stages: [
      {
        slug: "fx-missing-tracer",
        stage_id: "1.0",
        status: "pending",
        tracer_slice_block: null,
        visibility_delta: null,
      },
      {
        slug: "fx-missing-tracer",
        stage_id: "1.1",
        status: "pending",
        tracer_slice_block: VALID_TRACER,
        visibility_delta: null,
      },
    ],
  });
  assert.notStrictEqual(
    res.status,
    0,
    `expected nonzero exit; stderr=${res.stderr}; stdout=${res.stdout}`,
  );
  assert.match(res.stderr, /tracer_slice_required/);
  assert.match(res.stderr, /fx-missing-tracer\/1\.0/);
});

test("visibility_delta_unique — duplicate delta on Stage 2 + Stage 3 → exit 1", () => {
  const res = runValidator({
    plans: [{ slug: "fx-dup-delta", created_at: "2026-06-01" }],
    stages: [
      {
        slug: "fx-dup-delta",
        stage_id: "1.0",
        status: "pending",
        tracer_slice_block: VALID_TRACER,
        visibility_delta: null,
      },
      {
        slug: "fx-dup-delta",
        stage_id: "1.1",
        status: "pending",
        tracer_slice_block: VALID_TRACER,
        visibility_delta: null,
      },
      {
        slug: "fx-dup-delta",
        stage_id: "2.0",
        status: "pending",
        tracer_slice_block: null,
        visibility_delta: "same delta",
      },
      {
        slug: "fx-dup-delta",
        stage_id: "3.0",
        status: "pending",
        tracer_slice_block: null,
        visibility_delta: "same delta",
      },
    ],
  });
  assert.notStrictEqual(
    res.status,
    0,
    `expected nonzero exit; stderr=${res.stderr}; stdout=${res.stdout}`,
  );
  assert.match(res.stderr, /visibility_delta_unique/);
  assert.match(res.stderr, /same delta/);
});

test("grandfathered plan (created_at < cutover) → exit 0, skipped", () => {
  const res = runValidator({
    plans: [{ slug: "fx-old-plan", created_at: "2026-04-01" }],
    stages: [],
  });
  assert.strictEqual(
    res.status,
    0,
    `expected exit 0; stderr=${res.stderr}; stdout=${res.stdout}`,
  );
  assert.match(res.stdout, /grandfathered/);
});
