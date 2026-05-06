#!/usr/bin/env node
/**
 * validate-boundary-markers.mjs — TECH-15905
 *
 * Pre-flip regex gate: validates that every <!-- TASK:{ISSUE_ID} START -->
 * has a matching <!-- TASK:{ISSUE_ID} END --> in Pass A output buffer.
 *
 * Usage:
 *   echo "<pass-a-output>" | node validate-boundary-markers.mjs
 *   node validate-boundary-markers.mjs --file <path>
 *   node validate-boundary-markers.mjs --text "<pass-a-output>"
 *
 * Exit codes:
 *   0 — all markers balanced
 *   1 — malformed or unbalanced markers found (structured error on stderr)
 */

import { readFileSync } from "fs";

// ---------------------------------------------------------------------------
// Regex constants
// ---------------------------------------------------------------------------

// Matches <!-- TASK:ISSUE_ID START --> (well-formed)
const START_RE = /<!--\s*TASK:([\w-]+)\s+START\s*-->/g;
// Matches <!-- TASK:ISSUE_ID END --> (well-formed)
const END_RE = /<!--\s*TASK:([\w-]+)\s+END\s*-->/g;
// Detects partial / malformed marker attempts (missing closing -->)
const MALFORMED_RE = /<!--\s*TASK:[\w-]+\s+(?:START|END)(?!\s*-->)/g;

// ---------------------------------------------------------------------------
// Core validation function (exported for test imports)
// ---------------------------------------------------------------------------

/**
 * Validate boundary markers in a Pass A output buffer.
 * @param {string} text - Pass A output buffer content.
 * @returns {{ ok: boolean, errors: string[], starts: string[], ends: string[] }}
 */
export function validateBoundaryMarkers(text) {
  const errors = [];

  // 1. Detect malformed markers (missing closing -->).
  const malformed = [...text.matchAll(MALFORMED_RE)];
  for (const m of malformed) {
    errors.push(`malformed_marker: "${m[0].slice(0, 60)}..." (missing closing -->)`);
  }

  // 2. Collect well-formed START and END markers.
  const starts = [...text.matchAll(START_RE)].map((m) => m[1]);
  const ends = [...text.matchAll(END_RE)].map((m) => m[1]);

  // 3. Check balance: every START must have exactly one END, in order.
  const startSet = new Map();
  for (const id of starts) {
    startSet.set(id, (startSet.get(id) ?? 0) + 1);
  }
  const endSet = new Map();
  for (const id of ends) {
    endSet.set(id, (endSet.get(id) ?? 0) + 1);
  }

  // Missing END for a START.
  for (const [id, count] of startSet.entries()) {
    const endCount = endSet.get(id) ?? 0;
    if (endCount < count) {
      errors.push(`unbalanced_marker: TASK:${id} has ${count} START(s) but ${endCount} END(s)`);
    }
    if (endCount > count) {
      errors.push(`unbalanced_marker: TASK:${id} has ${count} START(s) but ${endCount} END(s) (extra END)`);
    }
  }

  // END without START.
  for (const [id] of endSet.entries()) {
    if (!startSet.has(id)) {
      errors.push(`orphan_end_marker: TASK:${id} END has no matching START`);
    }
  }

  // 4. No markers at all is valid (empty stage or prose-only output).
  return { ok: errors.length === 0, errors, starts, ends };
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
  // stdin fallback
  try {
    return readFileSync("/dev/stdin", "utf8");
  } catch {
    return "";
  }
}

// Guard: only run CLI logic when invoked directly (not imported as module).
// Detects main-module execution via import.meta.url vs process.argv[1].
import { fileURLToPath } from "url";
const isMain = process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1];

if (isMain) {
  const text = getInput();
  const result = validateBoundaryMarkers(text);

  if (!result.ok) {
    process.stderr.write(
      JSON.stringify(
        {
          ok: false,
          error: "boundary_marker_unbalanced",
          details: result.errors,
          starts: result.starts,
          ends: result.ends,
        },
        null,
        2,
      ) + "\n",
    );
    process.exit(1);
  }

  // Green path — print counts to stdout.
  process.stdout.write(
    JSON.stringify(
      {
        ok: true,
        tasks: result.starts.length,
        starts: result.starts,
      },
      null,
      2,
    ) + "\n",
  );
  process.exit(0);
}
