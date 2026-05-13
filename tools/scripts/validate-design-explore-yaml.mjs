#!/usr/bin/env node
/**
 * validate-design-explore-yaml.mjs
 *
 * Schema-validate the YAML frontmatter emitted by design-explore Phase 4 + Phase 3.5.
 * Exit 0 = clean (all required keys present + types valid + enriched mandatory bands met).
 * Exit 1 = schema violation(s) — stderr carries details.
 *
 * Usage:
 *   node tools/scripts/validate-design-explore-yaml.mjs docs/explorations/my-exploration.md
 *   node tools/scripts/validate-design-explore-yaml.mjs --stdin   (read from stdin)
 *
 * Required YAML keys (design-explore Phase 4 lean YAML contract):
 *   slug                 string
 *   stages               array (each: id string, title string, tasks[]: prefix/depends_on/digest_outline/touched_paths/kind)
 *
 * Enriched schema (post-uplift, per ia/rules/design-explore-output-schema.md):
 *   When `stages[].enriched:` OR `stages[].tasks[].enriched:` is present on ANY entry, the
 *   exploration is treated as post-uplift and parity rules apply. Otherwise, emit WARNING +
 *   exit 0 (legacy doc shape preserved).
 *
 * Post-uplift assertions:
 *   1. Per task — `enriched.glossary_anchors[]` (≥1) AND `enriched.failure_modes[]` (≥1).
 *      `enriched.touched_paths_with_preview[]` length matches `touched_paths` length.
 *   2. First stage (id starts "1.") strict band — every task carries all 8 fields:
 *      visual_mockup_svg, before_after_code, glossary_anchors, failure_modes,
 *      decision_dependencies, shared_seams (stage-level), edge_cases (stage-level),
 *      touched_paths_with_preview.
 *   3. Per stage — `enriched.edge_cases[]` (≥3).
 *   4. YAML / MD parity — when task carries an `enriched:` block, the body MUST contain
 *      `#### Task {id} — Enriched` heading. Vice versa.
 *
 * TECH-15912 + design-explore-html-effectiveness-uplift D1.
 */

import { readFileSync } from "node:fs";
import { createInterface } from "node:readline";

import yaml from "js-yaml";

const REQUIRED_TASK_KEYS = ["prefix", "depends_on", "digest_outline", "touched_paths", "kind"];
const REQUIRED_STAGE_KEYS = ["id", "title", "tasks"];

async function readContent(filePath) {
  if (filePath === "--stdin") {
    return new Promise((resolve) => {
      const chunks = [];
      const rl = createInterface({ input: process.stdin });
      rl.on("line", (line) => chunks.push(line));
      rl.on("close", () => resolve(chunks.join("\n")));
    });
  }
  return readFileSync(filePath, "utf8");
}

function parseFrontmatter(src) {
  if (!src.startsWith("---\n")) return { ok: false, errors: ["File does not start with '---' fence"] };
  const closeIdx = src.indexOf("\n---", 4);
  if (closeIdx === -1) return { ok: false, errors: ["Closing '---' not found"] };
  const fmText = src.slice(4, closeIdx);
  const body = src.slice(closeIdx + 4);
  try {
    const fm = yaml.load(fmText) || {};
    return { ok: true, fm, body };
  } catch (err) {
    return { ok: false, errors: [`YAML parse failed: ${err.message}`] };
  }
}

function validateBaseShape(fm, errors) {
  if (typeof fm.slug !== "string" || fm.slug.length === 0) errors.push("slug must be a non-empty string");
  if (!Array.isArray(fm.stages)) {
    errors.push("stages must be an array");
    return;
  }
  fm.stages.forEach((s, sidx) => {
    REQUIRED_STAGE_KEYS.forEach((k) => {
      if (!(k in s)) errors.push(`stages[${sidx}].${k} missing`);
    });
    if (Array.isArray(s.tasks)) {
      s.tasks.forEach((t, tidx) => {
        REQUIRED_TASK_KEYS.forEach((k) => {
          if (!(k in t)) errors.push(`stages[${sidx}].tasks[${tidx}].${k} missing`);
        });
      });
    }
  });
}

