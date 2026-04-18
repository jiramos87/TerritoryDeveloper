#!/usr/bin/env node
/**
 * materialize-backlog.mjs
 *
 * Regenerate BACKLOG.md + BACKLOG-ARCHIVE.md from yaml records + section manifests.
 * Called by materialize-backlog.sh.
 *
 * Usage:
 *   node tools/scripts/materialize-backlog.mjs [--check]
 *
 * --check: diff generated vs on-disk (whitespace-normalized); exit 1 on diff.
 *
 * Section manifest format (interleaved):
 *   {
 *     preamble: ["line", ...],
 *     sections: [
 *       {
 *         header: "## Section Name",
 *         items: [
 *           {type: "prose", lines: ["...", ""]},
 *           {type: "issue", id: "TECH-38"},
 *           {type: "prose", lines: [""]},
 *           {type: "issue", id: "TECH-32"},
 *           ...
 *         ]
 *       },
 *       ...
 *     ]
 *   }
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../..");
const CHECK_MODE = process.argv.includes("--check");

// ---------------------------------------------------------------------------
// Simple YAML parser (single-key-per-line, literal block scalars, lists)
// ---------------------------------------------------------------------------

function parseYaml(content) {
  const lines = content.split("\n");
  const obj = {};
  let i = 0;

  while (i < lines.length) {
    const line = lines[i];
    if (!line || line.startsWith("#")) { i++; continue; }

    const colonIdx = line.indexOf(": ");
    if (colonIdx < 0) {
      // Could be "key:\n" (block value follows)
      const bareColon = line.indexOf(":");
      if (bareColon >= 0 && bareColon === line.length - 1) {
        const key = line.slice(0, bareColon).trim();
        i++;
        // Peek next line for block indicator
        if (i < lines.length && (lines[i] === "[]" || lines[i].startsWith("  - "))) {
          // List
          const items = [];
          while (i < lines.length && lines[i].startsWith("  - ")) {
            items.push(unquoteYamlScalar(lines[i].slice(4)));
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
      // Literal block scalar — collect indented lines
      i++;
      const blockLines = [];
      while (i < lines.length && (lines[i].startsWith("  ") || lines[i] === "")) {
        if (lines[i].startsWith("  ")) {
          blockLines.push(lines[i].slice(2));
        } else {
          blockLines.push("");
        }
        i++;
      }
      // Remove trailing blank lines (clip chomping)
      while (blockLines.length > 0 && !blockLines[blockLines.length - 1]) blockLines.pop();
      obj[key] = blockLines.join("\n");
      continue;
    }

    if (rawVal === "[]") {
      obj[key] = [];
    } else if (rawVal.trimStart() === "") {
      // Next lines may be list items
      i++;
      if (i < lines.length && lines[i].trim().startsWith("- ")) {
        const items = [];
        while (i < lines.length && lines[i].trim().startsWith("- ")) {
          items.push(unquoteYamlScalar(lines[i].trim().slice(2)));
          i++;
        }
        obj[key] = items;
      } else {
        obj[key] = "";
      }
      continue;
    } else {
      obj[key] = unquoteYamlScalar(rawVal);
    }

    i++;
  }

  return obj;
}

function unquoteYamlScalar(s) {
  if (!s) return "";
  s = s.trim();
  if ((s.startsWith('"') && s.endsWith('"')) || (s.startsWith("'") && s.endsWith("'"))) {
    return s
      .slice(1, -1)
      .replace(/\\n/g, "\n")
      .replace(/\\"/g, '"')
      .replace(/\\\\/g, "\\");
  }
  return s;
}

// ---------------------------------------------------------------------------
// Load yaml records
// ---------------------------------------------------------------------------

function loadYamlDir(dirPath) {
  if (!fs.existsSync(dirPath)) return [];
  return fs
    .readdirSync(dirPath)
    .filter((f) => f.endsWith(".yaml"))
    .map((f) => {
      const content = fs.readFileSync(path.join(dirPath, f), "utf8");
      return parseYaml(content);
    });
}

const openIssues = loadYamlDir(path.join(REPO_ROOT, "ia/backlog"));
const closedIssues = loadYamlDir(path.join(REPO_ROOT, "ia/backlog-archive"));

// Build id → record maps
const openMap = new Map(openIssues.map((r) => [r.id, r]));
const closedMap = new Map(closedIssues.map((r) => [r.id, r]));
const allIssuesMap = new Map([...closedMap, ...openMap]); // open wins on collision

// ---------------------------------------------------------------------------
// Load section manifests
// ---------------------------------------------------------------------------

function loadManifest(filePath) {
  if (!fs.existsSync(filePath)) {
    return { preamble: [], sections: [] };
  }
  return JSON.parse(fs.readFileSync(filePath, "utf8"));
}

const backlogManifest = loadManifest(
  path.join(REPO_ROOT, "ia/state/backlog-sections.json"),
);
const archiveManifest = loadManifest(
  path.join(REPO_ROOT, "ia/state/backlog-archive-sections.json"),
);

// ---------------------------------------------------------------------------
// Reconstruct md content from interleaved manifest
// ---------------------------------------------------------------------------

function reconstruct(manifest) {
  const lines = [];

  // Preamble
  for (const l of manifest.preamble || []) {
    lines.push(l);
  }

  for (const section of manifest.sections || []) {
    lines.push(section.header);

    for (const item of section.items || []) {
      if (item.type === "prose") {
        for (const l of item.lines || []) {
          lines.push(l);
        }
      } else if (item.type === "issue") {
        const record = allIssuesMap.get(item.id);
        if (!record) {
          console.error(`[materialize] WARNING: no yaml record for ${item.id} — skipping`);
          continue;
        }
        const rawMd = record.raw_markdown || "";
        // Emit raw_markdown lines (strip trailing blank lines at end of block)
        const mdLines = rawMd.split("\n");
        while (mdLines.length > 0 && !mdLines[mdLines.length - 1].trim()) mdLines.pop();
        // For duplicate IDs or mismatched first lines, use the manifest's checklist_line
        if (item.checklist_line && mdLines.length > 0 && mdLines[0] !== item.checklist_line) {
          mdLines[0] = item.checklist_line;
        }
        for (const l of mdLines) {
          lines.push(l);
        }
        // Emit trailing blank lines as recorded in manifest
        const trailingBlanks = item.trailing_blanks || 0;
        for (let b = 0; b < trailingBlanks; b++) {
          lines.push("");
        }
      }
    }
  }

  return lines.join("\n");
}

const generatedBacklog = reconstruct(backlogManifest);
const generatedArchive = reconstruct(archiveManifest);

// ---------------------------------------------------------------------------
// Check or write
// ---------------------------------------------------------------------------

function normalizeWS(s) {
  return s
    .split("\n")
    .map((l) => l.trimEnd())
    .join("\n")
    .replace(/\n{3,}/g, "\n\n")
    .trim();
}

if (CHECK_MODE) {
  const backlogPath = path.join(REPO_ROOT, "BACKLOG.md");
  const archivePath = path.join(REPO_ROOT, "BACKLOG-ARCHIVE.md");

  let ok = true;

  if (fs.existsSync(backlogPath)) {
    const current = fs.readFileSync(backlogPath, "utf8");
    const norm_current = normalizeWS(current);
    const norm_generated = normalizeWS(generatedBacklog);
    if (norm_current !== norm_generated) {
      console.error("[materialize] DIFF: BACKLOG.md does not match generated output");
      // Show first divergence for debugging
      const a = norm_current.split("\n");
      const b = norm_generated.split("\n");
      for (let i = 0; i < Math.max(a.length, b.length); i++) {
        if (a[i] !== b[i]) {
          console.error(`  First diff at line ${i + 1}:`);
          console.error(`  DISK:  ${JSON.stringify(a[i])}`);
          console.error(`  GEN:   ${JSON.stringify(b[i])}`);
          break;
        }
      }
      ok = false;
    } else {
      console.error("[materialize] OK: BACKLOG.md matches generated output");
    }
  }

  if (fs.existsSync(archivePath)) {
    const current = fs.readFileSync(archivePath, "utf8");
    const norm_current = normalizeWS(current);
    const norm_generated = normalizeWS(generatedArchive);
    if (norm_current !== norm_generated) {
      console.error("[materialize] DIFF: BACKLOG-ARCHIVE.md does not match generated output");
      const a = norm_current.split("\n");
      const b = norm_generated.split("\n");
      for (let i = 0; i < Math.max(a.length, b.length); i++) {
        if (a[i] !== b[i]) {
          console.error(`  First diff at line ${i + 1}:`);
          console.error(`  DISK:  ${JSON.stringify(a[i])}`);
          console.error(`  GEN:   ${JSON.stringify(b[i])}`);
          break;
        }
      }
      ok = false;
    } else {
      console.error("[materialize] OK: BACKLOG-ARCHIVE.md matches generated output");
    }
  }

  if (!ok) process.exit(1);
} else {
  fs.writeFileSync(path.join(REPO_ROOT, "BACKLOG.md"), generatedBacklog, "utf8");
  fs.writeFileSync(
    path.join(REPO_ROOT, "BACKLOG-ARCHIVE.md"),
    generatedArchive,
    "utf8",
  );
  console.error("[materialize] Written: BACKLOG.md + BACKLOG-ARCHIVE.md");
}
