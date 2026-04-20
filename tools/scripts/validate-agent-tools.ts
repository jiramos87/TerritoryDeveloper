#!/usr/bin/env tsx
/**
 * validate-agent-tools.ts (TECH-535 — Stage 1.3 B3 allowlist drift gate)
 *
 * CI lint enforcing explicit `tools:` frontmatter on the 7 non-pair-seam
 * subagents narrowed in TECH-534. Sibling of validate-agent-tools-uniformity.ts
 * (pair-seam role-baseline validator). Together they cover the full agent
 * surface: pair-seam = uniformity baseline; non-pair-seam = anti-wildcard gate.
 *
 * Targets (Stage 1.3 T1.3.1):
 *   verifier, spec-implementer, stage-decompose,
 *   project-new-planner, project-new-applier,
 *   design-explore, test-mode-loop
 *
 * Assertions per target agent:
 *   1. Frontmatter YAML block present (delimited by --- … ---).
 *   2. `tools:` line present (single-line CSV form).
 *   3. No wildcard entry (`*`, bare `Agent`-as-wildcard — flagged literally).
 *   4. Every declared tool either
 *      - built-in core (Read, Edit, Write, Bash, Grep, Glob, Agent,
 *        NotebookEdit, TodoWrite, MultiEdit), or
 *      - matches MCP namespace prefix `mcp__territory-ia__`.
 *
 * Exit 0 on all-pass; 1 on any violation.
 *
 * Usage:
 *   npx tsx tools/scripts/validate-agent-tools.ts
 *   npx tsx tools/scripts/validate-agent-tools.ts --agents-dir <dir>
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..");
const DEFAULT_AGENTS_DIR = path.join(REPO_ROOT, ".claude", "agents");

const NARROWED_AGENTS = [
  "verifier",
  "spec-implementer",
  "stage-decompose",
  "project-new-planner",
  "project-new-applier",
  "design-explore",
  "test-mode-loop",
];

const BUILTIN_TOOLS = new Set([
  "Read",
  "Edit",
  "Write",
  "Bash",
  "Grep",
  "Glob",
  "Agent",
  "NotebookEdit",
  "TodoWrite",
  "MultiEdit",
  "WebFetch",
  "WebSearch",
]);

const MCP_PREFIX = "mcp__territory-ia__";

function parseToolsFromFrontmatter(content: string): string[] | null {
  const match = content.match(/^---\n([\s\S]*?)\n---/);
  if (!match) return null;
  const frontmatter = match[1];
  const toolsMatch = frontmatter.match(/^tools:\s*(.+)$/m);
  if (!toolsMatch) return null;
  return toolsMatch[1]
    .split(",")
    .map((t) => t.trim())
    .filter(Boolean);
}

const args = process.argv.slice(2);
let agentsDir = DEFAULT_AGENTS_DIR;
for (let i = 0; i < args.length; i++) {
  if (args[i] === "--agents-dir" && args[i + 1]) {
    agentsDir = path.resolve(args[i + 1]);
    i++;
  }
}

let failCount = 0;
let passCount = 0;

for (const stem of NARROWED_AGENTS) {
  const file = path.join(agentsDir, `${stem}.md`);
  if (!fs.existsSync(file)) {
    console.error(`  FAIL  ${stem}: file not found (${file})`);
    failCount++;
    continue;
  }
  const content = fs.readFileSync(file, "utf-8");
  const tools = parseToolsFromFrontmatter(content);
  if (tools === null) {
    console.error(`  FAIL  ${stem}: no tools: frontmatter found`);
    failCount++;
    continue;
  }

  const issues: string[] = [];

  // Wildcard check
  if (tools.includes("*")) issues.push("wildcard `*` present");

  // Namespace check
  for (const t of tools) {
    if (BUILTIN_TOOLS.has(t)) continue;
    if (t.startsWith(MCP_PREFIX)) continue;
    issues.push(`unapproved tool: ${t}`);
  }

  if (issues.length === 0) {
    passCount++;
    console.log(`  PASS  ${stem}  (${tools.length} tools)`);
  } else {
    failCount++;
    console.error(`  FAIL  ${stem} — ${issues.join("; ")}`);
  }
}

console.log(
  `\nvalidate-agent-tools: ${NARROWED_AGENTS.length} narrowed agents — ${passCount} pass, ${failCount} fail`
);

process.exit(failCount > 0 ? 1 : 0);