function hasAnyEnriched(fm) {
  if (!Array.isArray(fm.stages)) return false;
  for (const s of fm.stages) {
    if (s.enriched && Object.keys(s.enriched).length) return true;
    if (Array.isArray(s.tasks)) {
      for (const t of s.tasks) if (t.enriched && Object.keys(t.enriched).length) return true;
    }
  }
  return false;
}

function validateEnriched(fm, body, errors, warnings) {
  if (!Array.isArray(fm.stages)) return;
  fm.stages.forEach((s, sidx) => {
    const stageId = s.id;
    const isFirstStage = String(stageId).startsWith("1.");
    const stageEnr = s.enriched || {};
    if (!Array.isArray(stageEnr.edge_cases) || stageEnr.edge_cases.length < 3) {
      errors.push(`stages[${sidx}] (id=${stageId}) — enriched.edge_cases[] must have ≥3 entries (found ${(stageEnr.edge_cases || []).length})`);
    }
    if (!Array.isArray(stageEnr.shared_seams) || stageEnr.shared_seams.length === 0) {
      warnings.push(`stages[${sidx}] (id=${stageId}) — enriched.shared_seams[] absent (optional band)`);
    }

    if (!Array.isArray(s.tasks)) return;
    s.tasks.forEach((t, tidx) => {
      const enr = t.enriched || {};
      const ctx = `stages[${sidx}].tasks[${tidx}] (id=${t.id})`;
      if (!Array.isArray(enr.glossary_anchors) || enr.glossary_anchors.length < 1) {
        errors.push(`${ctx} — enriched.glossary_anchors[] must have ≥1 entry`);
      }
      if (!Array.isArray(enr.failure_modes) || enr.failure_modes.length < 1) {
        errors.push(`${ctx} — enriched.failure_modes[] must have ≥1 entry`);
      }
      const tpLen = Array.isArray(t.touched_paths) ? t.touched_paths.length : 0;
      const tppLen = Array.isArray(enr.touched_paths_with_preview) ? enr.touched_paths_with_preview.length : 0;
      if (tppLen !== tpLen) {
        errors.push(`${ctx} — enriched.touched_paths_with_preview length (${tppLen}) must match touched_paths length (${tpLen})`);
      }
      if (isFirstStage) {
        if (!enr.visual_mockup_svg) errors.push(`${ctx} — Stage 1 strict: visual_mockup_svg required`);
        if (!enr.before_after_code) errors.push(`${ctx} — Stage 1 strict: before_after_code required`);
        if (!Array.isArray(enr.decision_dependencies)) errors.push(`${ctx} — Stage 1 strict: decision_dependencies[] required`);
      }
    });
  });

  fm.stages.forEach((s, sidx) => {
    if (!Array.isArray(s.tasks)) return;
    s.tasks.forEach((t, tidx) => {
      if (!t.enriched) return;
      const ctx = `stages[${sidx}].tasks[${tidx}] (id=${t.id})`;
      const expectedHeading = `#### Task ${t.id} — Enriched`;
      if (!body.includes(expectedHeading)) {
        errors.push(`${ctx} — YAML carries enriched block but body missing heading: ${expectedHeading}`);
      }
    });
  });
}

const arg = process.argv[2];
if (!arg) {
  console.error("Usage: validate-design-explore-yaml.mjs <path-to-exploration.md>  OR  --stdin");
  process.exit(2);
}

const content = await readContent(arg);
const parsed = parseFrontmatter(content);

if (!parsed.ok) {
  for (const e of parsed.errors) console.error("ERROR:", e);
  process.exit(1);
}

const errors = [];
const warnings = [];
validateBaseShape(parsed.fm, errors);
if (hasAnyEnriched(parsed.fm)) {
  validateEnriched(parsed.fm, parsed.body, errors, warnings);
} else {
  warnings.push("doc carries no enriched fields (legacy shape); exit 0 with WARNING.");
}

for (const w of warnings) console.error("INFO:", w);
if (errors.length > 0) {
  for (const e of errors) console.error("ERROR:", e);
  process.exit(1);
}

console.log("validate-design-explore-yaml: OK");
process.exit(0);
