#!/usr/bin/env node
/**
 * backfill-task-stage-linkage.mjs
 *
 * One-shot recovery: re-associates archived ia_tasks rows with their parent
 * (slug, stage_id) by parsing pre-deletion master-plan markdown blobs from
 * git commit d657e35^ (the commit before Step 9.4 folded master plans into
 * per-stage folders).
 *
 * Symptom — post `feature/ia-dev-db-refactor` merge:
 *   706 tasks have status='archived' but slug=NULL, stage_id=NULL.
 *   Dashboard query (`WHERE slug IS NOT NULL`) excludes them, so per-plan
 *   progress shows 0 done across most stages.
 *
 * Strategy:
 *   1. For each slug in ia_master_plans, fetch markdown via
 *      `git show d657e35^:ia/projects/{slug}-master-plan.md`.
 *   2. Parse `### Stage N — title` blocks; within each, parse the
 *      | Task | Issue | Status | Intent | table for issue ids.
 *   3. Emit SQL UPDATE ia_tasks SET slug=?, stage_id=? WHERE task_id=?
 *      AND slug IS NULL  (idempotent — won't overwrite existing linkage).
 *
 * Output: SQL on stdout. Pipe to psql:
 *   node tools/scripts/backfill-task-stage-linkage.mjs | psql $DATABASE_URL
 */

import { execFileSync } from "node:child_process";

const PRE_DELETE_REF = "d657e35^";
const ISSUE_RE = /\b(BUG|FEAT|TECH|ART|AUDIO)-\d+[a-z]?\b/;

const slugs = [
  "backlog-yaml-mcp-alignment", "blip", "city-sim-depth", "citystats-overhaul",
  "distribution", "full-game-mvp", "grid-asset-visual-registry", "landmarks",
  "lifecycle-refactor", "mcp-lifecycle-tools-opus-4-7-audit", "multi-scale",
  "music-player", "session-token-latency", "skill-training", "sprite-gen",
  "ui-polish", "unity-agent-bridge", "utilities", "web-platform", "zone-s-economy",
];

function fetchBlob(slug) {
  try {
    return execFileSync("git", ["show", `${PRE_DELETE_REF}:ia/projects/${slug}-master-plan.md`], {
      encoding: "utf8", maxBuffer: 50 * 1024 * 1024,
    });
  } catch {
    return null;
  }
}

function parseTasks(markdown, slug) {
  const lines = markdown.split("\n");
  const out = [];
  let stageId = null;
  let inFence = false;
  let inTaskTable = false;
  let issueColIdx = -1;

  for (const line of lines) {
    const trimmed = line.trim();
    if (trimmed.startsWith("```")) { inFence = !inFence; continue; }
    if (inFence) continue;

    const stageMatch = line.match(/^#{3,4}\s+Stage\s+([0-9.]+(?:\s+\w+)?)\s+—/);
    if (stageMatch) {
      stageId = stageMatch[1].trim();
      inTaskTable = false;
      issueColIdx = -1;
      continue;
    }
    if (/^##\s+/.test(line)) {
      stageId = null;
      inTaskTable = false;
      continue;
    }

    if (!stageId) continue;
    if (!trimmed.startsWith("|")) {
      if (inTaskTable && trimmed === "") inTaskTable = false;
      continue;
    }

    const cells = trimmed.split("|").slice(1, -1).map(c => c.trim());
    if (!inTaskTable) {
      const lower = cells.map(c => c.toLowerCase());
      const taskI = lower.findIndex(c => c === "task");
      const issueI = lower.findIndex(c => c === "issue");
      if (taskI >= 0 && issueI >= 0) {
        inTaskTable = true;
        issueColIdx = issueI;
      }
      continue;
    }
    if (cells.every(c => /^[-: ]+$/.test(c))) continue;

    const issueCell = (cells[issueColIdx] ?? "").replace(/\*\*/g, "").trim();
    const m = issueCell.match(ISSUE_RE);
    if (m) out.push({ task_id: m[0], slug, stage_id: stageId });
  }
  return out;
}

function sqlEsc(s) { return s.replace(/'/g, "''"); }

console.log("BEGIN;");
console.log("-- backfill-task-stage-linkage.mjs — restores (slug, stage_id) on archived ia_tasks");

let total = 0;
const perSlug = {};
for (const slug of slugs) {
  const md = fetchBlob(slug);
  if (!md) { console.log(`-- skip ${slug}: blob not found at ${PRE_DELETE_REF}`); continue; }
  const tasks = parseTasks(md, slug);
  perSlug[slug] = tasks.length;
  total += tasks.length;
  for (const t of tasks) {
    console.log(
      `UPDATE ia_tasks SET slug='${sqlEsc(t.slug)}', stage_id='${sqlEsc(t.stage_id)}' ` +
      `WHERE task_id='${sqlEsc(t.task_id)}' AND slug IS NULL;`
    );
  }
}
console.log(`-- summary: ${total} task linkage UPDATE statements emitted`);
for (const [slug, n] of Object.entries(perSlug).sort()) {
  console.log(`--   ${slug}: ${n}`);
}
console.log("COMMIT;");
