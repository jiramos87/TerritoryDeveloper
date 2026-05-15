// Stage 1.0 — Tracer slice: hook-layer tracer (stop hook + test-write denylist) — incremental TDD red→green test file.
//
// Protocol (ia/rules/agent-principles.md §Testing + verification):
//   ONE test file per Stage. First task creates file in failing/red state.
//   Each subsequent task extends with new assertions tied to its phase.
//   File stays red until last task of the Stage. Stage close = file green.
//
// Stage anchor: tracer-verb-test:tests/hooks/stage1-stop-verification.test.mjs::StopHookBlocksMissingVerification
//
// Tasks:
//   1.0.1  Write stop-verification-required.sh hook script
//   1.0.2  Extend skill-surface-guard.sh with tests/scenarios denylist branch
//   1.0.3  Wire hooks.Stop[] matcher in .claude/settings.json
//   1.0.4  Wire hooks.PreToolUse[] chain in .claude/settings.json
//   1.0.5  Hook smoke test — stop verification required (this file)
//   1.0.6  Hook smoke test — test-write denylist (sibling stage1-test-denylist.test.mjs)

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, "../..");
const hookScript = join(repoRoot, "tools/scripts/claude-hooks/stop-verification-required.sh");

function runHook(ctxJson, env = {}) {
  const res = spawnSync("bash", [hookScript], {
    input: ctxJson,
    encoding: "utf8",
    env: { ...process.env, ...env },
  });
  return { code: res.status, stdout: res.stdout, stderr: res.stderr };
}

describe("stop-verification-required.sh — Stage 1.0 hook tracer", () => {
  it("hookScriptExists [task 1.0.1]", () => {
    assert.ok(existsSync(hookScript), `hook script must exist: ${hookScript}`);
  });

  it("exits2WhenAssetsTouchedWithoutVerificationBlock [task 1.0.5]", () => {
    const ctx = JSON.stringify({
      touched_files: ["Assets/Scripts/Foo.cs"],
      response_text: "ok done",
    });
    const { code, stderr } = runHook(ctx);
    assert.equal(code, 2, `expected exit 2 on missing Verification block; got ${code}`);
    assert.match(stderr ?? "", /Verification block required/i);
  });

  it("exits0OnDocsOnlySession [task 1.0.5]", () => {
    const ctx = JSON.stringify({
      touched_files: ["docs/foo.md"],
      response_text: "ok",
    });
    const { code } = runHook(ctx);
    assert.equal(code, 0, `expected exit 0 on docs-only session; got ${code}`);
  });

  it("exits0WhenVerificationBlockPresent [task 1.0.5]", () => {
    const ctx = JSON.stringify({
      touched_files: ["Assets/Scripts/Foo.cs"],
      response_text: '```json\n{"verification": {"path": "A", "rows": []}}\n```',
    });
    const { code } = runHook(ctx);
    assert.equal(code, 0, `expected exit 0 when Verification block present; got ${code}`);
  });

  it("settingsJsonWiresStopHook [task 1.0.3]", () => {
    const settingsPath = join(repoRoot, ".claude/settings.json");
    assert.ok(existsSync(settingsPath), `.claude/settings.json must exist`);
    const settings = JSON.parse(readFileSync(settingsPath, "utf8"));
    const stopEntries = settings?.hooks?.Stop ?? [];
    const found = JSON.stringify(stopEntries).includes("stop-verification-required.sh");
    assert.ok(found, "hooks.Stop[] must reference stop-verification-required.sh");
  });
});
