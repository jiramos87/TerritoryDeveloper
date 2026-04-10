#!/usr/bin/env node
// One-off Phase 3.3 cleanup: rewrite `.cursor/{specs,rules,skills,projects,
// templates}` and well-known `*.mdc` rule references inside the active IA
// surfaces (specs, rules, skills, templates, the project-spec meta files) to
// the neutral `ia/` namespace and `.md` extension. Intentionally SKIPS:
//
//   - ia/projects/{ID}.md project specs (historical context — migrated /
//     deleted at issue close, not at namespace migration time)
//   - ia/projects/TECH-85-ia-migration.md (the migration spec itself, which
//     legitimately documents the `.cursor/` → `ia/` rename)
//
// Run from repo root: `node tools/scripts/rewrite-cursor-paths-in-ia.mjs`.

import { promises as fs } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "..", "..");

// Whitelisted active-surface directories (recursive walk under each).
const SCOPES = ["ia/specs", "ia/rules", "ia/skills", "ia/templates"];
// Plus a small set of explicit files outside those scopes.
const EXTRA_FILES = ["ia/projects/PROJECT-SPEC-STRUCTURE.md"];

// Skip patterns (always preserve these files exactly as-is).
function isSkipped(rel) {
  if (rel === "ia/projects/TECH-85-ia-migration.md") return true;
  if (rel.startsWith("ia/projects/") && rel !== "ia/projects/PROJECT-SPEC-STRUCTURE.md") return true;
  return false;
}

// Rewrite rules. Order matters: longer / more specific first so prefixes do
// not steal matches from shorter forms.
const REWRITES = [
  // Path prefixes (with trailing slash).
  [/\.cursor\/specs\//g,     "ia/specs/"],
  [/\.cursor\/rules\//g,     "ia/rules/"],
  [/\.cursor\/skills\//g,    "ia/skills/"],
  [/\.cursor\/projects\//g,  "ia/projects/"],
  [/\.cursor\/templates\//g, "ia/templates/"],

  // Bare directory references (no trailing slash, not followed by path char).
  [/\.cursor\/specs\b/g,     "ia/specs"],
  [/\.cursor\/rules\b/g,     "ia/rules"],
  [/\.cursor\/skills\b/g,    "ia/skills"],
  [/\.cursor\/projects\b/g,  "ia/projects"],
  [/\.cursor\/templates\b/g, "ia/templates"],

  // Known rule files: `.mdc` → `.md` (only inside `ia/rules/` references and
  // bare filename mentions of canonical rule names).
  [/(ia\/rules\/[A-Za-z0-9_\-]+)\.mdc/g, "$1.md"],
  [/\binvariants\.mdc\b/g,                "invariants.md"],
  [/\bagent-router\.mdc\b/g,              "agent-router.md"],
  [/\bcoding-conventions\.mdc\b/g,        "coding-conventions.md"],
  [/\bterminology-consistency\.mdc\b/g,   "terminology-consistency.md"],
  [/\bmcp-ia-default\.mdc\b/g,            "mcp-ia-default.md"],
  [/\bagent-verification-directives\.mdc\b/g, "agent-verification-directives.md"],
  [/\bproject-overview\.mdc\b/g,          "project-overview.md"],
  [/\bmanagers-guide\.mdc\b/g,            "managers-guide.md"],
  [/\bpersistence\.mdc\b/g,               "persistence.md"],
  [/\broads\.mdc\b/g,                     "roads.md"],
  [/\bsimulation\.mdc\b/g,                "simulation.md"],
  [/\bwater-terrain\.mdc\b/g,             "water-terrain.md"],
];

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

async function processFile(absPath) {
  const rel = path.relative(repoRoot, absPath).replace(/\\/g, "/");
  if (isSkipped(rel)) return null;
  const before = await fs.readFile(absPath, "utf8");
  let after = before;
  for (const [pattern, replacement] of REWRITES) {
    after = after.replace(pattern, replacement);
  }
  if (after === before) return { rel, changed: false };
  await fs.writeFile(absPath, after);
  return { rel, changed: true };
}

async function main() {
  const seen = new Set();
  const results = [];
  for (const scope of SCOPES) {
    const abs = path.join(repoRoot, scope);
    try {
      for await (const f of walk(abs)) {
        if (seen.has(f)) continue;
        seen.add(f);
        const r = await processFile(f);
        if (r) results.push(r);
      }
    } catch (err) {
      if (err.code !== "ENOENT") throw err;
    }
  }
  for (const rel of EXTRA_FILES) {
    const abs = path.join(repoRoot, rel);
    if (seen.has(abs)) continue;
    seen.add(abs);
    try {
      const r = await processFile(abs);
      if (r) results.push(r);
    } catch (err) {
      if (err.code !== "ENOENT") throw err;
    }
  }
  const changed = results.filter((r) => r.changed);
  console.log(JSON.stringify({ scanned: results.length, changed: changed.length }, null, 2));
  for (const r of changed) console.log("  ~", r.rel);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
