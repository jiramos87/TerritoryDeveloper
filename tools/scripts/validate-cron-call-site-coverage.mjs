#!/usr/bin/env node
/**
 * validate-cron-call-site-coverage.mjs
 *
 * Asserts that the 5 legacy sync MCP write tools have been fully migrated
 * to their async cron_*_enqueue counterparts.
 *
 * Scans for **actual invocation patterns**:
 *
 * 1. Skill files (ia/skills/ ** /SKILL.md, agent-body.md, command-body.md):
 *    - `mcp__territory-ia__{forbidden_name}` — direct MCP tool invocation in
 *      skills tool lists or agent-body calls.
 *
 * 2. MCP server TS files (tools/mcp-ia-server/src/ ** /*.ts):
 *    - `registerTool("journal_append"` (or the other 4 names) — actual sync
 *      tool registration. After Task 6.0.4 these will be absent.
 *
 * Forbidden sync tool names:
 *   master_plan_change_log_append
 *   journal_append
 *   task_commit_record
 *   stage_verification_flip
 *   arch_changelog_append
 *
 * Exit 0 = 0 forbidden invocations. Exit 1 = hits found.
 *
 * TECH-18109 / async-cron-jobs Stage 6.0.3
 */

import path from "node:path";
import { fileURLToPath } from "node:url";
import fs from "node:fs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = process.env.IA_REPO_ROOT ?? path.resolve(__dirname, "../..");

const FORBIDDEN = [
  "master_plan_change_log_append",
  "journal_append",
  "task_commit_record",
  "stage_verification_flip",
  "arch_changelog_append",
];

// Pattern 1: mcp__territory-ia__{name} (skill invocation)
const SKILL_PATTERNS = FORBIDDEN.map(
  (name) => ({ pattern: new RegExp(`mcp__territory-ia__${name}`), name }),
);

// Pattern 2: registerTool("name" or registerTool('name' (actual sync registration)
const TS_PATTERNS = FORBIDDEN.map(
  (name) => ({ pattern: new RegExp(`registerTool\\s*\\(\\s*["']${name}["']`), name }),
);

function walkDir(dir, exts, out = []) {
  if (!fs.existsSync(dir)) return out;
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      if (["node_modules", "dist", ".git", "_retired"].includes(entry.name)) continue;
      walkDir(full, exts, out);
    } else if (entry.isFile() && exts.some((e) => full.endsWith(e))) {
      out.push(full);
    }
  }
  return out;
}

function scanFileForPatterns(filePath, patterns) {
  const content = fs.readFileSync(filePath, "utf8");
  const lines = content.split("\n");
  const hits = [];
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    for (const { pattern, name } of patterns) {
      if (pattern.test(line)) {
        hits.push({ file: filePath, line: i + 1, text: line.trim(), name });
      }
    }
  }
  return hits;
}

function main() {
  const skillFiles = walkDir(path.join(REPO_ROOT, "ia/skills"), [".md"]);
  const tsFiles = walkDir(path.join(REPO_ROOT, "tools/mcp-ia-server/src"), [".ts"]);

  const allHits = [];

  // Scan skill files for mcp__territory-ia__{forbidden} patterns
  for (const f of skillFiles) {
    const hits = scanFileForPatterns(f, SKILL_PATTERNS);
    allHits.push(...hits);
  }

  // Scan TS files for registerTool("forbidden") patterns
  for (const f of tsFiles) {
    const hits = scanFileForPatterns(f, TS_PATTERNS);
    allHits.push(...hits);
  }

  const totalFiles = skillFiles.length + tsFiles.length;

  if (allHits.length === 0) {
    console.log(
      `validate:cron-call-site-coverage: scanned ${totalFiles} file(s) — 0 forbidden sync tool invocations. ok.`,
    );
    process.exit(0);
  } else {
    console.error(
      `validate:cron-call-site-coverage: ${allHits.length} forbidden sync tool invocation(s) found.`,
    );
    for (const h of allHits) {
      const rel = path.relative(REPO_ROOT, h.file);
      console.error(`  ${rel}:${h.line}  [${h.name}]  ${h.text.slice(0, 120)}`);
    }
    console.error(
      "\nMigrate each call site to the cron_*_enqueue counterpart, then re-run.",
    );
    process.exit(1);
  }
}

main();
