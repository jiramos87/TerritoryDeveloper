#!/usr/bin/env node
// One-off migration: add the four-field IA frontmatter (purpose, audience,
// loaded_by, slices_via) to every Markdown file under ia/{specs,rules,
// skills/{name}/SKILL.md,projects,templates}. Idempotent: re-running leaves
// already-migrated files alone.

import { promises as fs } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "..", "..");

const REQUIRED = ["purpose", "audience", "loaded_by", "slices_via"];

// Per-file overrides for atypical cases (CLAUDE.md @-imports, glossary, etc.)
const OVERRIDES = {
  "ia/rules/invariants.md":              { loaded_by: "always",   slices_via: "none" },
  "ia/rules/terminology-consistency.md": { loaded_by: "always",   slices_via: "none" },
  "ia/rules/mcp-ia-default.md":          { loaded_by: "always",   slices_via: "none" },
  "ia/rules/agent-router.md":            { loaded_by: "always",   slices_via: "none" },
  "ia/rules/agent-verification-directives.md": { loaded_by: "always", slices_via: "none" },
  "ia/rules/project-overview.md":        { loaded_by: "always",   slices_via: "none" },
  "ia/specs/glossary.md":                { loaded_by: "router",   slices_via: "glossary_lookup" },
  "ia/specs/REFERENCE-SPEC-STRUCTURE.md":{ loaded_by: "ondemand", slices_via: "none" },
};

function defaultsFor(relPath) {
  if (relPath.startsWith("ia/specs/")) {
    return { audience: "agent", loaded_by: "router", slices_via: "spec_section" };
  }
  if (relPath.startsWith("ia/rules/")) {
    return { audience: "agent", loaded_by: "router", slices_via: "none" };
  }
  if (relPath.startsWith("ia/skills/") && relPath.endsWith("/SKILL.md")) {
    const name = relPath.split("/")[2];
    return { audience: "agent", loaded_by: `skill:${name}`, slices_via: "none" };
  }
  if (relPath.startsWith("ia/projects/")) {
    return { audience: "both", loaded_by: "ondemand", slices_via: "none" };
  }
  if (relPath.startsWith("ia/templates/")) {
    return { audience: "both", loaded_by: "ondemand", slices_via: "none" };
  }
  return null;
}

