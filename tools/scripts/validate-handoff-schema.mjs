#!/usr/bin/env node
/**
 * validate-handoff-schema.mjs — TECH-12634 (ship-protocol Stage 2).
 *
 * Validates lean handoff YAML frontmatter at top of `docs/explorations/{slug}.md`
 * against the schema consumed by `ship-plan` Phase 1.
 *
 * Required top-level: slug, target_version, stages[].
 * Optional top-level: parent_plan_id, notes.
 * Required per stage: id, title, exit, red_stage_proof, tasks[].
 * Required per task: id, title, prefix, digest_outline, kind.
 * Optional per task: depends_on, touched_paths.
 *
 * Args:
 *   `node validate-handoff-schema.mjs {path-or-glob}` — path or shell-expanded glob
 *   No args → `docs/explorations/*.md`.
 *
 * Output: structured `{file, field, expected, got}` JSON line per stderr violation.
 * Exit: 0 on all-pass, 1 on any schema violation.
 */

import { readFileSync, existsSync } from "node:fs";
import { glob } from "node:fs/promises";
import yaml from "js-yaml";

const PREFIX_ENUM = new Set(["TECH", "FEAT", "BUG", "ART", "AUDIO"]);
const KIND_ENUM = new Set(["code", "doc-only", "mcp-only"]);

function emitError(file, field, expected, got) {
  process.stderr.write(
    JSON.stringify({ file, field, expected, got }) + "\n"
  );
}

function parseFrontmatter(file, src) {
  const lines = src.split(/\r?\n/);
  if (lines[0]?.trim() !== "---") {
    // No frontmatter at all → not a handoff doc; silent pass.
    return { skip: true };
  }
  let endIdx = -1;
  for (let i = 1; i < lines.length; i++) {
    if (lines[i].trim() === "---") {
      endIdx = i;
      break;
    }
  }
  if (endIdx < 0) {
    return { error: "missing_frontmatter_close_fence" };
  }
  const body = lines.slice(1, endIdx).join("\n");
  try {
    const parsed = yaml.load(body);
    if (parsed === null || parsed === undefined) {
      return { error: "empty_frontmatter" };
    }
    if (typeof parsed !== "object" || Array.isArray(parsed)) {
      return { error: "frontmatter_not_object" };
    }
    return { frontmatter: parsed };
  } catch (err) {
    return { error: `yaml_parse_error: ${err.message}` };
  }
}

function validateTopLevel(file, fm, errors) {
  // Skip non-handoff frontmatter (exploration docs may carry other YAML shapes too).
  // Heuristic: must carry `slug` AND (`stages` OR `target_version`) to be considered a handoff.
  if (!("slug" in fm)) {
    // Not a handoff doc — silent pass. Allows exploration docs without handoff.
    return false;
  }
  if (typeof fm.slug !== "string" || fm.slug.length === 0) {
    emitError(file, "slug", "non-empty string", typeof fm.slug);
  }
  if (
    !("target_version" in fm) &&
    !("stages" in fm)
  ) {
    // Has slug but no handoff markers — silent pass.
    return false;
  }
  if (!("target_version" in fm)) {
    emitError(file, "target_version", "integer", "undefined");
  } else if (!Number.isInteger(fm.target_version) || fm.target_version < 1) {
    emitError(file, "target_version", "positive integer", String(fm.target_version));
  }
  if ("parent_plan_id" in fm && fm.parent_plan_id !== null) {
    if (!Number.isInteger(fm.parent_plan_id) || fm.parent_plan_id < 1) {
      emitError(file, "parent_plan_id", "positive integer or null", String(fm.parent_plan_id));
    }
  }
  if (!("stages" in fm)) {
    emitError(file, "stages", "array", "undefined");
    return true;
  }
  if (!Array.isArray(fm.stages)) {
    emitError(file, "stages", "array", typeof fm.stages);
    return true;
  }
  return true;
}

