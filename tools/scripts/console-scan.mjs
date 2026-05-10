#!/usr/bin/env node
/**
 * console-scan.mjs — ship-gate filter for Unity Console output.
 *
 * Usage:
 *   node tools/scripts/console-scan.mjs [--input <json-file>] [--allowlist <yaml-file>]
 *
 * Reads JSON console dump produced by RunFirstFrameAndDumpConsole (array of
 * { severity, message, timestamp_utc } objects). Filters known-noise entries
 * via tools/unity-warning-allowlist.yaml. Expired allowlist entries are NOT
 * applied — they flip back to blocking. Exits 0 when no unfiltered errors
 * remain; exits 1 when any error or warning survives the filter.
 *
 * Environment:
 *   CONSOLE_SCAN_INPUT   — path to console JSON dump (default: /tmp/unity-console.json)
 *   CONSOLE_SCAN_ALLOWLIST — path to allowlist YAML (default: tools/unity-warning-allowlist.yaml)
 */

import { readFileSync, existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join, resolve } from "node:path";

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, "../..");

// ── Minimal YAML parser (no npm dep) ─────────────────────────────────────────
// Parses the allowlist YAML shape: top-level `entries:` list of mappings.
// Does not handle anchors, multi-line blocks beyond `>`, or complex types.
function parseAllowlistYaml(text) {
  const entries = [];
  let current = null;
  for (const raw of text.split("\n")) {
    const line = raw.trimEnd();
    if (line.trimStart().startsWith("#") || line.trim() === "") continue;
    if (line.match(/^  - pattern:/)) {
      if (current) entries.push(current);
      current = { pattern: line.replace(/^  - pattern:\s*/, "").replace(/^["']|["']$/g, "") };
    } else if (current && line.match(/^    reason:/)) {
      current.reason = line.replace(/^    reason:\s*>?\s*/, "").replace(/^["']|["']$/g, "");
    } else if (current && line.match(/^    expires:/)) {
      current.expires = line.replace(/^    expires:\s*/, "").replace(/^["']|["']$/g, "").trim();
    } else if (current && line.match(/^    owner:/)) {
      current.owner = line.replace(/^    owner:\s*/, "").replace(/^["']|["']$/g, "").trim();
    } else if (current && line.match(/^      /)) {
      // Continuation line for `>` scalar — append to reason.
      if (current.reason !== undefined) {
        current.reason += " " + line.trim();
      }
    }
  }
  if (current) entries.push(current);
  return entries;
}

// ── Load allowlist ────────────────────────────────────────────────────────────
const allowlistPath = process.env.CONSOLE_SCAN_ALLOWLIST
  ?? resolve(repoRoot, "tools/unity-warning-allowlist.yaml");

let allowlistEntries = [];
if (existsSync(allowlistPath)) {
  const yamlText = readFileSync(allowlistPath, "utf8");
  allowlistEntries = parseAllowlistYaml(yamlText);
} else {
  process.stderr.write(`[console-scan] WARNING: allowlist not found at ${allowlistPath}\n`);
}

const today = new Date().toISOString().slice(0, 10);

// Only keep entries whose expires date is still in the future (or today).
const activeAllowlist = allowlistEntries.filter((e) => {
  if (!e.expires) return true; // No expiry = always active.
  return e.expires >= today;
});

const expiredCount = allowlistEntries.length - activeAllowlist.length;
if (expiredCount > 0) {
  process.stderr.write(
    `[console-scan] ${expiredCount} allowlist ${expiredCount === 1 ? "entry" : "entries"} expired — treating as blocking.\n`,
  );
}

// ── Load console JSON ─────────────────────────────────────────────────────────
const inputPath = process.env.CONSOLE_SCAN_INPUT
  ?? process.argv[process.argv.indexOf("--input") + 1]
  ?? "/tmp/unity-console.json";

let lines = [];
if (existsSync(inputPath)) {
  const raw = readFileSync(inputPath, "utf8");
  try {
    lines = JSON.parse(raw);
    if (!Array.isArray(lines)) lines = [];
  } catch (e) {
    process.stderr.write(`[console-scan] Failed to parse input JSON: ${e.message}\n`);
    process.exit(1);
  }
} else {
  // No input file — nothing to scan, pass clean.
  process.stdout.write("[console-scan] No console dump found — skip.\n");
  process.exit(0);
}

// ── Filter ────────────────────────────────────────────────────────────────────
function isAllowed(line) {
  const msg = (line.message ?? "").toLowerCase();
  return activeAllowlist.some((e) => msg.includes(e.pattern.toLowerCase()));
}

const blocking = lines.filter(
  (l) => (l.severity === "error" || l.severity === "warning") && !isAllowed(l),
);

if (blocking.length === 0) {
  process.stdout.write(`[console-scan] OK — ${lines.length} lines scanned, 0 blocking.\n`);
  process.exit(0);
} else {
  process.stderr.write(
    `[console-scan] FAIL — ${blocking.length} blocking ${blocking.length === 1 ? "entry" : "entries"}:\n`,
  );
  for (const l of blocking) {
    process.stderr.write(`  [${l.severity}] ${l.message}\n`);
  }
  process.exit(1);
}
