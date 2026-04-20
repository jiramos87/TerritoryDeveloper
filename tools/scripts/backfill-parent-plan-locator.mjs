#!/usr/bin/env node
/**
 * backfill-parent-plan-locator.mjs
 *
 * Idempotent one-shot driver: resolves parent_plan + task_key for every open
 * backlog yaml that does not yet have both fields populated.
 *
 * Resolution strategy:
 *   1. Build plan index by scanning ia/projects/*master-plan*.md task-table
 *      rows via regex.  Maps issue_id → { plan_path, task_key }.
 *   2. For each ia/backlog/*.yaml: if both fields present → skip (idempotent).
 *      Else look up issue_id in plan index → write v2 fields via schema-v2
 *      writer (backlog-yaml-writer.mjs, TECH-365).  On miss → skip w/ reason
 *      (if --skip-unresolvable) or exit 1.
 *
 * Flags:
 *   --dry-run            Preview only; no disk writes.
 *   --skip-unresolvable  Log + skip records with no plan hit; never exit 1 for
 *                        unresolvable.  Exit 1 only on IO/writer error.
 *   --archive            Accepted but no-op (archive scan lives in Step 6 /
 *                        TECH-387 wrapper passthrough).  Emits one warn line.
 *
 * Exit codes:
 *   0  Clean (all resolved or skipped with --skip-unresolvable).
 *   1  Unresolvable record(s) without --skip-unresolvable.
 *   2  IO / writer error.
 */

import fs from "node:fs";
import path from "node:path";
import { parseArgs } from "node:util";
import { fileURLToPath } from "node:url";
import { buildYaml } from "./backlog-yaml-writer.mjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = process.env.IA_REPO_ROOT ?? path.resolve(__dirname, "../..");

// ---------------------------------------------------------------------------
// CLI flags
// ---------------------------------------------------------------------------

const { values: flags } = parseArgs({
  options: {
    "dry-run": { type: "boolean", default: false },
    "skip-unresolvable": { type: "boolean", default: false },
    archive: { type: "boolean", default: false },
  },
  strict: false,
});

const DRY_RUN = flags["dry-run"];
const SKIP_UNRESOLVABLE = flags["skip-unresolvable"];
const ARCHIVE = flags["archive"];

if (ARCHIVE) {
  console.warn("[WARN] --archive flag accepted but archive scan not yet supported (Step 6 / TECH-387). Continuing with open yaml only.");
}

// ---------------------------------------------------------------------------
// Minimal yaml reader — field extraction only, mirrors backlog-yaml-loader.ts
// ---------------------------------------------------------------------------

/**
 * Unquote a yaml scalar value: strip surrounding " or ' + unescape.
 */
