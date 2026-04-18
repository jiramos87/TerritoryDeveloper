#!/usr/bin/env node
/**
 * migrate-backlog-to-yaml.mjs
 *
 * One-shot migration: parse BACKLOG.md + BACKLOG-ARCHIVE.md using the
 * existing TypeScript parser (loaded via tsx), then emit one YAML file
 * per issue under:
 *   ia/backlog/{id}.yaml          (open issues)
 *   ia/backlog-archive/{id}.yaml  (completed issues)
 *
 * Also writes:
 *   ia/state/id-counter.json                  — per-prefix max ids
 *   ia/state/backlog-sections.json            — section structure for BACKLOG.md
 *   ia/state/backlog-archive-sections.json    — section structure for BACKLOG-ARCHIVE.md
 *
 * Section structure uses interleaved items to preserve exact round-trip fidelity:
 *   {type: "header", line: "## Compute-lib program"}
 *   {type: "prose", lines: ["...", "..."]}
 *   {type: "issue", id: "TECH-38"}
 *   {type: "prose", lines: [""]}
 *   {type: "issue", id: "TECH-32"}
 *   ...
 *
 * Usage:
 *   node tools/scripts/migrate-backlog-to-yaml.mjs [--dry-run]
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { execSync } from "node:child_process";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "../..");
const DRY_RUN = process.argv.includes("--dry-run");

// ---------------------------------------------------------------------------
// Load all parsed issues from TypeScript parser via tsx
// ---------------------------------------------------------------------------
const PARSER_PATH = path.join(
  REPO_ROOT,
  "tools/mcp-ia-server/src/parser/backlog-parser.ts",
);

const loaderScript = `
import { parseAllBacklogIssues } from ${JSON.stringify(PARSER_PATH)};
const issues = parseAllBacklogIssues(${JSON.stringify(REPO_ROOT)}, "all");
process.stdout.write(JSON.stringify(issues));
`;

const tmpLoader = path.join(REPO_ROOT, "tools/scripts/.tmp-backlog-loader.mts");
fs.writeFileSync(tmpLoader, loaderScript, "utf8");

let parsedIssues;
try {
  const json = execSync(`npx tsx ${JSON.stringify(tmpLoader)}`, {
    cwd: path.join(REPO_ROOT, "tools/mcp-ia-server"),
    encoding: "utf8",
    maxBuffer: 32 * 1024 * 1024,
  });
  parsedIssues = JSON.parse(json);
} finally {
  fs.rmSync(tmpLoader, { force: true });
}

console.error(
  `[migrate] Parsed ${parsedIssues.length} issues from BACKLOG.md + BACKLOG-ARCHIVE.md`,
);

// Build lookup: issue_id → parsed issue
const issuesByRaw = new Map();
for (const iss of parsedIssues) {
  issuesByRaw.set(iss.issue_id, iss);
}

// ---------------------------------------------------------------------------
// Parse file into interleaved sequence for round-trip reconstruction
// ---------------------------------------------------------------------------

const CHECKLIST_HEADER_RE = /^(\s*)-\s+\[([ x])\]\s+\*\*([A-Z]+-\d+[a-z]*)\*\*\s*(.*)$/;
const TOP_SECTION_RE = /^## (.+)$/; // Only ## (not ###)

/**
 * Parse a backlog md file into a sequence of items:
 * - {type: "preamble", lines: [...]} — content before first ## section
 * - {type: "section", header: "...", items: [...]} — each ## section contains
 *     interleaved {type:"prose", lines:[...]} and {type:"issue", id:"..."}
 *
 * Issue blocks are identified by their checklist header; content is
 * attributed to the PREVIOUS item if it's an issue (sub-lines) or
 * to the section's running prose block.
 */
