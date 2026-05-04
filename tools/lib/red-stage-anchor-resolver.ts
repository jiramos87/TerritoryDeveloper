/**
 * red-stage-anchor-resolver.ts
 *
 * TECH-10897 — parse 4 grammar forms of `red_test_anchor` and validate
 * cited path resolves on disk for non-`n/a` forms.
 *
 * Grammar forms:
 *   tracer-verb-test:{path}::{method}
 *   visibility-delta-test:{path}::{method}
 *   BUG-NNNN:{path}::{method}
 *   n/a
 */

import * as fs from "node:fs";
import * as path from "node:path";
import { fileURLToPath } from "node:url";

const _dirname =
  typeof __dirname !== "undefined"
    ? __dirname
    : path.dirname(fileURLToPath(import.meta.url));

const REPO_ROOT =
  process.env.IA_REPO_ROOT ?? path.resolve(_dirname, "../..");

// ---------------------------------------------------------------------------
// Error class
// ---------------------------------------------------------------------------

export class RedStageAnchorParseError extends Error {
  override name = "RedStageAnchorParseError";
  cause?: string;

  constructor(message: string, cause?: string) {
    super(message);
    this.cause = cause;
  }
}

// ---------------------------------------------------------------------------
// Discriminated union
// ---------------------------------------------------------------------------

export type RedStageAnchor =
  | { kind: "tracer-verb-test"; path: string; method: string }
  | { kind: "visibility-delta-test"; path: string; method: string }
  | { kind: "bug-repro"; bug_id: string; path: string; method: string }
  | { kind: "na" };

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

const BUG_ID_REGEX = /^BUG-\d+$/;

function splitPathMethod(rest: string, grammar: string): [string, string] {
  const sep = rest.indexOf("::");
  if (sep === -1) {
    throw new RedStageAnchorParseError(
      `Malformed anchor — missing "::" separator. Expected grammar: ${grammar}`,
      grammar,
    );
  }
  const anchorPath = rest.slice(0, sep);
  const method = rest.slice(sep + 2);
  if (!anchorPath) {
    throw new RedStageAnchorParseError(
      `Malformed anchor — empty path. Expected grammar: ${grammar}`,
      grammar,
    );
  }
  if (!method) {
    throw new RedStageAnchorParseError(
      `Malformed anchor — empty method. Expected grammar: ${grammar}`,
      grammar,
    );
  }
  return [anchorPath, method];
}

function validatePathExists(anchorPath: string, grammar: string): void {
  const abs = path.isAbsolute(anchorPath)
    ? anchorPath
    : path.join(REPO_ROOT, anchorPath);
  if (!fs.existsSync(abs)) {
    throw new RedStageAnchorParseError(
      `Anchor path not found on disk: "${anchorPath}". Expected grammar: ${grammar}`,
      grammar,
    );
  }
}

// ---------------------------------------------------------------------------
// Main export
// ---------------------------------------------------------------------------

export function resolveAnchor(anchor: string): RedStageAnchor {
  if (anchor === "n/a") {
    return { kind: "na" };
  }

  const colonIdx = anchor.indexOf(":");
  if (colonIdx === -1) {
    throw new RedStageAnchorParseError(
      `Malformed anchor — unrecognized grammar form: "${anchor}". ` +
        `Expected one of: tracer-verb-test:{path}::{method} | visibility-delta-test:{path}::{method} | BUG-NNNN:{path}::{method} | n/a`,
    );
  }

  const prefix = anchor.slice(0, colonIdx);
  const rest = anchor.slice(colonIdx + 1);

  if (prefix === "tracer-verb-test") {
    const grammar = "tracer-verb-test:{path}::{method}";
    const [anchorPath, method] = splitPathMethod(rest, grammar);
    validatePathExists(anchorPath, grammar);
    return { kind: "tracer-verb-test", path: anchorPath, method };
  }

  if (prefix === "visibility-delta-test") {
    const grammar = "visibility-delta-test:{path}::{method}";
    const [anchorPath, method] = splitPathMethod(rest, grammar);
    validatePathExists(anchorPath, grammar);
    return { kind: "visibility-delta-test", path: anchorPath, method };
  }

  if (BUG_ID_REGEX.test(prefix)) {
    const grammar = "BUG-NNNN:{path}::{method}";
    const [anchorPath, method] = splitPathMethod(rest, grammar);
    validatePathExists(anchorPath, grammar);
    return { kind: "bug-repro", bug_id: prefix, path: anchorPath, method };
  }

  // Bug-id with wrong format (e.g. BUG-abc)
  if (prefix.startsWith("BUG-")) {
    throw new RedStageAnchorParseError(
      `Malformed bug id "${prefix}" — expected pattern BUG-\\d+. Expected grammar: BUG-NNNN:{path}::{method}`,
      "BUG-NNNN:{path}::{method}",
    );
  }

  throw new RedStageAnchorParseError(
    `Unrecognized anchor prefix "${prefix}". ` +
      `Expected one of: tracer-verb-test | visibility-delta-test | BUG-NNNN | n/a`,
  );
}
