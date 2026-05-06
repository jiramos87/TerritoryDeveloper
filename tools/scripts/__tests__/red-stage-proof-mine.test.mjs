/**
 * red-stage-proof-mine.test.mjs
 *
 * unit-test:tools/scripts/__tests__/red-stage-proof-mine.test.mjs::feat_123_emits_candidate
 *
 * TECH-15910 — Red-Stage Proof: call `red_stage_proof_mine FEAT-123`
 * (or any known test class); assert candidate body length > 0 + matches test class shape.
 *
 * Relies on actual Assets/Scripts/Tests/** — if that tree changes, update the
 * expected class name accordingly.
 */

import { execFile } from "child_process";
import { promisify } from "util";
import { fileURLToPath } from "url";
import { join, resolve } from "path";
import assert from "node:assert/strict";

const execFileAsync = promisify(execFile);
const __dirname = fileURLToPath(new URL(".", import.meta.url));
const REPO_ROOT = resolve(__dirname, "../../..");
const SCRIPT = join(REPO_ROOT, "tools/scripts/red-stage-proof-mine.mjs");

async function run(issueId) {
  const { stdout, stderr } = await execFileAsync(process.execPath, [SCRIPT, issueId], {
    cwd: REPO_ROOT,
    timeout: 15000,
  });
  return { stdout: stdout.trim(), stderr: stderr.trim() };
}

// ---------------------------------------------------------------------------
// feat_123_emits_candidate
// ---------------------------------------------------------------------------
// Pass a known-pattern issue id (any non-existent id that happens to map to
// test files via slug scoring). Since the slug of "TECH-15909" = "tech15909"
// which won't match test class names, we use a query that WILL match because
// the mine script returns the best-scoring file even with score=0 if methods
// exist. We use "FEAT-123" with knowledge that the script falls back to
// returning the highest-method-count file.
//
// The key assertions are:
//   1. Exit code = 0
//   2. stdout length > 0
//   3. Output contains "### Red-Stage Proof"
//   4. Output contains a class name (```...``` block present)
// ---------------------------------------------------------------------------

const { stdout: out1 } = await run("FEAT-123");

assert(out1.length > 0, "candidate body length should be > 0");
assert(out1.includes("### Red-Stage Proof"), "output should contain Red-Stage Proof heading");
assert(out1.includes("```"), "output should contain a code block");
assert(out1.includes("Class:"), "output should reference a class name");

console.log("PASS feat_123_emits_candidate — length:", out1.length);

// ---------------------------------------------------------------------------
// empty_issue_id_exits_nonzero
// ---------------------------------------------------------------------------
// When called with no args, script should exit 1 (usage error) — stderr has message.
try {
  await execFileAsync(process.execPath, [SCRIPT], { cwd: REPO_ROOT, timeout: 5000 });
  assert.fail("should have thrown on missing arg");
} catch (e) {
  assert(e.code === 1 || e.code > 0, "exit code should be non-zero");
  console.log("PASS empty_issue_id_exits_nonzero");
}

console.log("All red-stage-proof-mine tests passed.");
