#!/usr/bin/env node
// plan-applier core — apply §Plan Fix tuples from stage body to filesystem.
//
// Args (argv):
//   --slug <slug>            (informational only)
//   --stage-id <X.Y>         (informational only)
//   --body <markdown>        OR
//   --body-file <path>       Full stage body markdown.
//   --repo-root <path>       Optional override (default: derived from script path).
//
// Stdout (exit 0):
//   plan-applier: applied=N skipped=K mode={apply|pass|missing}
//
// Stderr (exit 1):
//   JSON `{escalation: true, tuple_index, reason, ...}` per plan-apply-pair-contract.
//
// Tuples (YAML list, see ia/rules/plan-apply-pair-contract.md §Plan tuple shape):
//   - operation, target_path, target_anchor, payload
//
// Operations: replace_section / insert_after / insert_before / append_row /
//             delete_section / set_frontmatter / archive_record / delete_file / write_file
// Anchors:    heading text (`## Foo`), line ref (`L42`), `glossary:Term`, `task_key:T1.2.5`.
//
// Idempotent: re-running a fully- or partially-applied §Plan Fix exits 0 zero-diff.

import {
  readFileSync,
  writeFileSync,
  existsSync,
  renameSync,
  unlinkSync,
  mkdirSync,
} from "node:fs";
import { resolve, dirname, isAbsolute, join } from "node:path";
import { fileURLToPath } from "node:url";
import process from "node:process";
import yaml from "js-yaml";

const __dirname = dirname(fileURLToPath(import.meta.url));
const DEFAULT_REPO_ROOT = resolve(__dirname, "..", "..", "..", "..");

// ------------------------------------------------------------------ args ----
const args = parseArgs(process.argv.slice(2));
const repoRoot = args["repo-root"] ?? DEFAULT_REPO_ROOT;
const slug = args.slug ?? "";
const stageId = args["stage-id"] ?? "";
let body = args.body;
if (!body && args["body-file"]) body = readFileSync(args["body-file"], "utf8");
if (!body) fail("missing --body / --body-file");

// ------------------------------------------------------------ extract block ----
const block = extractPlanFixBlock(body);
if (!block) {
  console.log(`plan-applier: applied=0 skipped=0 mode=missing slug=${slug} stage=${stageId}`);
  process.exit(0);
}
if (block.passSentinel) {
  console.log(`plan-applier: applied=0 skipped=0 mode=pass slug=${slug} stage=${stageId}`);
  process.exit(0);
}

// -------------------------------------------------------------- parse tuples ----
let tuples;
try {
  tuples = yaml.load(block.body);
} catch (e) {
  escalate(0, "malformed_yaml", { error: String(e?.message ?? e) });
}
if (!Array.isArray(tuples)) {
  escalate(0, "malformed_tuple", { error: "expected YAML list at §Plan Fix" });
}
if (tuples.length === 0) {
  console.log(`plan-applier: applied=0 skipped=0 mode=empty slug=${slug} stage=${stageId}`);
  process.exit(0);
}

// ----------------------------------------------------------------- apply pass ----
let applied = 0;
let skipped = 0;
for (let i = 0; i < tuples.length; i++) {
  const result = applyTuple(tuples[i], i);
  if (result.escalated) escalate(i, result.reason, result.details ?? {});
  if (result.skipped) skipped++;
  else applied++;
}

console.log(
  `plan-applier: applied=${applied} skipped=${skipped} mode=apply slug=${slug} stage=${stageId} tuples=${tuples.length}`,
);
process.exit(0);

// =============================================================== utilities ====
function parseArgs(argv) {
  const out = {};
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (!a.startsWith("--")) continue;
    const k = a.slice(2);
    const next = argv[i + 1];
    if (next === undefined || next.startsWith("--")) {
      out[k] = "";
    } else {
      out[k] = next;
      i++;
    }
  }
  return out;
}

