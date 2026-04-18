/**
 * parent-plan-validator.ts
 *
 * Pure shared validator core for `parent_plan_validate` MCP tool + CLI
 * `validate-parent-plan-locator.mjs`. No process exit, no console I/O,
 * no file writes. Consumer handles output + exit code.
 *
 * Implements 4 cross-file invariant checks per issue record:
 *   (a) parent_plan path resolves on disk
 *   (b) task_key matches ^T\d+\.\d+(\.\d+)?$
 *   (c) task_key present as first-column row in the plan
 *   (d) that same plan row back-references the yaml id via **{ID}**
 *
 * Plan rows are indexed once per plan file (memoized within a single call)
 * to keep the check O(plans × 1) rather than O(plans × issues).
 */

import fs from "node:fs";
import path from "node:path";
import { loadAllYamlIssues } from "./backlog-yaml-loader.js";

// ---------------------------------------------------------------------------
// Public types
// ---------------------------------------------------------------------------

export interface ValidateArgs {
  /** Absolute path to the repo root. Caller is responsible for resolving. */
  repoRoot: string;
  /** Relative dirs to load yaml from (e.g. ["ia/backlog", "ia/backlog-archive"]). */
  yamlDirs: string[];
  /**
   * Glob-style pattern matched against plan basenames only (not full paths).
   * Supports the literal `*` wildcard. Plans are discovered by scanning every
   * `ia/projects/` directory for .md files whose basename matches the pattern.
   * Example: `"ia/projects/*master-plan*.md"`
   */
  planGlob: string;
  /** When true, warnings are promoted to errors and exit_code reflects them. */
  strict: boolean;
}

export interface ValidateResult {
  errors: string[];
  warnings: string[];
  exit_code: 0 | 1;
}

// ---------------------------------------------------------------------------
// Internal types
// ---------------------------------------------------------------------------

interface PlanRow {
  line: number;    // 1-based line number in the plan file
  issueRef: string; // e.g. "TECH-123" extracted from **TECH-123**
}

type PlanIndex = Map<string, PlanRow>; // taskKey → PlanRow

// ---------------------------------------------------------------------------
// Regex constants
// ---------------------------------------------------------------------------

const TASK_KEY_RE = /^T\d+\.\d+(\.\d+)?$/;

/**
 * Matches the first pipe-delimited column of a plan table row.
 * Captures the task key from the first column.
 * Example: `| T3.3.1 | Author shared validator core | ...`
 */
const PLAN_ROW_RE = /^\|\s*(T\d+\.\d+(?:\.\d+)?)\s*\|/;

/**
 * Matches `**ISSUE_ID**` in a plan row (e.g. `**TECH-123**`).
 */
const BACK_REF_RE = /\*\*([A-Z]+-\d+[a-z]*)\*\*/g;

// ---------------------------------------------------------------------------
// Plan file discovery
// ---------------------------------------------------------------------------

/**
 * Convert a glob-style pattern (supporting `*` wildcard) to a RegExp
 * that matches against a file basename only.
 * Only the basename portion of planGlob is used for matching.
 */
function planGlobToRegex(planGlob: string): RegExp {
  // Take the basename of the glob pattern
  const basePart = path.basename(planGlob);
  const escaped = basePart
    .replace(/[.+^${}()|[\]\\]/g, "\\$&")
    .replace(/\*/g, ".*");
  return new RegExp(`^${escaped}$`);
}

/**
 * Discover plan files: walk the directory portion of planGlob under repoRoot,
 * filter by basename regex. Returns absolute paths sorted for determinism.
 */
function discoverPlanFiles(repoRoot: string, planGlob: string): string[] {
  // Derive the directory to scan from the planGlob prefix (up to last '/')
  const slashIdx = planGlob.lastIndexOf("/");
  const dirPart = slashIdx >= 0 ? planGlob.slice(0, slashIdx) : ".";
  const absDir = path.join(repoRoot, dirPart);

  if (!fs.existsSync(absDir)) return [];

  const re = planGlobToRegex(planGlob);
  return fs
    .readdirSync(absDir)
    .filter((f) => re.test(f) && f.endsWith(".md"))
    .sort()
    .map((f) => path.join(absDir, f));
}

// ---------------------------------------------------------------------------
// Plan indexing (memoized per call via planCache passed through)
// ---------------------------------------------------------------------------

