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

const plan = data.plan ?? {};
const stages = Array.isArray(data.stages) ? data.stages : [];
const tasks = Array.isArray(data.tasks) ? data.tasks : [];

const taskKeys = tasks.map((t) => t.task_key).filter((x) => typeof x === "string" && x);

const anchorsSet = new Set();
const glossarySet = new Set();
const lintTasks = [];

for (const t of tasks) {
  const taskKey = t.task_key ?? "";
  const body = typeof t.body === "string" ? t.body : (t.digest_body ?? "");
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

const out = {
  plan_version: plan.version ?? 1,
  plan,
  stages,
  tasks,
  task_keys: taskKeys,
  anchors: [...anchorsSet],
  glossary_terms: [...glossarySet],
  lint_tasks: lintTasks,
};

process.stdout.write(JSON.stringify(out));