function parseFileStructure(filePath) {
  if (!fs.existsSync(filePath)) {
    return { preamble: [], sections: [] };
  }
  const raw = fs.readFileSync(filePath, "utf8");
  const lines = raw.split("\n");

  const preamble = [];
  const sections = [];
  let currentSection = null;
  let currentProseLines = null;
  let inPreamble = true;
  let inIssueBlock = false;
  // Track trailing blank lines accumulated after current issue's sub-lines
  let pendingBlanks = 0;

  function flushProse() {
    if (currentProseLines && currentProseLines.length > 0) {
      currentSection.items.push({ type: "prose", lines: currentProseLines });
      currentProseLines = null;
    }
  }

  function startProse() {
    if (!currentProseLines) {
      currentProseLines = [];
    }
  }

  function lastIssueItem() {
    if (!currentSection) return null;
    for (let i = currentSection.items.length - 1; i >= 0; i--) {
      if (currentSection.items[i].type === "issue") return currentSection.items[i];
    }
    return null;
  }

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];

    // Top-level section header
    if (TOP_SECTION_RE.test(line)) {
      if (inPreamble) {
        inPreamble = false;
      } else if (currentSection) {
        // Flush any pending blanks onto last issue
        if (inIssueBlock && pendingBlanks > 0) {
          const last = lastIssueItem();
          if (last) last.trailing_blanks = pendingBlanks;
        }
        flushProse();
      }

      if (currentSection) {
        sections.push(currentSection);
      }

      currentSection = {
        header: line,
        items: [],
      };
      currentProseLines = null;
      inIssueBlock = false;
      pendingBlanks = 0;
      continue;
    }

    // Preamble content (before first ## section)
    if (inPreamble) {
      preamble.push(line);
      continue;
    }

    if (!currentSection) {
      preamble.push(line);
      continue;
    }

    // Checklist item (issue header)
    const cm = line.match(CHECKLIST_HEADER_RE);
    if (cm) {
      if (inIssueBlock && pendingBlanks > 0) {
        // Store trailing blanks on the previous issue item
        const last = lastIssueItem();
        if (last) last.trailing_blanks = pendingBlanks;
        pendingBlanks = 0;
      }
      // Flush any current prose before this issue
      flushProse();
      // Push issue reference (store checklist line for exact reconstruction of duplicates / header text)
      currentSection.items.push({ type: "issue", id: cm[3], checklist_line: line, trailing_blanks: 0 });
      inIssueBlock = true;
      continue;
    }

    // Inside an issue block: sub-lines (indented) or blank lines
    if (inIssueBlock) {
      if (line === "") {
        // Blank line — could be inter-issue or trailing; count it
        pendingBlanks++;
        continue;
      }
      if (line.startsWith("  ") || line.startsWith("\t")) {
        // Indented sub-line: reset pending blanks (they were inside the issue)
        pendingBlanks = 0;
        // Skip — reconstructed from yaml
        continue;
      }
      // Non-indented non-empty line: end of issue block
      // Store pending blanks on last issue
      const last = lastIssueItem();
      if (last) last.trailing_blanks = pendingBlanks;
      pendingBlanks = 0;
      inIssueBlock = false;
    }

    // Prose line (between/around issues or section header prose)
    startProse();
    currentProseLines.push(line);
  }

  // Flush last section
  if (currentSection) {
    if (inIssueBlock && pendingBlanks > 0) {
      const last = lastIssueItem();
      if (last) last.trailing_blanks = pendingBlanks;
    }
    flushProse();
    sections.push(currentSection);
  }

  return { preamble, sections };
}

// ---------------------------------------------------------------------------
// YAML serializer
// ---------------------------------------------------------------------------

function yamlLiteralBlock(value, indent = "  ") {
  if (!value || !value.trim()) return '""';
  const lines = value.split("\n");
  while (lines.length > 0 && !lines[lines.length - 1].trim()) lines.pop();
  return "|\n" + lines.map((l) => indent + l).join("\n");
}