/**
 * Parse a plan file once and return a Map from task_key → { line, issueRef }.
 * issueRef is the first **ID** found on that row (the Issue column).
 */
function indexPlanFile(planPath: string): PlanIndex {
  const index: PlanIndex = new Map();
  if (!fs.existsSync(planPath)) return index;

  const lines = fs.readFileSync(planPath, "utf8").split("\n");
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;
    const rowMatch = PLAN_ROW_RE.exec(line);
    if (!rowMatch) continue;

    const taskKey = rowMatch[1]!;
    // Extract first **ID** back-ref from the row
    BACK_REF_RE.lastIndex = 0;
    const refMatch = BACK_REF_RE.exec(line);
    const issueRef = refMatch ? refMatch[1]! : "";

    // First occurrence wins (duplicate task_key rows in a malformed plan)
    if (!index.has(taskKey)) {
      index.set(taskKey, { line: i + 1, issueRef });
    }
  }
  return index;
}

// ---------------------------------------------------------------------------
// Main export
// ---------------------------------------------------------------------------

export function validateParentPlanLocator(args: ValidateArgs): ValidateResult {
  const { repoRoot, yamlDirs: _yamlDirs, planGlob, strict } = args;

  const errors: string[] = [];
  const warnings: string[] = [];

  // Load all yaml records (schema-v2-aware; tolerates empty dirs)
  const { records } = loadAllYamlIssues(repoRoot, "all");

  // Discover and index all plan files once
  const planFiles = discoverPlanFiles(repoRoot, planGlob);
  const planCache = new Map<string, PlanIndex>();
  for (const pf of planFiles) {
    planCache.set(pf, indexPlanFile(pf));
  }

  for (const issue of records) {
    const id = issue.issue_id;

    // -----------------------------------------------------------------------
    // (a) parent_plan field presence + path resolution
    // -----------------------------------------------------------------------
    if (!issue.parent_plan) {
      // Missing parent_plan is advisory — warn, not error
      warnings.push(`${id}: missing parent_plan`);
      continue; // remaining checks require parent_plan
    }

    const planAbsPath = path.join(repoRoot, issue.parent_plan);
    if (!fs.existsSync(planAbsPath)) {
      errors.push(`${id}: parent_plan not found: ${issue.parent_plan}`);
      continue; // can't check (c)/(d) if file missing
    }

    // -----------------------------------------------------------------------
    // (b) task_key presence + regex
    // -----------------------------------------------------------------------
    if (!issue.task_key) {
      // Missing task_key when parent_plan is set: advisory warning
      warnings.push(`${id}: missing task_key`);
      continue;
    }

    if (!TASK_KEY_RE.test(issue.task_key)) {
      errors.push(
        `${id}: task_key "${issue.task_key}" fails /^T\\d+\\.\\d+(\\.\\d+)?$/`,
      );
      continue; // task_key invalid; skip row lookup
    }

    // -----------------------------------------------------------------------
    // (c) task_key present as a row in the plan
    // -----------------------------------------------------------------------
    // Ensure this plan is indexed (may not be in planCache if planGlob didn't
    // discover it — e.g. the yaml points to a plan outside the glob scope)
    if (!planCache.has(planAbsPath)) {
      planCache.set(planAbsPath, indexPlanFile(planAbsPath));
    }

    const planIndex = planCache.get(planAbsPath)!;
    const row = planIndex.get(issue.task_key);

    if (!row) {
      warnings.push(
        `${id}: task_key ${issue.task_key} not found in ${issue.parent_plan}`,
      );
      continue; // can't check (d) without a matching row
    }

    // -----------------------------------------------------------------------
    // (d) plan row back-ref matches yaml id
    // -----------------------------------------------------------------------
    if (row.issueRef !== id) {
      const refLabel = row.issueRef || "(none)";
      warnings.push(
        `${id}: plan row at ${issue.parent_plan}:${row.line} references ${refLabel}`,
      );
    }
  }

  // Strict mode: promote all warnings to errors
  const finalErrors = strict ? [...errors, ...warnings] : errors;
  const finalWarnings = strict ? [] : warnings;
  const exit_code: 0 | 1 = finalErrors.length > 0 ? 1 : 0;

  return { errors: finalErrors, warnings: finalWarnings, exit_code };
}
