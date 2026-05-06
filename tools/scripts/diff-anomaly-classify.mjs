#!/usr/bin/env node
/**
 * diff-anomaly-classify.mjs — TECH-15906
 *
 * Regex-pack classifier over git diff output. Replaces LLM diff review
 * (~25k tokens) with deterministic anomaly detection.
 *
 * Anomaly kinds:
 *   - debug_log       : Debug.Log / console.log inserts in diff
 *   - meta_delete     : *.meta file deletions
 *   - large_hunk      : single diff hunk > LARGE_HUNK_LINES added lines
 *   - retired_symbol  : known retired symbol references added
 *
 * Usage:
 *   git diff HEAD | node diff-anomaly-classify.mjs
 *   node diff-anomaly-classify.mjs --file <path-to-diff>
 *   node diff-anomaly-classify.mjs --text "<diff-text>"
 *
 * Exit codes:
 *   0 — no anomalies (or anomalies within acceptable threshold)
 *   1 — anomalies detected (structured JSON on stdout)
 *
 * Always prints JSON result to stdout.
 */

import { readFileSync } from "fs";

// ---------------------------------------------------------------------------
// Configuration
// ---------------------------------------------------------------------------

const LARGE_HUNK_LINES = 200; // added lines per hunk threshold

/** Known retired symbols that should not appear in new diff lines. */
const RETIRED_SYMBOLS = [
  "ThemedPanel",
  "LegacyToolbar",
  "OldGridManager",
];

// ---------------------------------------------------------------------------
// Anomaly regex pack
// ---------------------------------------------------------------------------

const PATTERNS = [
  {
    kind: "debug_log",
    // Matches added lines (start with +, not +++) containing Debug.Log or console.log
    re: /^\+(?!\+\+).*(?:Debug\.Log|console\.log)\s*\(/m,
    description: "Debug.Log or console.log insert in diff",
  },
  {
    kind: "meta_delete",
    // Matches deleted .meta file header in git diff
    re: /^---\s+.*\.meta$/m,
    description: "Unity .meta file deletion",
  },
  {
    kind: "retired_symbol",
    // Matches added lines containing any retired symbol
    re: new RegExp(
      `^\\+(?!\\+\\+).*(?:${RETIRED_SYMBOLS.map((s) => s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")).join("|")})`,
      "m",
    ),
    description: "Retired symbol reference added",
  },
];

// ---------------------------------------------------------------------------
// Core classifier (exported for test imports)
// ---------------------------------------------------------------------------

/**
 * Classify anomalies in a git diff string.
 * @param {string} diff - Full git diff text.
 * @returns {{ ok: boolean, anomaly_count: number, anomalies: Array<{kind: string, description: string, match: string}> }}
 */
export function classifyDiffAnomalies(diff) {
  const anomalies = [];

  // Pattern-based anomalies.
  for (const { kind, re, description } of PATTERNS) {
    const matches = [...diff.matchAll(new RegExp(re.source, re.flags.includes("m") ? "gm" : "g"))];
    for (const m of matches) {
      anomalies.push({
        kind,
        description,
        match: m[0].slice(0, 120),
      });
    }
  }

  // Large hunk check — count added lines per hunk.
  const hunks = diff.split(/^@@/m);
  for (const hunk of hunks) {
    const addedLines = (hunk.match(/^\+(?!\+\+)/gm) ?? []).length;
    if (addedLines > LARGE_HUNK_LINES) {
      anomalies.push({
        kind: "large_hunk",
        description: `Hunk adds ${addedLines} lines (threshold: ${LARGE_HUNK_LINES})`,
        match: `+${addedLines} lines`,
      });
    }
  }

  return {
    ok: anomalies.length === 0,
    anomaly_count: anomalies.length,
    anomalies,
  };
}

// ---------------------------------------------------------------------------
// CLI entry point
// ---------------------------------------------------------------------------

function getInput() {
  const args = process.argv.slice(2);
  const fileIdx = args.indexOf("--file");
  const textIdx = args.indexOf("--text");

  if (fileIdx !== -1 && args[fileIdx + 1]) {
    return readFileSync(args[fileIdx + 1], "utf8");
  }
  if (textIdx !== -1 && args[textIdx + 1]) {
    return args[textIdx + 1];
  }
  try {
    return readFileSync("/dev/stdin", "utf8");
  } catch {
    return "";
  }
}

// Guard: only run CLI logic when invoked directly (not imported as module).
import { fileURLToPath } from "url";
const isMain = process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1];

if (isMain) {
  const diff = getInput();
  const result = classifyDiffAnomalies(diff);

  process.stdout.write(JSON.stringify(result, null, 2) + "\n");
  process.exit(result.ok ? 0 : 1);
}