function unquote(s) {
  const t = s.trim();
  if (
    (t.startsWith('"') && t.endsWith('"')) ||
    (t.startsWith("'") && t.endsWith("'"))
  ) {
    return t
      .slice(1, -1)
      .replace(/\\n/g, "\n")
      .replace(/\\"/g, '"')
      .replace(/\\\\/g, "\\");
  }
  return t;
}

/**
 * Parse a yaml record string into a plain object.
 * Handles scalar values, block literals (|), inline lists ([]), and indented lists.
 */
function parseYamlRecord(content) {
  const lines = content.split("\n");
  const obj = {};
  let i = 0;

  while (i < lines.length) {
    const line = lines[i];
    if (!line || line.startsWith("#")) { i++; continue; }

    const colonIdx = line.indexOf(": ");
    if (colonIdx < 0) {
      // Bare-colon key (e.g. "files:")
      const bareColon = line.indexOf(":");
      if (bareColon >= 0 && bareColon === line.length - 1) {
        const key = line.slice(0, bareColon).trim();
        i++;
        if (i < lines.length && (lines[i] === "[]" || lines[i].startsWith("  - "))) {
          const items = [];
          while (i < lines.length && lines[i].startsWith("  - ")) {
            items.push(unquote(lines[i].slice(4)));
            i++;
          }
          obj[key] = items;
        } else {
          obj[key] = "";
        }
      } else {
        i++;
      }
      continue;
    }

    const key = line.slice(0, colonIdx).trim();
    const rawVal = line.slice(colonIdx + 2);

    if (rawVal === "|") {
      // Block literal
      i++;
      const blockLines = [];
      while (i < lines.length && (lines[i].startsWith("  ") || lines[i] === "")) {
        blockLines.push(lines[i].startsWith("  ") ? lines[i].slice(2) : "");
        i++;
      }
      while (blockLines.length > 0 && !blockLines[blockLines.length - 1]) blockLines.pop();
      obj[key] = blockLines.join("\n");
      continue;
    }

    if (rawVal === "[]") {
      obj[key] = [];
    } else if (rawVal.trimStart() === "") {
      i++;
      if (i < lines.length && lines[i] && lines[i].trim().startsWith("- ")) {
        const items = [];
        while (i < lines.length && lines[i] && lines[i].trim().startsWith("- ")) {
          items.push(unquote(lines[i].trim().slice(2)));
          i++;
        }
        obj[key] = items;
      } else {
        obj[key] = "";
      }
      continue;
    } else {
      obj[key] = unquote(rawVal);
    }

    i++;
  }

  return obj;
}

/**
 * Convert a parsed yaml record object into the issue shape expected by buildYaml.
 * Mirrors yamlToIssue in backlog-yaml-loader.ts — keeps field semantics identical.
 */
function yamlRecordToIssue(rec, fileHint) {
  const TASK_KEY_RE = /^T\d+\.\d+(\.\d+)?$/;
  const filesArr = Array.isArray(rec.files) ? rec.files : [];
  const filesStr = filesArr.length ? filesArr.map((f) => `\`${f}\``).join(", ") : undefined;
  const dependsOnArr = Array.isArray(rec.depends_on) ? rec.depends_on : [];
  const dependsOnStr =
    rec.depends_on_raw
      ? rec.depends_on_raw
      : dependsOnArr.length
        ? dependsOnArr.join(", ")
        : undefined;

  if (rec.task_key != null && !TASK_KEY_RE.test(rec.task_key)) {
    throw new Error(`invalid task_key '${rec.task_key}' in ${fileHint}: must match ^T\\d+\\.\\d+(\\.\\d+)?$`);
  }

  return {
    issue_id: rec.id ?? "",
    title: rec.title ?? "",
    status: rec.status === "closed" ? "completed" : "open",
    backlog_section: rec.section ?? "",
    type: rec.type || undefined,
    files: filesStr,
    spec: rec.spec && rec.spec !== '""' ? rec.spec : undefined,
    notes: rec.notes || undefined,
    acceptance: rec.acceptance || undefined,
    depends_on: dependsOnStr,
    priority: rec.priority ?? null,
    related: Array.isArray(rec.related) ? rec.related : undefined,
    created: rec.created ?? null,
    raw_markdown: rec.raw_markdown ?? "",
    // v2 locator fields
    parent_plan: rec.parent_plan ?? null,
    task_key: rec.task_key ?? null,
    step: (() => { const n = Number(rec.step); return rec.step != null && !isNaN(n) ? n : null; })(),
    stage: rec.stage ?? null,
    phase: (() => { const n = Number(rec.phase); return rec.phase != null && !isNaN(n) ? n : null; })(),
    router_domain: rec.router_domain ?? null,
    surfaces: Array.isArray(rec.surfaces) ? rec.surfaces : [],
    mcp_slices: Array.isArray(rec.mcp_slices) ? rec.mcp_slices : [],
    skill_hints: Array.isArray(rec.skill_hints) ? rec.skill_hints : [],
  };
}

// ---------------------------------------------------------------------------
// Phase 1 — Build plan index
// ---------------------------------------------------------------------------

/**
 * Regex to match task-table rows in master-plan markdown.
 * Captures:
 *   group 1 = task_key  (e.g. T3.2.3)
 *   group 2 = issue_id  (e.g. TECH-386)
 *
 * Row shape: | {task_key} | {title} | {size} | **{ISSUE_ID}** | {status} | {notes} |
 */
const TASK_ROW_RE = /^\|\s*(T[\d.]+)\s*\|[^|]+\|[^|]+\|\s*\*\*((?:TECH|FEAT|BUG|ART|AUDIO)-\d+)\*\*\s*\|/gm;

function buildPlanIndex() {
  const plansDir = path.join(REPO_ROOT, "ia/projects");
  const files = fs.existsSync(plansDir)
    ? fs.readdirSync(plansDir).filter((f) => f.includes("master-plan") && f.endsWith(".md"))
    : [];

  /** @type {Map<string, { plan_path: string, task_key: string }>} */
  const index = new Map();
  /** @type {string[]} */
  const collisions = [];

  for (const file of files) {
    const planPath = `ia/projects/${file}`;
    const absPath = path.join(REPO_ROOT, planPath);
    let content;
    try {
      content = fs.readFileSync(absPath, "utf8");
    } catch (e) {
      console.warn(`[WARN] Could not read plan file ${planPath}: ${e.message}`);
      continue;
    }

    let m;
    TASK_ROW_RE.lastIndex = 0;
    while ((m = TASK_ROW_RE.exec(content)) !== null) {
      const task_key = m[1];
      const issue_id = m[2];
      if (index.has(issue_id)) {
        const existing = index.get(issue_id);
        collisions.push(`${issue_id}: found in '${existing.plan_path}' (kept) and '${planPath}' (ignored)`);
      } else {
        index.set(issue_id, { plan_path: planPath, task_key });
      }
    }
  }

  if (collisions.length > 0) {
    console.warn(`[WARN] ${collisions.length} collision(s) — same issue_id in multiple plan files (kept first match):`);
    for (const c of collisions) console.warn(`  ${c}`);
  }

  return { index, collisions };
}

// ---------------------------------------------------------------------------
// Phase 2 — Per-yaml resolve + write
// ---------------------------------------------------------------------------

const BACKLOG_DIR = path.join(REPO_ROOT, "ia/backlog");

function run() {
  // Build plan index once
  const { index, collisions } = buildPlanIndex();

  const yamlFiles = fs
    .readdirSync(BACKLOG_DIR)
    .filter((f) => f.endsWith(".yaml"))
    .sort();

  let resolved = 0;
  let alreadyPopulated = 0;
  let skippedTitleSuffix = 0;  // no title suffix — retained for completeness
  let skippedPlanMissing = 0;
  const unresolvableIds = [];

  const prefix = DRY_RUN ? "[DRY RUN] " : "";

  for (const file of yamlFiles) {
    const filePath = path.join(BACKLOG_DIR, file);
    let rec;
    let issue;
    try {
      const content = fs.readFileSync(filePath, "utf8");
      rec = parseYamlRecord(content);
      issue = yamlRecordToIssue(rec, filePath);
    } catch (e) {
      console.error(`[ERROR] Could not parse ${file}: ${e.message}`);
      process.exit(2);
    }

    // Idempotent guard — both fields already populated
    if (issue.parent_plan != null && issue.task_key != null) {
      alreadyPopulated++;
      continue;
    }

    const issueId = issue.issue_id;
    const hit = index.get(issueId);

    if (!hit) {
      // Unresolvable — no plan row found for this issue id
      if (SKIP_UNRESOLVABLE) {
        console.log(`${prefix}SKIP plan-missing  ${issueId}  (no task-table row in any master-plan)`);
        skippedPlanMissing++;
        continue;
      } else {
        unresolvableIds.push(issueId);
        continue;
      }
    }

    // Resolve — mutate in-memory, write via schema-v2 writer
    issue.parent_plan = hit.plan_path;
    issue.task_key = hit.task_key;

    let yaml;
    try {
      yaml = buildYaml(issue);
    } catch (e) {
      console.error(`[ERROR] buildYaml failed for ${issueId}: ${e.message}`);
      process.exit(2);
    }

    if (DRY_RUN) {
      console.log(`${prefix}RESOLVE  ${issueId}  parent_plan=${hit.plan_path}  task_key=${hit.task_key}`);
    } else {
      try {
        fs.writeFileSync(filePath, yaml, "utf8");
      } catch (e) {
        console.error(`[ERROR] Write failed for ${filePath}: ${e.message}`);
        process.exit(2);
      }
      console.log(`${prefix}RESOLVE  ${issueId}  parent_plan=${hit.plan_path}  task_key=${hit.task_key}`);
    }
    resolved++;
  }

  // Summary
  const summaryPrefix = DRY_RUN ? "[DRY RUN] " : "";
  console.log(
    `\n${summaryPrefix}Summary: resolved=${resolved}  already-populated=${alreadyPopulated}` +
    `  skipped-plan-missing=${skippedPlanMissing}  collisions=${collisions.length}`
  );

  // Unresolvable exit
  if (unresolvableIds.length > 0) {
    console.error(`\n[ERROR] ${unresolvableIds.length} unresolvable record(s) (no plan row found). Pass --skip-unresolvable to continue:`);
    for (const id of unresolvableIds) console.error(`  ${id}`);
    process.exit(1);
  }
}

run();
