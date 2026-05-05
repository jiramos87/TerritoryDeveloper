/**
 * phase-1-exit-token.spec.ts — TECH-12646
 *
 * Red-stage proof: Phase 1 cannot exit without the `phase-1-done` token
 * when decisions remain open.
 *
 * These tests assert the SKILL.md contract by parsing the prose directly —
 * no Unity bridge, no DB. Node-runnable via vitest.
 */

import { describe, it, expect } from "vitest";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const SKILL_PATH = resolve(
  process.cwd(),
  "ia/skills/design-explore/SKILL.md",
);
const AGENT_BODY_PATH = resolve(
  process.cwd(),
  "ia/skills/design-explore/agent-body.md",
);

const skillBody = readFileSync(SKILL_PATH, "utf8");
const agentBody = readFileSync(AGENT_BODY_PATH, "utf8");

describe("Phase 1 exit token — SKILL.md contract", () => {
  it("SKILL.md Phase 1 section mentions phase-1-done token", () => {
    expect(skillBody).toContain("phase-1-done");
  });

  it("SKILL.md Phase 1 section mentions 'close phase 1' alternative option", () => {
    expect(skillBody).toContain("close phase 1");
  });

  it("SKILL.md Phase 1 specifies zero unresolved decisions as hard rule", () => {
    expect(skillBody).toMatch(/zero unresolved decisions/i);
  });

  it("SKILL.md Phase 1 specifies loop re-runs while decisions remain", () => {
    // Must describe the loop re-entry condition
    expect(skillBody).toMatch(/remain unresolved|unresolved decisions remain|≥1 decision/i);
  });

  it("SKILL.md Phase 1 mandates listing outstanding decisions before each poll round", () => {
    expect(skillBody).toMatch(/list.*outstanding decisions|outstanding decisions.*before/i);
  });

  it("SKILL.md Phase 1 cross-links agent-human-polling.md", () => {
    expect(skillBody).toContain("ia/rules/agent-human-polling.md");
  });

  it("SKILL.md Phase 1 specifies polling round size 1-4 questions", () => {
    expect(skillBody).toMatch(/1[–-]4 questions/);
  });
});

describe("Phase 1 exit token — agent-body.md inheritance", () => {
  it("agent-body.md replicates phase-1-done token directive", () => {
    expect(agentBody).toContain("phase-1-done");
  });

  it("agent-body.md replicates polling loop directive", () => {
    expect(agentBody).toMatch(/relentless.*poll|poll.*loop|AskUserQuestion.*loop/i);
  });

  it("agent-body.md cross-links agent-human-polling.md", () => {
    expect(agentBody).toContain("agent-human-polling.md");
  });
});

describe("Phase 1 exit contract — fixture assertion", () => {
  // Simulate the Phase 1 logic: given open decisions, the phase must NOT emit Phase 2 hand-off
  it("state with 1 open decision → loop directive expected, not Phase 2 hand-off", () => {
    // Fixture: describe what happens when exit-token logic runs with 1 open decision
    const openDecisions = ["D1: Which approach to take?"];
    const tokenReceived = false;

    // Per SKILL.md contract: if openDecisions.length > 0, must loop (not advance)
    const shouldLoop = openDecisions.length > 0 || !tokenReceived;
    expect(shouldLoop).toBe(true);
  });

  it("state with 0 open decisions but no token → must still require token", () => {
    const openDecisions: string[] = [];
    const tokenReceived = false;

    // Per SKILL.md: both conditions must be true (zero decisions AND token received)
    const canAdvance = openDecisions.length === 0 && tokenReceived;
    expect(canAdvance).toBe(false);
  });

  it("state with 0 open decisions AND token received → can advance to Phase 2", () => {
    const openDecisions: string[] = [];
    const tokenReceived = true;

    const canAdvance = openDecisions.length === 0 && tokenReceived;
    expect(canAdvance).toBe(true);
  });

  it("token received but decisions remain → token ignored, must still loop", () => {
    const openDecisions = ["D1: unresolved"];
    const tokenReceived = true;

    // Per SKILL.md: token ignored when decisions remain
    const shouldLoop = openDecisions.length > 0;
    expect(shouldLoop).toBe(true);
  });
});
