// Stage 6.0 — Wave E (multi-agent critic at /ship-final Pass B) — TDD red→green.
//
// Stage anchor: visibility-delta-test:tests/vibe-coding-safety/stage5-critics.test.mjs::CriticsParallelDispatchHighSeverityBlocks
//
// Tasks:
//   6.0.1  Migration — ia_review_findings table
//   6.0.2  Author /critic-style skill
//   6.0.3  Author /critic-logic skill
//   6.0.4  Author /critic-security skill
//   6.0.5  Register MCP tool review_findings_write
//   6.0.6  Update /ship-final Pass B to dispatch 3 critics in parallel
//   6.0.7  Stage test — critic parallel dispatch + high-severity block + override (this file)
//   6.0.8  Regenerate skill catalog + IA indexes

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../..");

describe("Stage 6.0 — multi-agent critic pipeline at /ship-final Pass B", () => {
  it("dispatchesThreeCriticsInParallelAndPersistsFindings [task 6.0.7]", () => {
    // Verify all 3 critic SKILL.md files exist
    const criticSlugs = ["critic-style", "critic-logic", "critic-security"];
    for (const slug of criticSlugs) {
      const skillPath = path.join(REPO_ROOT, "ia", "skills", slug, "SKILL.md");
      assert.ok(
        fs.existsSync(skillPath),
        `Critic skill missing: ${slug}/SKILL.md`
      );
      const body = fs.readFileSync(skillPath, "utf-8");
      assert.ok(
        body.includes("review_findings_write"),
        `${slug}/SKILL.md must reference review_findings_write MCP tool`
      );
    }

    // Verify ship-final agent-body.md dispatches all 3 critics in parallel
    const agentBody = fs.readFileSync(
      path.join(REPO_ROOT, "ia", "skills", "ship-final", "agent-body.md"),
      "utf-8"
    );
    assert.ok(
      agentBody.includes("/critic-style") &&
        agentBody.includes("/critic-logic") &&
        agentBody.includes("/critic-security"),
      "ship-final agent-body.md must reference all 3 critics"
    );
    assert.ok(
      agentBody.includes("parallel"),
      "ship-final agent-body.md must state parallel dispatch"
    );
    assert.ok(
      agentBody.includes("review_findings_write"),
      "ship-final agent-body.md must reference review_findings_write"
    );
  });

  it("blocksPlanCloseOnHighSeverityWithoutOverride [task 6.0.7]", () => {
    // Verify ship-final SKILL.md hard_boundaries block on severity=high
    const skillBody = fs.readFileSync(
      path.join(REPO_ROOT, "ia", "skills", "ship-final", "SKILL.md"),
      "utf-8"
    );
    assert.ok(
      skillBody.includes("critic_high_severity_block"),
      "ship-final SKILL.md must define critic_high_severity_block stop condition"
    );
    assert.ok(
      skillBody.includes("severity=high") || skillBody.includes("severity='high'"),
      "ship-final SKILL.md must reference severity=high block"
    );
    assert.ok(
      skillBody.includes("AskUserQuestion"),
      "ship-final SKILL.md must reference AskUserQuestion override prompt"
    );
  });

  it("logsArchChangelogEntryWhenOperatorOverrides [task 6.0.7]", () => {
    // Verify override path logs to arch_changelog
    const skillBody = fs.readFileSync(
      path.join(REPO_ROOT, "ia", "skills", "ship-final", "SKILL.md"),
      "utf-8"
    );
    assert.ok(
      skillBody.includes("critic_override"),
      "ship-final SKILL.md must reference arch_changelog kind=critic_override"
    );
    assert.ok(
      skillBody.includes("cron_arch_changelog_append_enqueue") ||
        skillBody.includes("arch_changelog"),
      "ship-final SKILL.md must log override to arch_changelog"
    );

    const agentBody = fs.readFileSync(
      path.join(REPO_ROOT, "ia", "skills", "ship-final", "agent-body.md"),
      "utf-8"
    );
    assert.ok(
      agentBody.includes("critic_override"),
      "ship-final agent-body.md must reference critic_override kind"
    );
  });

  it("reviewFindingsTableExistsWithCriticKindCheck [task 6.0.7]", () => {
    // Verify migration file exists with correct schema
    const migPath = path.join(
      REPO_ROOT,
      "db",
      "migrations",
      "0164_ia_review_findings.sql"
    );
    assert.ok(
      fs.existsSync(migPath),
      "Migration 0164_ia_review_findings.sql must exist"
    );
    const migBody = fs.readFileSync(migPath, "utf-8");
    assert.ok(
      migBody.includes("ia_review_findings"),
      "Migration must create ia_review_findings table"
    );
    assert.ok(
      migBody.includes("critic_kind") &&
        migBody.includes("style") &&
        migBody.includes("logic") &&
        migBody.includes("security"),
      "Migration must include critic_kind CHECK constraint with style|logic|security"
    );
    assert.ok(
      migBody.includes("severity") &&
        migBody.includes("low") &&
        migBody.includes("medium") &&
        migBody.includes("high"),
      "Migration must include severity CHECK constraint with low|medium|high"
    );

    // Verify MCP tool file exists
    const toolPath = path.join(
      REPO_ROOT,
      "tools",
      "mcp-ia-server",
      "src",
      "tools",
      "review-findings-write.ts"
    );
    assert.ok(
      fs.existsSync(toolPath),
      "MCP tool review-findings-write.ts must exist"
    );
    const toolBody = fs.readFileSync(toolPath, "utf-8");
    assert.ok(
      toolBody.includes("review_findings_write"),
      "MCP tool must register review_findings_write"
    );
    assert.ok(
      toolBody.includes("ia_review_findings"),
      "MCP tool must INSERT into ia_review_findings"
    );

    // Verify registration in server-registrations.ts
    const regPath = path.join(
      REPO_ROOT,
      "tools",
      "mcp-ia-server",
      "src",
      "server-registrations.ts"
    );
    const regBody = fs.readFileSync(regPath, "utf-8");
    assert.ok(
      regBody.includes("registerReviewFindingsWrite"),
      "server-registrations.ts must call registerReviewFindingsWrite"
    );
  });
});
