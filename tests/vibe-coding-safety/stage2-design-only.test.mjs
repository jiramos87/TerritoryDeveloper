// Stage 2.0 — Wave A finalization (docs cross-links + rule prose) — design_only stage.
//
// Stage anchor: design-only-test:ia/rules/agent-principles.md::HookCrossRef
//
// proof_status = not_applicable per handoff red_stage_proof_block. This file only asserts the
// cross-ref prose lands in the right specs/rules. No bridge/runtime surface.
//
// Tasks:
//   2.0.1  Cross-ref hooks in agent-principles.md Testing+verification section
//   2.0.2  Cross-ref Stop hook in agent-led-verification-policy.md

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, "../..");

describe("Stage 2.0 — doc cross-ref to hook scripts", () => {
  it("agentPrinciplesReferencesStopHook [task 2.0.1]", () => {
    const p = join(repoRoot, "ia/rules/agent-principles.md");
    assert.ok(existsSync(p), `rule file must exist: ${p}`);
    const body = readFileSync(p, "utf8");
    assert.match(body, /stop-verification-required\.sh/, "must cross-ref stop hook");
    assert.match(body, /skill-surface-guard\.sh/, "must cross-ref guard hook");
  });

  it("verificationPolicyNamesStopHook [task 2.0.2]", () => {
    const p = join(repoRoot, "docs/agent-led-verification-policy.md");
    assert.ok(existsSync(p), `doc must exist: ${p}`);
    const body = readFileSync(p, "utf8");
    assert.match(body, /Stop hook/i, "must name Stop hook as enforcement layer");
    assert.match(body, /stop-verification-required\.sh/, "must link hook script");
  });
});
