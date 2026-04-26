// TECH-1352 unit test for validate-capability-coverage.ts.
//
// Spawns `npx tsx` against the validator, pointed at fixture dirs under
// `tools/scripts/__fixtures__/capability-coverage/`. Stubs DB via env
// `CAPABILITY_COVERAGE_FAKE_IDS` so no Postgres connection is required.

import assert from "node:assert";
import { spawnSync } from "node:child_process";
import * as path from "node:path";
import { test } from "node:test";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..", "..");
const SCRIPT = path.join(REPO_ROOT, "tools/scripts/validate-capability-coverage.ts");
const FIXTURE_ROOT = path.join(
  REPO_ROOT,
  "tools/scripts/__fixtures__/capability-coverage",
);

function runValidator(fixtureSubdir: string, fakeIds: string) {
  const dir = path.join(FIXTURE_ROOT, fixtureSubdir);
  const res = spawnSync("npx", ["tsx", SCRIPT, dir], {
    cwd: REPO_ROOT,
    env: { ...process.env, CAPABILITY_COVERAGE_FAKE_IDS: fakeIds },
    encoding: "utf8",
  });
  return res;
}

test("green fixture exits 0", () => {
  const res = runValidator("green", "catalog.entity.create");
  assert.strictEqual(
    res.status,
    0,
    `expected exit 0; stderr=${res.stderr}; stdout=${res.stdout}`,
  );
  assert.match(res.stdout, /OK/);
});

test("missing-requires fixture exits non-zero with route path in stderr", () => {
  // Run a single-file fixture by pointing the validator at the parent
  // fixture root and filtering for missing-requires only — easier path is
  // a dedicated subdir, but the fixture file lives at the root level per
  // §Examples. We isolate via a per-test subdir.
  const res = runValidator("isolate-missing", "catalog.entity.create");
  assert.notStrictEqual(res.status, 0);
  assert.match(res.stderr, /missing routeMeta\.requires/);
  assert.match(res.stderr, /missing-requires\.ts/);
});

test("unknown-capability fixture exits non-zero with bad id in stderr", () => {
  const res = runValidator("isolate-unknown", "catalog.entity.create");
  assert.notStrictEqual(res.status, 0);
  assert.match(res.stderr, /unknown capability id/);
  assert.match(res.stderr, /does\.not\.exist/);
});
