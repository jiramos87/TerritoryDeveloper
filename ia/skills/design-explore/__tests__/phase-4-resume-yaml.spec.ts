/**
 * phase-4-resume-yaml.spec.ts — TECH-12647
 *
 * Red-stage proof: --resume mode scope predicate + target_version computation
 * + versioned filename rule.
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

// ---------------------------------------------------------------------------
// Resume scope predicate
// ---------------------------------------------------------------------------

type StageStatus = "backfilled" | "partial" | "present_complete";

function shouldReGrill(backfilled: boolean, band: StageStatus): boolean {
  // Re-grill ONLY if backfilled=true OR band=partial
  // Skip present_complete to avoid clobbering human content
  return backfilled || band === "partial";
}

describe("--resume scope predicate", () => {
  it("backfilled=true stage → re-grill", () => {
    expect(shouldReGrill(true, "present_complete")).toBe(true);
  });

  it("partial stage → re-grill", () => {
    expect(shouldReGrill(false, "partial")).toBe(true);
  });

  it("present_complete + not backfilled → skip", () => {
    expect(shouldReGrill(false, "present_complete")).toBe(false);
  });

  it("fixture: 1 backfilled + 1 partial + 1 present_complete → exactly 2 re-grilled", () => {
    const stages: Array<{ backfilled: boolean; band: StageStatus }> = [
      { backfilled: true, band: "present_complete" }, // re-grill (backfilled)
      { backfilled: false, band: "partial" }, // re-grill (partial)
      { backfilled: false, band: "present_complete" }, // skip
    ];
    const reGrilled = stages.filter((s) => shouldReGrill(s.backfilled, s.band));
    expect(reGrilled.length).toBe(2);
  });
});

// ---------------------------------------------------------------------------
// target_version computation
// ---------------------------------------------------------------------------

function computeTargetVersion(existingMaxVersion: number): number {
  return existingMaxVersion + 1;
}

describe("target_version computation", () => {
  it("existing_max_version=1 → target_version=2", () => {
    expect(computeTargetVersion(1)).toBe(2);
  });

  it("existing_max_version=2 → target_version=3", () => {
    expect(computeTargetVersion(2)).toBe(3);
  });

  it("resume on fixture lineage at version=2 → target_version=3", () => {
    const lineage = [{ version: 1 }, { version: 2 }];
    const maxVer = Math.max(...lineage.map((r) => r.version));
    expect(computeTargetVersion(maxVer)).toBe(3);
  });
});

// ---------------------------------------------------------------------------
// Versioned filename rule
// ---------------------------------------------------------------------------

function computeExplorationFilename(slug: string, targetVersion: number): string {
  if (targetVersion <= 1) return `${slug}.md`;
  return `${slug}-v${targetVersion}.md`;
}

describe("versioned filename rule", () => {
  it("target_version=1 → {slug}.md", () => {
    expect(computeExplorationFilename("my-plan", 1)).toBe("my-plan.md");
  });

  it("target_version=2 → {slug}-v2.md", () => {
    expect(computeExplorationFilename("my-plan", 2)).toBe("my-plan-v2.md");
  });

  it("--resume on slug with existing v=1 doc → new path is {slug}-v2.md", () => {
    const slug = "ship-protocol";
    const existingMaxVer = 1;
    const targetVer = computeTargetVersion(existingMaxVer);
    const filename = computeExplorationFilename(slug, targetVer);
    expect(filename).toBe("ship-protocol-v2.md");
  });
});

// ---------------------------------------------------------------------------
// SKILL.md + agent-body.md prose assertions
// ---------------------------------------------------------------------------

describe("Phase 4 SKILL.md prose — YAML emitter + resume", () => {
  it("SKILL.md Phase 4 mentions --resume mode", () => {
    expect(skillBody).toContain("--resume");
  });

  it("SKILL.md Phase 4 mentions lean YAML frontmatter", () => {
    expect(skillBody).toMatch(/YAML frontmatter|lean YAML/i);
  });

  it("SKILL.md Phase 4 lists required YAML keys", () => {
    expect(skillBody).toContain("slug");
    expect(skillBody).toContain("parent_plan_slug");
    expect(skillBody).toContain("target_version");
    expect(skillBody).toContain("stages:");
    expect(skillBody).toContain("tasks:");
  });

  it("SKILL.md Phase 4 references master_plan_lineage MCP tool", () => {
    expect(skillBody).toContain("master_plan_lineage");
  });

  it("SKILL.md Phase 4 mentions per-stage red-stage proof block", () => {
    expect(skillBody).toMatch(/red-stage proof.*stage|per.stage.*proof/i);
  });

  it("agent-body.md replicates --resume mode", () => {
    expect(agentBody).toContain("--resume");
  });

  it("agent-body.md replicates YAML frontmatter directive", () => {
    expect(agentBody).toMatch(/YAML frontmatter|lean YAML/i);
  });
});
