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

describe("Stage 4.0 — adaptive iterations by gap_reason", () => {
  it("transientGapGets5RetriesWithGrowingBackoff [task 4.0.5]", () => {
    assert.fail("TODO 4.0.5 — pending MAX_ITERATIONS_BY_GAP_REASON classifier (task 4.0.3)");
  });

  it("deterministicGapGets2Retries [task 4.0.5]", () => {
    assert.fail("TODO 4.0.5 — pending classifier lookup (task 4.0.3)");
  });

  it("escalateNowGapSkipsRetryAndPolls [task 4.0.5]", () => {
    assert.fail("TODO 4.0.5 — pending escalate-now path (task 4.0.3)");
  });

  it("backoffHelperReturnsExponentialDelay [task 4.0.5]", () => {
    assert.fail("TODO 4.0.5 — pending exponential-backoff helper (task 4.0.2)");
  });
});
