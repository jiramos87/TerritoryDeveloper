/**
 * MCP tool: plan_apply_validate — validate §Plan anchor presence + tuple count
 * in a target markdown file before a Sonnet pair-tail applier runs.
 *
 * Handler reads `target_path`, locates a heading line matching
 * `## {section_header}` (level-2 markdown heading, exact title match), then
 * counts tuple-shaped lines between that heading and the next `## ` heading
 * (or EOF). Tuple detection heuristic (matches plan-apply-pair-contract.md
 * canonical tuple shape `{operation, target_path, target_anchor, payload}`):
 *
 *   - line begins with `-` (list bullet), OR
 *   - line begins with `{` (inline object), OR
 *   - line contains the substring `operation:` (yaml block form)
 *
 * Returns `{ok, found, tuple_count, error?}`:
 *   - missing heading                 → { ok: false, found: false, tuple_count: 0 }
 *   - heading present, no tuples      → { ok: false, found: true,  tuple_count: 0 }
 *   - heading present, N > 0 tuples   → { ok: true,  found: true,  tuple_count: N }
 *   - read / parse error              → { ok: false, found: false, tuple_count: 0, error }
 *
 * Called by Sonnet pair-tail appliers (plan-applier, stage-file-apply)
 * before reading tuples — skip apply
 * when `ok: false`; return control to Opus.
 *
 * NOTE: After editing this descriptor, restart Claude Code (or run
 * `tsx tools/mcp-ia-server/src/index.ts` script) to refresh the in-memory
 * schema cache — the MCP server caches tool descriptors at session start (N4).
 *
 * Registration in `src/index.ts` deferred to TECH-461 (T5.4).
 */

import * as fs from "fs";
import * as path from "path";
import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";

// ---------------------------------------------------------------------------
// Input schema
// ---------------------------------------------------------------------------

const inputShape = {
  section_header: z
    .string()
    .min(1)
    .describe(
      "Exact title text of the `## ` heading to locate (e.g. 'Plan Fix', 'Stage File Plan', 'Code Fix Plan', 'Stage Closeout Plan'). Matched case-sensitively against the heading line body after `## `.",
    ),
  target_path: z
    .string()
    .min(1)
    .describe(
      "Repo-relative path to the markdown file to scan (e.g. 'ia/projects/TECH-460.md'). Absolute paths accepted — used verbatim if present.",
    ),
};

// ---------------------------------------------------------------------------
// Output shape
// ---------------------------------------------------------------------------