function yamlScalar(value) {
  if (value === null || value === undefined) return '""';
  const s = String(value);
  if (
    !s ||
    /^[:\[\]{},#&*!|>'"%@`]/.test(s) ||
    s.includes(": ") ||
    s.includes("\n") ||
    s.includes('"') ||
    s === "true" ||
    s === "false" ||
    s === "null"
  ) {
    return '"' + s.replace(/\\/g, "\\\\").replace(/"/g, '\\"').replace(/\n/g, "\\n") + '"';
  }
  return s;
}

function yamlList(items, indent = "  ") {
  if (!items || items.length === 0) return "[]";
  return "\n" + items.map((item) => `${indent}- ${yamlScalar(item)}`).join("\n");
}

function parseFilesField(filesStr) {
  if (!filesStr || !filesStr.trim()) return [];
  const paths = [];
  const backtickRe = /`([^`]+)`/g;
  let m;
  while ((m = backtickRe.exec(filesStr)) !== null) {
    const p = m[1].trim();
    if (p && p !== "…") paths.push(p);
  }
  return [...new Set(paths)];
}

function parseDependsOn(dependsStr) {
  if (!dependsStr || !dependsStr.trim() || dependsStr.trim() === "none") return [];
  const ids = [];
  const re = /\b(BUG|FEAT|TECH|ART|AUDIO)-(\d+)([a-z]?)\b/gi;
  let m;
  while ((m = re.exec(dependsStr)) !== null) {
    ids.push(`${m[1].toUpperCase()}-${m[2]}${m[3] ? m[3].toLowerCase() : ""}`);
  }
  return [...new Set(ids)];
}

function normalizePriority(section) {
  if (!section) return "medium";
  const s = section.toLowerCase();
  if (s.includes("high")) return "high";
  if (s.includes("medium")) return "medium";
  if (s.includes("low")) return "low";
  if (s.includes("completed")) return "closed";
  return "medium";
}

/**
 * Trim raw_markdown: stop at any ## top-level section header line.
 * Also trim trailing blank lines.
 */
function trimRawMarkdown(raw) {
  if (!raw) return "";
  const lines = raw.split("\n");
  const out = [];
  // First line is always the checklist marker (- [ ] or - [x])
  // Subsequent lines: include only blank lines or lines starting with whitespace (indented sub-lines)
  // Stop at any non-blank, non-indented line (inter-issue prose, HTML comments, section headers, etc.)
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    if (i === 0) {
      // Checklist header line — always include
      out.push(line);
      continue;
    }
    // Blank line: tentatively include (may be between sub-lines)
    if (line === "") {
      // Look ahead: if next non-empty line is indented, include this blank
      let nextIndented = false;
      for (let j = i + 1; j < lines.length; j++) {
        if (lines[j] === "") continue;
        if (lines[j].startsWith("  ") || lines[j].startsWith("\t")) {
          nextIndented = true;
        }
        break;
      }
      if (nextIndented) {
        out.push(line);
      } else {
        // Trailing blank — stop here
        break;
      }
      continue;
    }
    // Non-blank: must be indented to be part of this issue block
    if (line.startsWith("  ") || line.startsWith("\t")) {
      out.push(line);
    } else {
      // Non-indented non-blank line = inter-issue prose / next issue / section header — stop
      break;
    }
  }
  // Strip trailing blank lines
  while (out.length > 0 && !out[out.length - 1].trim()) out.pop();
  return out.join("\n");
}

function buildYaml(issue) {
  const files = parseFilesField(issue.files ?? "");
  const dependsOn = parseDependsOn(issue.depends_on ?? "");
  const priority = normalizePriority(issue.backlog_section);
  const typeStr = issue.type ?? "unknown";
  const specVal = issue.spec ? issue.spec.replace(/^`|`$/g, "").trim() : "";
  const createdDate = new Date().toISOString().slice(0, 10);

  // depends_on_raw = original prose string (preserves soft markers for resolveDependsOnStatus)
  const dependsOnRaw = (issue.depends_on ?? "").trim();

  const lines = [
    `id: ${issue.issue_id}`,
    `type: ${yamlScalar(typeStr)}`,
    `title: ${yamlScalar(issue.title)}`,
    `priority: ${priority}`,
    `status: ${issue.status === "completed" ? "closed" : "open"}`,
    `section: ${yamlScalar(issue.backlog_section)}`,
    `spec: ${specVal ? yamlScalar(specVal) : '""'}`,
    `files: ${yamlList(files)}`,
    `notes: ${issue.notes ? yamlLiteralBlock(issue.notes) : '""'}`,
    `acceptance: ${issue.acceptance ? yamlLiteralBlock(issue.acceptance) : '""'}`,
    `depends_on: ${yamlList(dependsOn)}`,
    `depends_on_raw: ${dependsOnRaw ? yamlScalar(dependsOnRaw) : '""'}`,
    `related: []`,
    `created: ${createdDate}`,
    `raw_markdown: ${yamlLiteralBlock(trimRawMarkdown(issue.raw_markdown || ""))}`,
  ];

  return lines.join("\n") + "\n";
}

// ---------------------------------------------------------------------------
// Counter accumulator
// ---------------------------------------------------------------------------
const PREFIXES = ["TECH", "FEAT", "BUG", "ART", "AUDIO"];
const counter = {};
for (const p of PREFIXES) counter[p] = 0;

function updateCounter(issueId) {
  const m = issueId.match(/^([A-Z]+)-(\d+)/);
  if (!m) return;
  const prefix = m[1];
  const num = parseInt(m[2], 10);
  if (counter[prefix] !== undefined && num > counter[prefix]) {
    counter[prefix] = num;
  }
}

// ---------------------------------------------------------------------------
// Process both files
// ---------------------------------------------------------------------------

const BACKLOG_PATH = path.join(REPO_ROOT, "BACKLOG.md");
const ARCHIVE_PATH = path.join(REPO_ROOT, "BACKLOG-ARCHIVE.md");

const backlogStructure = parseFileStructure(BACKLOG_PATH);
const archiveStructure = parseFileStructure(ARCHIVE_PATH);

// Emit YAML files
let openCount = 0;
let closedCount = 0;

for (const issue of parsedIssues) {
  updateCounter(issue.issue_id);
  const yaml = buildYaml(issue);
  const isClosed = issue.status === "completed";
  const dir = isClosed
    ? path.join(REPO_ROOT, "ia/backlog-archive")
    : path.join(REPO_ROOT, "ia/backlog");
  const outPath = path.join(dir, `${issue.issue_id}.yaml`);

  if (DRY_RUN) {
    console.log(`[dry-run] Would write: ${path.relative(REPO_ROOT, outPath)}`);
  } else {
    fs.writeFileSync(outPath, yaml, "utf8");
  }

  if (isClosed) closedCount++;
  else openCount++;
}

// Write section manifests
if (!DRY_RUN) {
  fs.writeFileSync(
    path.join(REPO_ROOT, "ia/state/id-counter.json"),
    JSON.stringify(counter, null, 2) + "\n",
    "utf8",
  );
  fs.writeFileSync(
    path.join(REPO_ROOT, "ia/state/backlog-sections.json"),
    JSON.stringify(backlogStructure, null, 2) + "\n",
    "utf8",
  );
  fs.writeFileSync(
    path.join(REPO_ROOT, "ia/state/backlog-archive-sections.json"),
    JSON.stringify(archiveStructure, null, 2) + "\n",
    "utf8",
  );
}

console.error(
  `[migrate] Done: ${openCount} open → ia/backlog/, ${closedCount} closed → ia/backlog-archive/`,
);
console.error(`[migrate] Counter seeded: ${JSON.stringify(counter)}`);
if (DRY_RUN) console.error("[migrate] DRY RUN — no files written");