function validateStage(file, stage, idx, errors) {
  const ctx = `stages[${idx}]`;
  for (const k of ["id", "title", "exit", "red_stage_proof"]) {
    if (!(k in stage)) {
      emitError(file, `${ctx}.${k}`, "non-empty string", "undefined");
    } else if (typeof stage[k] !== "string" || stage[k].trim().length === 0) {
      emitError(file, `${ctx}.${k}`, "non-empty string", String(stage[k]));
    }
  }
  if (!("tasks" in stage)) {
    emitError(file, `${ctx}.tasks`, "array", "undefined");
    return;
  }
  if (!Array.isArray(stage.tasks)) {
    emitError(file, `${ctx}.tasks`, "array", typeof stage.tasks);
    return;
  }
  if (stage.tasks.length === 0) {
    emitError(file, `${ctx}.tasks`, "non-empty array", "[]");
    return;
  }
  stage.tasks.forEach((task, tIdx) => {
    validateTask(file, task, idx, tIdx);
  });
}

function validateTask(file, task, sIdx, tIdx) {
  const ctx = `stages[${sIdx}].tasks[${tIdx}]`;
  for (const k of ["id", "title", "prefix", "digest_outline", "kind"]) {
    if (!(k in task)) {
      emitError(file, `${ctx}.${k}`, "non-empty string", "undefined");
    } else if (typeof task[k] !== "string" || task[k].trim().length === 0) {
      emitError(file, `${ctx}.${k}`, "non-empty string", String(task[k]));
    }
  }
  if (typeof task.prefix === "string" && !PREFIX_ENUM.has(task.prefix)) {
    emitError(
      file,
      `${ctx}.prefix`,
      "one of TECH | FEAT | BUG | ART | AUDIO",
      task.prefix
    );
  }
  if (typeof task.kind === "string" && !KIND_ENUM.has(task.kind)) {
    emitError(file, `${ctx}.kind`, "one of code | doc-only | mcp-only", task.kind);
  }
  if ("depends_on" in task) {
    if (!Array.isArray(task.depends_on)) {
      emitError(file, `${ctx}.depends_on`, "array of strings", typeof task.depends_on);
    } else {
      task.depends_on.forEach((d, i) => {
        if (typeof d !== "string" || d.length === 0) {
          emitError(file, `${ctx}.depends_on[${i}]`, "non-empty string", String(d));
        }
      });
    }
  }
  if ("touched_paths" in task) {
    if (!Array.isArray(task.touched_paths)) {
      emitError(file, `${ctx}.touched_paths`, "array of strings", typeof task.touched_paths);
    } else {
      task.touched_paths.forEach((p, i) => {
        if (typeof p !== "string" || p.length === 0) {
          emitError(file, `${ctx}.touched_paths[${i}]`, "non-empty string", String(p));
        }
      });
    }
  }
}

async function resolveTargets(args) {
  if (args.length === 0) {
    return await Array.fromAsync(glob("docs/explorations/*.md"));
  }
  return args;
}

async function main() {
  const args = process.argv.slice(2);
  const targets = await resolveTargets(args);
  let totalErrors = 0;
  let filesChecked = 0;
  for (const file of targets) {
    if (!existsSync(file)) {
      emitError(file, "<file>", "exists", "missing");
      totalErrors++;
      continue;
    }
    const src = readFileSync(file, "utf8");
    const parsed = parseFrontmatter(file, src);
    if (parsed.skip) continue;
    if (parsed.error) {
      emitError(file, "<frontmatter>", "valid YAML frontmatter", parsed.error);
      totalErrors++;
      continue;
    }
    const before = totalErrors;
    const isHandoff = validateTopLevel(file, parsed.frontmatter, []);
    if (!isHandoff) continue;
    filesChecked++;
    const errCountBefore = totalErrors;
    const stages = parsed.frontmatter.stages;
    if (Array.isArray(stages)) {
      stages.forEach((s, i) => validateStage(file, s, i));
    }
    // Count via stderr writes — re-tally by line count is impractical; track per-file via wrapper.
  }
  // Re-derive total by counting captured emit calls — simpler: track via global counter.
  process.exit(globalErrorCount > 0 ? 1 : 0);
}

let globalErrorCount = 0;
const _emit = emitError;
// monkey-patch emitError above — replaced by wrapper to count
// (kept top-level function so call sites stay clean)

// Wrap stderr write to count
const origWrite = process.stderr.write.bind(process.stderr);
process.stderr.write = (chunk, ...rest) => {
  if (typeof chunk === "string" && chunk.startsWith("{")) {
    globalErrorCount++;
  }
  return origWrite(chunk, ...rest);
};

main().catch((err) => {
  process.stderr.write(
    JSON.stringify({ file: "<runner>", field: "<unhandled>", expected: "no-throw", got: err.message }) + "\n"
  );
  process.exit(2);
});