function quote(value) {
  // YAML-safe single-line string. Escape any double quotes; wrap in double quotes
  // when value contains characters that would otherwise need escaping.
  if (value == null) return '""';
  const str = String(value).replace(/[\r\n]+/g, " ").trim();
  if (/^[A-Za-z0-9 _.,;:?!/()'`{}\-]+$/.test(str) && !/^[-?:&*!|>]/.test(str)) {
    return str;
  }
  return `"${str.replace(/\\/g, "\\\\").replace(/"/g, '\\"')}"`;
}

function deriveTitle(body) {
  const m = body.match(/^# +(.+)$/m);
  if (!m) return null;
  return m[1].trim().replace(/`/g, "");
}

function derivePurpose(relPath, parsed, body) {
  // Prefer existing 'description'; fall back to first H1; fall back to filename.
  if (parsed && parsed.description) {
    let desc = parsed.description.replace(/\s+/g, " ").trim();
    // If a clean sentence ends within 220 chars, use it. Otherwise use first
    // ~220 chars cut at the nearest word boundary.
    const sentenceMatch = desc.match(/^(.{20,220}?[.!])(\s|$)/);
    if (sentenceMatch) return sentenceMatch[1];
    if (desc.length <= 220) return desc;
    const cut = desc.slice(0, 220);
    const lastSpace = cut.lastIndexOf(" ");
    return (lastSpace > 100 ? cut.slice(0, lastSpace) : cut).trim() + "…";
  }
  const title = deriveTitle(body);
  if (title) {
    if (relPath.startsWith("ia/projects/")) return `Project spec for ${title}.`;
    if (relPath.startsWith("ia/specs/"))    return `Reference spec for ${title}.`;
    if (relPath.startsWith("ia/templates/"))return `${title}.`;
    return `${title}.`;
  }
  return path.basename(relPath, ".md");
}

// Minimal YAML frontmatter parser. Handles simple `key: value` and folded
// `key: >` blocks. Sufficient for the existing rule + skill files.
function parseFrontmatter(text) {
  if (!text.startsWith("---")) return { parsed: null, body: text, raw: null, lines: null };
  const lines = text.split("\n");
  let endIdx = -1;
  for (let i = 1; i < lines.length; i++) {
    if (lines[i] === "---") { endIdx = i; break; }
  }
  if (endIdx < 0) return { parsed: null, body: text, raw: null, lines: null };
  const fmLines = lines.slice(1, endIdx);
  const body = lines.slice(endIdx + 1).join("\n");
  const parsed = {};
  let i = 0;
  while (i < fmLines.length) {
    const line = fmLines[i];
    const m = line.match(/^([A-Za-z_][\w-]*):\s*(.*)$/);
    if (!m) { i++; continue; }
    const key = m[1];
    let value = m[2];
    if (value === ">" || value === "|" || value === ">-" || value === "|-") {
      // Folded block — collect indented lines
      const collected = [];
      i++;
      while (i < fmLines.length && /^( +|\t)/.test(fmLines[i])) {
        collected.push(fmLines[i].replace(/^\s+/, ""));
        i++;
      }
      parsed[key] = collected.join(" ").trim();
      continue;
    }
    parsed[key] = value.replace(/^["']|["']$/g, "");
    i++;
  }
  return { parsed, body, raw: lines.slice(0, endIdx + 1).join("\n"), lines: fmLines };
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

function shouldProcess(relPath) {
  if (relPath.startsWith("ia/specs/")) return true;
  if (relPath.startsWith("ia/rules/")) return true;
  if (relPath.startsWith("ia/skills/") && relPath.endsWith("/SKILL.md")) return true;
  if (relPath.startsWith("ia/projects/")) return true;
  if (relPath.startsWith("ia/templates/")) return true;
  return false;
}

async function main() {
  const iaDir = path.join(repoRoot, "ia");
  const targets = [];
  for await (const abs of walk(iaDir)) {
    const rel = path.relative(repoRoot, abs).replace(/\\/g, "/");
    if (shouldProcess(rel)) targets.push({ abs, rel });
  }
  targets.sort((a, b) => a.rel.localeCompare(b.rel));

  let added = 0;
  let updated = 0;
  let skipped = 0;

  for (const { abs, rel } of targets) {
    const text = await fs.readFile(abs, "utf8");
    const { parsed, body, lines: fmLines } = parseFrontmatter(text);

    const overrides = OVERRIDES[rel] || {};
    const defaults = { ...defaultsFor(rel), ...overrides };

    // Compute the four required values, only filling fields that are missing.
    const desired = {
      purpose: derivePurpose(rel, parsed, parsed ? body : text),
      audience: defaults.audience,
      loaded_by: defaults.loaded_by,
      slices_via: defaults.slices_via,
    };

    if (parsed) {
      const force = process.argv.includes("--force-purpose");
      const present = REQUIRED.filter((k) => parsed[k] != null);
      if (present.length === REQUIRED.length && !force) {
        skipped++;
        continue;
      }
      // Insert missing fields at the TOP of the existing frontmatter so the IA
      // header is the first thing readers see. With --force-purpose, drop the
      // existing IA fields so they get re-derived.
      const stripIa = force;
      const cleanFmLines = stripIa
        ? fmLines.filter((l) => !REQUIRED.some((k) => l.startsWith(`${k}:`)))
        : fmLines;
      const newFmLines = [];
      for (const k of REQUIRED) {
        if (force || parsed[k] == null) newFmLines.push(`${k}: ${quote(desired[k])}`);
      }
      const merged = ["---", ...newFmLines, ...cleanFmLines, "---", body].join("\n");
      await fs.writeFile(abs, merged.replace(/\n{3,}/g, "\n\n"));
      updated++;
      continue;
    }

    // No frontmatter at all — prepend a fresh block.
    const fm = [
      "---",
      `purpose: ${quote(desired.purpose)}`,
      `audience: ${quote(desired.audience)}`,
      `loaded_by: ${quote(desired.loaded_by)}`,
      `slices_via: ${quote(desired.slices_via)}`,
      "---",
      "",
    ].join("\n");
    await fs.writeFile(abs, fm + text);
    added++;
  }

  console.log(JSON.stringify({ added, updated, skipped, total: targets.length }, null, 2));
}

main().catch((err) => {
  console.error(err);
  process.exitCode = 1;
});