function fail(msg) {
  console.error(`plan-applier: ${msg}`);
  process.exit(1);
}

function escalate(idx, reason, details) {
  console.error(JSON.stringify({ escalation: true, tuple_index: idx, reason, ...details }));
  process.exit(1);
}

function extractPlanFixBlock(srcBody) {
  const lines = srcBody.split("\n");
  let start = -1;
  let level = 0;
  for (let i = 0; i < lines.length; i++) {
    const m = lines[i].match(/^(#{2,})\s+§Plan\s*Fix\b/);
    if (m) {
      start = i;
      level = m[1].length;
      break;
    }
  }
  if (start === -1) return null;
  let end = lines.length;
  for (let i = start + 1; i < lines.length; i++) {
    const m = lines[i].match(/^(#{1,})\s/);
    if (m && m[1].length <= level) {
      end = i;
      break;
    }
  }
  const inner = lines.slice(start + 1, end).join("\n").trim();
  const passSentinel = /plan-review\s+exit\s+0/i.test(inner);
  let yamlBody = inner;
  const fence = inner.match(/```(?:yaml|yml)?\s*\n([\s\S]*?)\n```/);
  if (fence) yamlBody = fence[1];
  return { body: yamlBody, passSentinel };
}

function resolvePath(p) {
  if (!p) return null;
  return isAbsolute(p) ? p : join(repoRoot, p);
}

function applyTuple(t, idx) {
  if (!t || typeof t !== "object") {
    return { escalated: true, reason: "malformed_tuple", details: { error: "tuple is not an object" } };
  }
  const op = t.operation;
  const tp = t.target_path;
  const ta = t.target_anchor;
  const pl = t.payload;
  if (!op) return { escalated: true, reason: "malformed_tuple", details: { missing_keys: ["operation"] } };

  switch (op) {
    case "replace_section":
      return mustHave(t, ["target_path", "target_anchor"]) ?? replaceSection(tp, ta, pl);
    case "insert_after":
      return mustHave(t, ["target_path", "target_anchor", "payload"]) ?? insertAt(tp, ta, pl, "after");
    case "insert_before":
      return mustHave(t, ["target_path", "target_anchor", "payload"]) ?? insertAt(tp, ta, pl, "before");
    case "append_row":
      return mustHave(t, ["target_path", "target_anchor", "payload"]) ?? appendRow(tp, ta, pl);
    case "delete_section":
      return mustHave(t, ["target_path", "target_anchor"]) ?? deleteSection(tp, ta);
    case "set_frontmatter":
      return mustHave(t, ["target_path", "payload"]) ?? setFrontmatter(tp, pl);
    case "archive_record":
      return archiveRecord(pl);
    case "delete_file":
      return mustHave(t, ["target_path"]) ?? deleteFile(tp);
    case "write_file":
      return mustHave(t, ["target_path", "payload"]) ?? writeFile(tp, pl);
    default:
      return { escalated: true, reason: "unknown_operation", details: { operation: op } };
  }
}

function mustHave(t, keys) {
  const missing = keys.filter((k) => t[k] === undefined || t[k] === null);
  if (missing.length === 0) return null;
  return { escalated: true, reason: "malformed_tuple", details: { missing_keys: missing } };
}

// --------------------------------------------------------------- anchor lookup ----
// Returns: { line: number } | { escalated, reason, details }.
function resolveAnchor(filePath, anchor) {
  const abs = resolvePath(filePath);
  if (!existsSync(abs)) return { escalated: true, reason: "target_path_missing", details: { target_path: filePath } };
  const lines = readFileSync(abs, "utf8").split("\n");

  if (/^L\d+$/i.test(anchor)) {
    const n = Number(anchor.slice(1));
    if (n < 1 || n > lines.length) {
      return { escalated: true, reason: "anchor_not_found", details: { anchor, candidate_matches: [] } };
    }
    return { line: n - 1 };
  }

  if (anchor.startsWith("glossary:")) {
    const term = anchor.slice("glossary:".length).trim();
    const matches = [];
    for (let i = 0; i < lines.length; i++) {
      // glossary row format: `| TermName | ... |` or heading `## TermName`.
      if (lines[i].includes(`| ${term} |`)) matches.push(i);
      if (lines[i].match(new RegExp(`^#{2,}\\s+${escapeRe(term)}\\b`))) matches.push(i);
    }
    return resolveSingle(matches, anchor, lines);
  }

  if (anchor.startsWith("task_key:")) {
    const key = anchor.slice("task_key:".length).trim();
    const matches = [];
    for (let i = 0; i < lines.length; i++) {
      if (lines[i].includes(`| ${key} |`) || lines[i].includes(`**${key}**`)) matches.push(i);
    }
    return resolveSingle(matches, anchor, lines);
  }

  // Default: heading text (anchor starts with `#` or is a literal heading line).
  const matches = [];
  for (let i = 0; i < lines.length; i++) {
    if (lines[i].trim() === anchor.trim()) matches.push(i);
  }
  return resolveSingle(matches, anchor, lines);
}

function resolveSingle(matches, anchor, lines) {
  if (matches.length === 0) {
    return { escalated: true, reason: "anchor_not_found", details: { anchor, candidate_matches: [] } };
  }
  if (matches.length > 1) {
    const samples = matches.slice(0, 5).map((i) => `line ${i + 1}: ${lines[i].slice(0, 120)}`);
    return { escalated: true, reason: "anchor_ambiguous", details: { anchor, candidate_matches: samples } };
  }
  return { line: matches[0] };
}

function escapeRe(s) {
  return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function headingLevel(line) {
  const m = line.match(/^(#{1,6})\s/);
  return m ? m[1].length : 0;
}

// ------------------------------------------------------------------ operations ----
function replaceSection(filePath, anchor, payload) {
  const r = resolveAnchor(filePath, anchor);
  if (r.escalated) return r;
  const abs = resolvePath(filePath);
  const lines = readFileSync(abs, "utf8").split("\n");
  const headIdx = r.line;
  const level = headingLevel(lines[headIdx]);
  if (level === 0) {
    return { escalated: true, reason: "anchor_not_a_heading", details: { anchor, line: headIdx + 1 } };
  }
  let end = lines.length;
  for (let i = headIdx + 1; i < lines.length; i++) {
    const lv = headingLevel(lines[i]);
    if (lv > 0 && lv <= level) { end = i; break; }
  }
  const before = lines.slice(0, headIdx);
  const after = lines.slice(end);
  const newSection = String(payload ?? "").split("\n");
  const newContent = [...before, ...newSection, ...after].join("\n");
  const oldContent = readFileSync(abs, "utf8");
  if (newContent === oldContent) return { skipped: true };
  writeFileSync(abs, newContent);
  return { skipped: false };
}

function insertAt(filePath, anchor, payload, where) {
  const r = resolveAnchor(filePath, anchor);
  if (r.escalated) return r;
  const abs = resolvePath(filePath);
  const lines = readFileSync(abs, "utf8").split("\n");
  const idx = r.line;
  const payloadLines = String(payload ?? "").split("\n");
  // Idempotent: skip if payload already at insertion point.
  const insertIdx = where === "after" ? idx + 1 : idx;
  const window = lines.slice(insertIdx, insertIdx + payloadLines.length).join("\n");
  if (window === payloadLines.join("\n")) return { skipped: true };
  const next = [...lines.slice(0, insertIdx), ...payloadLines, ...lines.slice(insertIdx)].join("\n");
  writeFileSync(abs, next);
  return { skipped: false };
}

function appendRow(filePath, anchor, payload) {
  const r = resolveAnchor(filePath, anchor);
  if (r.escalated) return r;
  const abs = resolvePath(filePath);
  const lines = readFileSync(abs, "utf8").split("\n");
  // Find end of the table starting at anchor (consecutive | rows).
  let i = r.line;
  while (i < lines.length && /^\|/.test(lines[i])) i++;
  const insertIdx = i;
  const rowLine = String(payload ?? "").trim();
  // Idempotent: skip if identical row already in the table.
  for (let j = r.line; j < insertIdx; j++) {
    if (lines[j].trim() === rowLine) return { skipped: true };
  }
  const next = [...lines.slice(0, insertIdx), rowLine, ...lines.slice(insertIdx)].join("\n");
  writeFileSync(abs, next);
  return { skipped: false };
}

function deleteSection(filePath, anchor) {
  const r = resolveAnchor(filePath, anchor);
  if (r.escalated) return r;
  const abs = resolvePath(filePath);
  const lines = readFileSync(abs, "utf8").split("\n");
  const headIdx = r.line;
  const level = headingLevel(lines[headIdx]);
  if (level === 0) {
    return { escalated: true, reason: "anchor_not_a_heading", details: { anchor, line: headIdx + 1 } };
  }
  let end = lines.length;
  for (let i = headIdx + 1; i < lines.length; i++) {
    const lv = headingLevel(lines[i]);
    if (lv > 0 && lv <= level) { end = i; break; }
  }
  const next = [...lines.slice(0, headIdx), ...lines.slice(end)].join("\n");
  const oldContent = readFileSync(abs, "utf8");
  if (next === oldContent) return { skipped: true };
  writeFileSync(abs, next);
  return { skipped: false };
}

function setFrontmatter(filePath, payload) {
  const abs = resolvePath(filePath);
  if (!existsSync(abs)) return { escalated: true, reason: "target_path_missing", details: { target_path: filePath } };
  const content = readFileSync(abs, "utf8");
  const fmMatch = content.match(/^---\n([\s\S]*?)\n---\n/);
  let fm = {};
  let after = content;
  if (fmMatch) {
    try {
      fm = yaml.load(fmMatch[1]) ?? {};
    } catch {
      return { escalated: true, reason: "malformed_frontmatter", details: { target_path: filePath } };
    }
    after = content.slice(fmMatch[0].length);
  }
  const updates = (payload && typeof payload === "object") ? payload : {};
  let changed = false;
  for (const [k, v] of Object.entries(updates)) {
    if (fm[k] !== v) {
      fm[k] = v;
      changed = true;
    }
  }
  if (!changed) return { skipped: true };
  const dumped = yaml.dump(fm, { lineWidth: 120 }).trimEnd();
  const next = `---\n${dumped}\n---\n${after}`;
  writeFileSync(abs, next);
  return { skipped: false };
}

function archiveRecord(payload) {
  const src = payload?.source;
  const dst = payload?.destination;
  if (!src || !dst) {
    return { escalated: true, reason: "malformed_tuple", details: { missing: ["payload.source", "payload.destination"] } };
  }
  const srcAbs = resolvePath(src);
  const dstAbs = resolvePath(dst);
  if (!existsSync(srcAbs) && existsSync(dstAbs)) return { skipped: true };
  if (!existsSync(srcAbs)) {
    return { escalated: true, reason: "target_path_missing", details: { source: src } };
  }
  mkdirSync(dirname(dstAbs), { recursive: true });
  renameSync(srcAbs, dstAbs);
  return { skipped: false };
}

function deleteFile(filePath) {
  const abs = resolvePath(filePath);
  if (!existsSync(abs)) return { skipped: true };
  unlinkSync(abs);
  return { skipped: false };
}

function writeFile(filePath, payload) {
  const abs = resolvePath(filePath);
  const next = String(payload ?? "");
  if (existsSync(abs)) {
    const cur = readFileSync(abs, "utf8");
    if (cur === next) return { skipped: true };
  }
  mkdirSync(dirname(abs), { recursive: true });
  writeFileSync(abs, next);
  return { skipped: false };
}
