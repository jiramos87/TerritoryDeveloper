#!/usr/bin/env node
/**
 * TECH-85 — Stage 3 / Phase 3.2.
 *
 * Validate that every Markdown file under `ia/{specs,rules,skills/{name}/SKILL.md,
 * projects,templates}` carries the four required IA frontmatter fields:
 *
 *   purpose, audience, loaded_by, slices_via
 *
 * Schema lives at `ia/templates/frontmatter-schema.md`. Allowed values are
 * checked structurally; the validator is intentionally lenient (presence-only
 * for `purpose`) so prose stays readable.
 *
 * Usage:
 *   node tools/mcp-ia-server/scripts/check-frontmatter.mjs            # advisory: exit 0 with summary
 *   node tools/mcp-ia-server/scripts/check-frontmatter.mjs --strict   # exit 1 on any failure
 *
 * Wired as `npm run validate:frontmatter` (advisory at first; CI promotion deferred).
 */

import { promises as fs } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..", "..");

const REQUIRED = ["purpose", "audience", "loaded_by", "slices_via"];
const ALLOWED_AUDIENCE = new Set(["human", "agent", "both"]);
const ALLOWED_SLICES_VIA = new Set(["spec_section", "glossary_lookup", "none"]);
// `loaded_by` accepts: always | router | ondemand | skill:{name}
function isLoadedByValid(value) {
  if (typeof value !== "string") return false;
  if (["always", "router", "ondemand"].includes(value)) return true;
  return /^skill:[A-Za-z0-9_\-]+$/.test(value);
}

function shouldProcess(rel) {
  if (rel.startsWith("ia/specs/") && rel.endsWith(".md")) return true;
  if (rel.startsWith("ia/rules/") && rel.endsWith(".md")) return true;
  if (rel.startsWith("ia/skills/") && rel.endsWith("/SKILL.md")) return true;
  if (rel.startsWith("ia/projects/") && rel.endsWith(".md")) return true;
  if (rel.startsWith("ia/templates/") && rel.endsWith(".md")) return true;
  return false;
}

async function* walk(dir) {
  for (const entry of await fs.readdir(dir, { withFileTypes: true })) {
    const p = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      yield* walk(p);
    } else if (entry.isFile() && entry.name.endsWith(".md")) {
      yield p;
    }
  }
}

// Minimal frontmatter parser (key: value lines + folded `>` blocks). Sufficient
// for the existing IA shapes. Returns `null` if the file has no frontmatter.
function parseFrontmatter(text) {
  if (!text.startsWith("---")) return null;
  const newline = text.indexOf("\n");
  if (newline < 0) return null;
  const closeIdx = text.indexOf("\n---", newline);
  if (closeIdx < 0) return null;
  const fm = text.slice(newline + 1, closeIdx);
  const lines = fm.split("\n");
  const out = {};
  let i = 0;
  while (i < lines.length) {
    const line = lines[i];
    const m = line.match(/^([A-Za-z_][\w-]*):\s*(.*)$/);
    if (!m) { i++; continue; }
    const key = m[1];
    const rawValue = m[2];
    if (rawValue === ">" || rawValue === "|" || rawValue === ">-" || rawValue === "|-") {
      const buf = [];
      i++;
      while (i < lines.length && /^( +|\t)/.test(lines[i])) {
        buf.push(lines[i].replace(/^\s+/, ""));
        i++;
      }
      out[key] = buf.join(" ").trim();
      continue;
    }
    out[key] = rawValue.replace(/^["']|["']$/g, "").trim();
    i++;
  }
  return out;
}

function validateFile(rel, fm) {
  const errors = [];
  if (!fm) {
    errors.push("missing YAML frontmatter");
    return errors;
  }
  for (const k of REQUIRED) {
    if (fm[k] == null || fm[k] === "") {
      errors.push(`missing field: ${k}`);
    }
  }
  if (fm.audience && !ALLOWED_AUDIENCE.has(fm.audience)) {
    errors.push(`invalid audience: ${fm.audience} (allowed: ${[...ALLOWED_AUDIENCE].join("|")})`);
  }
  if (fm.loaded_by && !isLoadedByValid(fm.loaded_by)) {
    errors.push(`invalid loaded_by: ${fm.loaded_by} (allowed: always|router|ondemand|skill:{name})`);
  }
  if (fm.slices_via && !ALLOWED_SLICES_VIA.has(fm.slices_via)) {
    errors.push(`invalid slices_via: ${fm.slices_via} (allowed: ${[...ALLOWED_SLICES_VIA].join("|")})`);
  }
  return errors;
}

async function main() {
  const strict = process.argv.includes("--strict");
  const iaDir = path.join(REPO_ROOT, "ia");
  const failures = [];
  let total = 0;
  for await (const abs of walk(iaDir)) {
    const rel = path.relative(REPO_ROOT, abs).replace(/\\/g, "/");
    if (!shouldProcess(rel)) continue;
    total++;
    const text = await fs.readFile(abs, "utf8");
    const fm = parseFrontmatter(text);
    const errors = validateFile(rel, fm);
    if (errors.length > 0) failures.push({ rel, errors });
  }

  const ok = failures.length === 0;
  if (ok) {
    console.log(`validate:frontmatter — OK (${total} files)`);
    process.exit(0);
  }

  console.log(`validate:frontmatter — ${failures.length} / ${total} files failed`);
  for (const f of failures) {
    console.log(`  ${f.rel}`);
    for (const e of f.errors) console.log(`    · ${e}`);
  }
  if (strict) process.exit(1);
  console.log("\n(advisory mode — exiting 0; pass --strict to fail)");
  process.exit(0);
}

main().catch((err) => {
  console.error(err);
  process.exit(2);
});
