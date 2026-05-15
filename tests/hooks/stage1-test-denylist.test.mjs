// Stage 1.0 sibling — test-write denylist branch of skill-surface-guard.sh — TDD red→green.
//
// Stage anchor (shared with stage1-stop-verification.test.mjs):
//   tracer-verb-test:tests/hooks/stage1-stop-verification.test.mjs::StopHookBlocksMissingVerification
//
// Task 1.0.6 — Hook smoke test — test-write denylist
//   - Deny Write/MultiEdit on tests/** without TD_ALLOW_TEST_EDIT={ISSUE_ID}
//   - Allow when TD_ALLOW_TEST_EDIT=BUG-1234 (or any non-empty issue id)
//   - Deny Edit that removes [Test] / it( / test( declaration tokens without override

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { existsSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, "../..");
const hookScript = join(repoRoot, "tools/scripts/claude-hooks/skill-surface-guard.sh");

function runHook(toolInput, env = {}) {
  const res = spawnSync("bash", [hookScript], {
    input: JSON.stringify(toolInput),
    encoding: "utf8",
    env: { ...process.env, ...env },
  });
  return { code: res.status, stdout: res.stdout, stderr: res.stderr };
}

describe("skill-surface-guard.sh — Stage 1.0 test-write denylist", () => {
  it("guardScriptExists [task 1.0.2]", () => {
    assert.ok(existsSync(hookScript), `guard script must exist: ${hookScript}`);
  });

  it("deniesWriteOnTestsWithoutOverride [task 1.0.6]", () => {
    const { code, stderr } = runHook({
      tool_name: "Write",
      tool_input: { file_path: "tests/foo.test.mjs", content: "..." },
    });
    assert.equal(code, 2, `expected exit 2 deny on tests/** Write; got ${code}`);
    assert.match(stderr ?? "", /TD_ALLOW_TEST_EDIT/);
  });

  it("allowsWriteOnTestsWhenOverridePresent [task 1.0.6]", () => {
    const { code } = runHook(
      {
        tool_name: "Write",
        tool_input: { file_path: "tests/foo.test.mjs", content: "..." },
      },
      { TD_ALLOW_TEST_EDIT: "BUG-1234" }
    );
    assert.equal(code, 0, `expected exit 0 with override; got ${code}`);
  });

  it("deniesEditRemovingItDeclarationWithoutOverride [task 1.0.6]", () => {
    const { code, stderr } = runHook({
      tool_name: "Edit",
      tool_input: {
        file_path: "tests/foo.test.mjs",
        old_string: "it('does the thing', () => {",
        new_string: "// removed",
      },
    });
    assert.equal(code, 2, `expected exit 2 on Edit removing it(; got ${code}`);
    assert.match(stderr ?? "", /test|it|Test/);
  });
});
