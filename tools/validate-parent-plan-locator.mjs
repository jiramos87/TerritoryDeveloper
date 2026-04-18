#!/usr/bin/env node
/**
 * validate-parent-plan-locator.mjs
 *
 * CLI wrapper around the shared `validateParentPlanLocator` core
 * (tools/mcp-ia-server/src/parser/parent-plan-validator.ts, compiled to dist).
 *
 * Why compiled dist, not tsx source?
 *   This script is chained into `validate:all` (hot path). `compute-lib:build`
 *   already runs before the chain position where this is appended, so dist is
 *   always fresh. Sibling `validate:backlog-yaml` uses `tsx` only because its
 *   target has no dist counterpart.
 *
 * Usage:
 *   node tools/validate-parent-plan-locator.mjs            # advisory (default)
 *   node tools/validate-parent-plan-locator.mjs --advisory # explicit advisory
 *   node tools/validate-parent-plan-locator.mjs --strict   # strict (exit 1 on error)
 *
 * Flags --strict and --advisory are mutually exclusive; --strict wins if both
 * are passed (defensive; the help text discourages it).
 */

import { createRequire } from "node:module";
import path from "node:path";
import { fileURLToPath } from "node:url";

// ---------------------------------------------------------------------------
// Resolve repo root from this file's location
// ---------------------------------------------------------------------------

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, "..");

// ---------------------------------------------------------------------------
// Arg parse — --strict / --advisory; default advisory
// ---------------------------------------------------------------------------

const args = process.argv.slice(2);
const strict = args.includes("--strict");
// --advisory explicit opt-out (for future post-flip era; no-op in current advisory default)
// const advisory = args.includes("--advisory");

const yamlDirs = ["ia/backlog", "ia/backlog-archive"];
const planGlob = "ia/projects/*master-plan*.md";

// ---------------------------------------------------------------------------
// Import compiled core (no tsx on hot path)
// ---------------------------------------------------------------------------

const require = createRequire(import.meta.url);
const distPath = path.join(
  repoRoot,
  "tools/mcp-ia-server/dist/parser/parent-plan-validator.js",
);
const { validateParentPlanLocator } = require(distPath);

// ---------------------------------------------------------------------------
// Run validator
// ---------------------------------------------------------------------------

const result = validateParentPlanLocator({ repoRoot, yamlDirs, planGlob, strict });

// ---------------------------------------------------------------------------
// Output formatting + exit (Phase 2)
// ---------------------------------------------------------------------------

if (strict) {
  // Strict mode: print full errors + warnings lists; exit result.exit_code
  for (const e of result.errors) {
    process.stdout.write(`ERROR: ${e}\n`);
  }
  for (const w of result.warnings) {
    process.stdout.write(`WARN: ${w}\n`);
  }
  process.exit(result.exit_code);
} else {
  // Advisory mode: print drift count only when drift exists; always exit 0
  const total = result.errors.length + result.warnings.length;
  if (total > 0) {
    process.stdout.write(
      `parent-plan-locator: ${total} drift item(s) found (advisory; run --strict to fail)\n`,
    );
  }
  process.exit(0);
}
