#!/usr/bin/env node
/**
 * ship-plan Phase A.1 — Node helper for parse-handoff-yaml.sh.
 *
 * Reads docs/explorations/{slug}.md frontmatter via js-yaml, derives the
 * downstream payload shape consumed by ship-plan-phase-a recipe (lint_tasks
 * pivot, anchors flat list, glossary_terms flat list, task_keys list).
 */

import { readFile } from "node:fs/promises";
import path from "node:path";
import yaml from "js-yaml";

function parseArgs(argv) {
  const out = {};
  for (let i = 2; i < argv.length; i += 2) {
    const k = argv[i];
    const v = argv[i + 1];
    if (k === "--handoff-path") out.handoffPath = v;
    else if (k === "--slug") out.slug = v;
  }
  return out;
}

function extractFrontmatter(src) {
  if (!src.startsWith("---\n") && !src.startsWith("---\r\n")) {
    throw new Error("handoff doc missing leading frontmatter fence");
  }
  const after = src.replace(/^---\r?\n/, "");
  const closeIdx = after.search(/\n---\r?\n/);
  if (closeIdx < 0) {
    throw new Error("handoff doc frontmatter not closed by `---`");
  }
  return after.slice(0, closeIdx);
}

const args = parseArgs(process.argv);
if (!args.handoffPath || !args.slug) {
  console.error("parse-handoff-yaml.mjs: missing --handoff-path or --slug");
  process.exit(1);
}

const abs = path.resolve(args.handoffPath);
const src = await readFile(abs, "utf8");
const fm = extractFrontmatter(src);
const data = yaml.load(fm) ?? {};

// Plan block — handoff may declare `plan:` explicitly OR put fields top-level
// (slug, target_version, parent_plan_id, parent_rationale). Synthesize when
// absent so master_plan_bundle_apply receives a minimum-valid plan row.
const plan = data.plan && typeof data.plan === "object"
  ? data.plan
  : {
      slug: data.slug ?? args.slug,
      version: data.target_version ?? 1,
      parent_plan_slug: data.parent_plan_slug ?? data.parent_plan_id ?? null,
      description: data.parent_rationale ?? data.description ?? "",
      title: data.title ?? "",
    };
const stages = Array.isArray(data.stages) ? data.stages : [];
const topLevelTasks = Array.isArray(data.tasks) ? data.tasks : [];

// New-plan handoff keeps tasks nested under stages[].tasks[]. Flatten when
// top-level tasks[] empty so downstream consumers see a unified task list.
const flattenedFromStages = [];
for (const stage of stages) {
  const stageTasks = Array.isArray(stage?.tasks) ? stage.tasks : [];
  for (const t of stageTasks) {
    flattenedFromStages.push({ ...t, stage_id: stage?.id ?? null });
  }
}

const tasks = topLevelTasks.length > 0 ? topLevelTasks : flattenedFromStages;

// task_ids = DB-issued ids (TECH-NNNN). For new-plan path tasks have no
// DB id yet — emit empty array; task_bundle_batch returns empty result
// per its schema ("Empty array → empty result"). Avoids recipe crash.
const taskIds = tasks
  .map((t) => t.task_id)
  .filter((x) => typeof x === "string" && x);

const anchorsSet = new Set();
const glossarySet = new Set();
const lintTasks = [];

// Harvest stage-level red_stage_proof_block.red_test_anchor (separate from
// per-task anchors so it doesn't inflate task count).
for (const stage of stages) {
  const stageAnchor = stage?.red_stage_proof_block?.red_test_anchor;
  if (typeof stageAnchor === "string" && stageAnchor) {
    anchorsSet.add(stageAnchor);
  }
}

for (const t of tasks) {
  const taskKey = t.task_key ?? t.task_id ?? t.id ?? "";
  const body = typeof t.body === "string" ? t.body : (t.digest_body ?? t.digest_outline ?? "");
  const taskAnchors = Array.isArray(t.anchors) ? t.anchors.filter((x) => typeof x === "string") : [];
  const taskTerms = Array.isArray(t.glossary_terms)
    ? t.glossary_terms.filter((x) => typeof x === "string")
    : [];
  for (const a of taskAnchors) anchorsSet.add(a);
  for (const g of taskTerms) glossarySet.add(g);
  lintTasks.push({
    task_key: taskKey,
    body,
    anchors: taskAnchors,
    glossary_terms: taskTerms,
  });
}

// Split anchors: spec-doc refs ({spec}::{section}) go to spec_sections batch;
// test-file refs (tests/... or *.cs::Method) stay in anchors for §Red-Stage
// Proof carry-through only (no MCP slice).
const specSectionRequests = [];
for (const anchor of anchorsSet) {
  const m = anchor.match(/^([^:]+)::(.+)$/);
  if (!m) continue;
  const lhs = m[1];
  const rhs = m[2];
  // Heuristic: spec ref when lhs has no path separator AND no file extension,
  // OR lhs starts with ia/specs/. Test/code refs always rejected (path/dot).
  const looksLikeSpecKey = !lhs.includes("/") && !lhs.includes(".");
  const looksLikeSpecPath = lhs.startsWith("ia/specs/");
  if (looksLikeSpecKey || looksLikeSpecPath) {
    specSectionRequests.push({ spec: lhs, section: rhs });
  }
}

const out = {
  plan_version: plan.version ?? data.target_version ?? 1,
  plan,
  stages,
  tasks,
  task_ids: taskIds,
  anchors: [...anchorsSet],
  spec_section_requests: specSectionRequests,
  glossary_terms: [...glossarySet],
  lint_tasks: lintTasks,
};

process.stdout.write(JSON.stringify(out));