export interface PlanApplyValidateResult {
  ok: boolean;
  found: boolean;
  tuple_count: number;
  error?: string;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function jsonResult(payload: unknown) {
  return {
    content: [
      {
        type: "text" as const,
        text: JSON.stringify(payload, null, 2),
      },
    ],
  };
}

/** Escape special regex characters in a string. */
function escapeRegExp(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

/**
 * Count tuple-shaped lines in a slice of markdown text.
 *
 * A "tuple line" matches any of:
 *   - starts with `-` after optional leading whitespace (list bullet)
 *   - starts with `{` after optional leading whitespace (inline object)
 *   - contains the substring `operation:` (yaml block form)
 *
 * Blank lines + comment lines (`<!-- -->`) + fenced-code delimiters are NOT
 * counted as tuples. Conservative heuristic — may under-count exotic shapes;
 * never over-counts on prose.
 */
function countTupleLines(lines: string[]): number {
  let count = 0;
  let inFence = false;
  for (const raw of lines) {
    const line = raw.trimStart();
    // Toggle fenced-code state.
    if (line.startsWith("```")) {
      inFence = !inFence;
      continue;
    }
    if (inFence) continue;
    if (!line) continue;
    if (line.startsWith("<!--")) continue;
    if (line.startsWith("-") || line.startsWith("{") || line.includes("operation:")) {
      count += 1;
    }
  }
  return count;
}

// ---------------------------------------------------------------------------
// Pure core — exported for testability
// ---------------------------------------------------------------------------

/**
 * Validate §{section_header} anchor presence + tuple count in target markdown.
 *
 * Non-throwing: returns structured result in all cases (including fs errors).
 * Appliers branch on `ok` / `found` / `tuple_count` without try/catch.
 *
 * @param repoRoot      - Absolute path to repo root (injected; no env dep).
 * @param sectionHeader - Exact heading title (e.g. "Plan Fix").
 * @param targetPath    - Repo-relative or absolute path to markdown file.
 */
export function validatePlanApply(
  repoRoot: string,
  sectionHeader: string,
  targetPath: string,
): PlanApplyValidateResult {
  const trimmedHeader = sectionHeader.trim();
  const trimmedPath = targetPath.trim();

  if (!trimmedHeader) {
    return {
      ok: false,
      found: false,
      tuple_count: 0,
      error: "section_header is required.",
    };
  }
  if (!trimmedPath) {
    return {
      ok: false,
      found: false,
      tuple_count: 0,
      error: "target_path is required.",
    };
  }

  const absPath = path.isAbsolute(trimmedPath)
    ? trimmedPath
    : path.join(repoRoot, trimmedPath);

  let content: string;
  try {
    content = fs.readFileSync(absPath, "utf8");
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    return {
      ok: false,
      found: false,
      tuple_count: 0,
      error: `Cannot read target_path '${trimmedPath}': ${message}`,
    };
  }

  const lines = content.split("\n");
  // Match `## ` (exactly level 2) followed by optional whitespace + exact title.
  const headingRe = new RegExp(`^##\\s+${escapeRegExp(trimmedHeader)}\\s*$`);
  // Next section boundary: any heading of depth <= 2 (EOF otherwise).
  const nextHeadingRe = /^#{1,2}\s+\S/;

  let headingIdx = -1;
  for (let i = 0; i < lines.length; i++) {
    if (headingRe.test(lines[i]!)) {
      headingIdx = i;
      break;
    }
  }

  if (headingIdx === -1) {
    return { ok: false, found: false, tuple_count: 0 };
  }

  let endIdx = lines.length;
  for (let i = headingIdx + 1; i < lines.length; i++) {
    if (nextHeadingRe.test(lines[i]!)) {
      endIdx = i;
      break;
    }
  }

  const body = lines.slice(headingIdx + 1, endIdx);
  const tupleCount = countTupleLines(body);

  return {
    ok: tupleCount > 0,
    found: true,
    tuple_count: tupleCount,
  };
}

// ---------------------------------------------------------------------------
// Registration
// ---------------------------------------------------------------------------

type ValidateArgs = { section_header?: string; target_path?: string };

/**
 * Register the plan_apply_validate tool.
 *
 * Thin wrapper around `validatePlanApply` — resolves `repoRoot` from env
 * then delegates to the pure core fn.
 *
 * Edit descriptor → restart Claude Code (or `tsx tools/mcp-ia-server/src/index.ts`
 * script) to refresh in-memory schema cache (N4).
 *
 * Registration call site in `src/index.ts` added by TECH-461 (T5.4).
 */
export function registerPlanApplyValidate(server: McpServer): void {
  server.registerTool(
    "plan_apply_validate",
    {
      description:
        "Validate §Plan anchor presence + tuple count in a target markdown file. " +
        "Given { section_header, target_path }, locates the `## {section_header}` " +
        "heading and counts tuple-shaped lines beneath it (lines starting with `-` " +
        "or `{`, or containing `operation:`). Returns { ok, found, tuple_count, " +
        "error? }. Called by Sonnet pair-tail appliers (plan-applier, " +
        "stage-file-apply) before reading " +
        "tuples — skip apply when ok=false; return control to Opus. " +
        "Non-throwing: fs/read errors surface in `error` field with ok=false. " +
        "Schema-cache restart required after adding this tool (N4): restart " +
        "Claude Code or run `tsx tools/mcp-ia-server/src/index.ts`.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("plan_apply_validate", async () => {
        const input = (args ?? {}) as ValidateArgs;
        const sectionHeader = (input.section_header ?? "").trim();
        const targetPath = (input.target_path ?? "").trim();
        const repoRoot = resolveRepoRoot();
        const result = validatePlanApply(repoRoot, sectionHeader, targetPath);
        return jsonResult(result);
      }),
  );
}
