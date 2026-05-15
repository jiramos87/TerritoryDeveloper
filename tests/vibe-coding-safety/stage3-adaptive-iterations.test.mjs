// Stage 4.0 — Wave C (adaptive MAX_ITERATIONS by gap_reason in /verify-loop) — TDD red→green.
//
// Stage anchor: visibility-delta-test:tests/vibe-coding-safety/stage3-adaptive-iterations.test.mjs::TransientGapGets5Retries
//
// Tasks:
//   4.0.1  Add MAX_ITERATIONS_BY_GAP_REASON canonical table to verify-loop SKILL.md
//   4.0.2  Implement exponential-backoff helper
//   4.0.3  Replace fixed MAX_ITERATIONS=2 with classifier lookup
//   4.0.4  Update verify-loop validator / rule prose to new shape
//   4.0.5  Stage test — adaptive iterations (this file)
//   4.0.6  Regenerate skill catalog

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = join(__dirname, "..", "..");

// --- helpers ---

function readSkillMd() {
  return readFileSync(
    join(REPO_ROOT, "ia/skills/verify-loop/SKILL.md"),
    "utf-8"
  );
}

function readAgentBodyMd() {
  return readFileSync(
    join(REPO_ROOT, "ia/skills/verify-loop/agent-body.md"),
    "utf-8"
  );
}

// --- exponential-backoff helper ---

const { delayMs } = await import(
  join(REPO_ROOT, "tools/scripts/exponential-backoff.mjs")
);

describe("Stage 4.0 — adaptive iterations by gap_reason", () => {
  it("transientGapGets5RetriesWithGrowingBackoff [task 4.0.5]", () => {
    const skill = readSkillMd();
    // Table row: bridge_timeout → 5
    assert.match(
      skill,
      /bridge_timeout.*transient.*5/s,
      "SKILL.md must have bridge_timeout mapped to transient category with max_iterations=5"
    );
    // Table row: lease_unavailable → 5
    assert.match(
      skill,
      /lease_unavailable.*transient.*5/s,
      "SKILL.md must have lease_unavailable mapped to transient with max_iterations=5"
    );
    // Table row: unity_lock_stale → 5
    assert.match(
      skill,
      /unity_lock_stale.*transient.*5/s,
      "SKILL.md must have unity_lock_stale mapped to transient with max_iterations=5"
    );
    // Hard cap stated
    assert.match(
      skill,
      /[Hh]ard cap.*5/,
      "SKILL.md must state hard cap = 5"
    );
    // Backoff helper referenced
    assert.match(
      skill,
      /exponential-backoff/,
      "SKILL.md must reference exponential-backoff helper"
    );
  });

  it("deterministicGapGets2Retries [task 4.0.5]", () => {
    const skill = readSkillMd();
    // compile_error → deterministic → 2
    assert.match(
      skill,
      /compile_error.*deterministic.*2/s,
      "SKILL.md must have compile_error mapped to deterministic with max_iterations=2"
    );
    // test_assertion → deterministic → 2
    assert.match(
      skill,
      /test_assertion.*deterministic.*2/s,
      "SKILL.md must have test_assertion mapped to deterministic with max_iterations=2"
    );
    // validator_violation → deterministic → 2
    assert.match(
      skill,
      /validator_violation.*deterministic.*2/s,
      "SKILL.md must have validator_violation mapped to deterministic with max_iterations=2"
    );
  });

  it("escalateNowGapSkipsRetryAndPolls [task 4.0.5]", () => {
    const skill = readSkillMd();
    // unity_api_limit → escalate-now → 0
    assert.match(
      skill,
      /unity_api_limit.*escalate-now.*0/s,
      "SKILL.md must have unity_api_limit mapped to escalate-now with max_iterations=0"
    );
    // human_judgment_required → escalate-now → 0
    assert.match(
      skill,
      /human_judgment_required.*escalate-now.*0/s,
      "SKILL.md must have human_judgment_required mapped to escalate-now with max_iterations=0"
    );
    // immediate human poll phrasing
    assert.match(
      skill,
      /immediate human poll/i,
      "SKILL.md must describe immediate human poll for escalate-now"
    );
    // agent-body also covers escalate-now
    const agentBody = readAgentBodyMd();
    assert.match(
      agentBody,
      /escalate-now/,
      "agent-body.md must reference escalate-now category"
    );
  });

  it("backoffHelperReturnsExponentialDelay [task 4.0.5]", () => {
    // attempt 0 → 500, attempt 1 → 1000, attempt 2 → 2000, attempt 3 → 4000, attempt 4 → 8000
    assert.equal(delayMs(0), 500, "attempt 0 must return base=500");
    assert.equal(delayMs(1), 1000, "attempt 1 must return 1000");
    assert.equal(delayMs(2), 2000, "attempt 2 must return 2000");
    assert.equal(delayMs(3), 4000, "attempt 3 must return 4000");
    assert.equal(delayMs(4), 8000, "attempt 4 must return 8000 (max)");
    // cap at 8000
    assert.equal(delayMs(5), 8000, "attempt 5 must be capped at max_ms=8000");
    assert.equal(delayMs(10), 8000, "attempt 10 must still be capped at 8000");
    // backoff helper exists on disk
    assert.ok(
      existsSync(join(REPO_ROOT, "tools/scripts/exponential-backoff.mjs")),
      "tools/scripts/exponential-backoff.mjs must exist"
    );
  });
});
