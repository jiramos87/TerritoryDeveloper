#!/usr/bin/env tsx
/**
 * validate-agent-tools-uniformity.ts
 *
 * CI uniformity-gate validator for pair-seam agent tools: frontmatter.
 * Groups pair-seam agents by role (head / tail); asserts each agent in a role group
 * contains at least the canonical baseline tools for that role.
 *
 * Pair-seam agents (post-refactor — retired heads/tails dropped 2026-04-25):
 *   Pair-heads (Opus):  opus-code-reviewer, project-new-planner
 *   Pair-tails (Sonnet): plan-applier, project-new-applier
 *
 * Retired (no longer present in `.claude/agents/`, tombstones in `_retired/`):
 *   plan-reviewer, stage-file-planner, stage-closeout-planner, stage-file-applier
 *
 * Canonical baseline (MUST be present in every agent of the role group):
 *   head: Read, Edit, Write, Bash, Grep, Glob,
 *         mcp__territory-ia__router_for_task, mcp__territory-ia__glossary_discover,
 *         mcp__territory-ia__glossary_lookup, mcp__territory-ia__invariants_summary,
 *         mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections,
 *         mcp__territory-ia__backlog_issue, mcp__territory-ia__master_plan_locate,
 *         mcp__territory-ia__list_rules, mcp__territory-ia__rule_content
 *   tail: Read, Edit, Write, Bash, Grep, Glob,
 *         mcp__territory-ia__backlog_issue, mcp__territory-ia__master_plan_locate
 *
 * Agents may carry additional seam-specific tools beyond the baseline (not flagged).
 * Exit: 0 all-pass; 1 any baseline tool missing from any agent.
 *
 * Usage:
 *   npx tsx tools/scripts/validate-agent-tools-uniformity.ts
 *   npx tsx tools/scripts/validate-agent-tools-uniformity.ts --agents-dir <dir>
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

// ---------------------------------------------------------------------------
// Config
// ---------------------------------------------------------------------------

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..");
const DEFAULT_AGENTS_DIR = path.join(REPO_ROOT, ".claude", "agents");

const HEAD_AGENTS = new Set([
  "opus-code-reviewer",
  "project-new-planner",
]);

const TAIL_AGENTS = new Set([
  "plan-applier",
  "project-new-applier",
]);

const HEAD_BASELINE = [
  "Read",
  "Edit",
  "Write",
  "Bash",
  "Grep",
  "Glob",
  "mcp__territory-ia__router_for_task",
  "mcp__territory-ia__glossary_discover",
  "mcp__territory-ia__glossary_lookup",
  "mcp__territory-ia__invariants_summary",
  "mcp__territory-ia__spec_section",
  "mcp__territory-ia__spec_sections",
  "mcp__territory-ia__backlog_issue",
  "mcp__territory-ia__master_plan_locate",
  "mcp__territory-ia__list_rules",
  "mcp__territory-ia__rule_content",
];

const TAIL_BASELINE = [
  "Read",
  "Edit",
  "Write",
  "Bash",
  "Grep",
  "Glob",
  "mcp__territory-ia__backlog_issue",
  "mcp__territory-ia__master_plan_locate",
];

// ---------------------------------------------------------------------------
// CLI
// ---------------------------------------------------------------------------

const args = process.argv.slice(2);
let agentsDir = DEFAULT_AGENTS_DIR;
for (let i = 0; i < args.length; i++) {
  if (args[i] === "--agents-dir" && args[i + 1]) {
    agentsDir = path.resolve(args[i + 1]);
    i++;
  }
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function parseToolsFromFrontmatter(content: string): string[] | null {
  // Extract YAML frontmatter block between first --- delimiters
  const match = content.match(/^---\n([\s\S]*?)\n---/);
  if (!match) return null;
  const frontmatter = match[1];
  // Find tools: line (single-line CSV format)
  const toolsMatch = frontmatter.match(/^tools:\s*(.+)$/m);
  if (!toolsMatch) return null;
  return toolsMatch[1]
    .split(",")
    .map((t) => t.trim())
    .filter(Boolean);
}

function agentStem(filename: string): string {
  return path.basename(filename, ".md");
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

interface AgentResult {
  stem: string;
  file: string;
  role: "head" | "tail";
  tools: string[];
  missing: string[];
}

const results: AgentResult[] = [];
let anyFail = false;

// Read all .md files in agents dir (skip _retired/)
const files = fs
  .readdirSync(agentsDir)
  .filter((f) => f.endsWith(".md"))
  .map((f) => path.join(agentsDir, f));

for (const file of files) {
  const stem = agentStem(file);
  const role: "head" | "tail" | null = HEAD_AGENTS.has(stem)
    ? "head"
    : TAIL_AGENTS.has(stem)
      ? "tail"
      : null;
  if (!role) continue; // not a pair-seam agent — skip

  const content = fs.readFileSync(file, "utf-8");
  const tools = parseToolsFromFrontmatter(content);
  if (tools === null) {
    console.error(`  FAIL  ${stem}: no tools: frontmatter found`);
    anyFail = true;
    continue;
  }

  const toolSet = new Set(tools);
  const baseline = role === "head" ? HEAD_BASELINE : TAIL_BASELINE;
  const missing = baseline.filter((t) => !toolSet.has(t));

  results.push({ stem, file, role, tools, missing });
  if (missing.length > 0) anyFail = true;
}

// Sort: heads first, then tails; alphabetical within group
results.sort((a, b) => {
  if (a.role !== b.role) return a.role === "head" ? -1 : 1;
  return a.stem.localeCompare(b.stem);
});

// Report
let headCount = 0;
let tailCount = 0;
let passCount = 0;
let failCount = 0;

for (const r of results) {
  if (r.role === "head") headCount++;
  else tailCount++;

  if (r.missing.length === 0) {
    passCount++;
    console.log(`  PASS  ${r.stem}  (${r.role}, ${r.tools.length} tools)`);
  } else {
    failCount++;
    console.error(
      `  FAIL  ${r.stem}  (${r.role}) — missing baseline tools: ${r.missing.join(", ")}`
    );
  }
}

console.log(
  `\nvalidate-agent-tools-uniformity: ${headCount} heads, ${tailCount} tails checked — ${passCount} pass, ${failCount} fail`
);

process.exit(anyFail ? 1 : 0);
