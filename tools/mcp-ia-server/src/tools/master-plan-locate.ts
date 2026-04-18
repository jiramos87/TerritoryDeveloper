/**
 * MCP tool: master_plan_locate — reverse-lookup tool that reads `parent_plan` +
 * `task_key` from a backlog yaml record and finds the matching row in the
 * master-plan file, returning `{ plan, step, stage, phase, task_key, row_line,
 * row_raw }`.
 *
 * NOTE: After editing this descriptor, restart Claude Code (or run
 * `tsx tools/mcp-ia-server/src/index.ts` script) to refresh the in-memory
 * schema cache — the MCP server caches tool descriptors at session start (N4).
 */

import * as fs from "fs";
import * as path from "path";
import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { parseBacklogIssue } from "../parser/backlog-parser.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

// ---------------------------------------------------------------------------
// Input schema
// ---------------------------------------------------------------------------

const inputShape = {
  issue_id: z
    .string()
    .describe("Issue id (e.g. TECH-413). Case-insensitive type prefix."),
};

// ---------------------------------------------------------------------------
// Output shape
// ---------------------------------------------------------------------------

export interface MasterPlanLocateResult {
  plan: string;
  step: number | null;
  stage: string | null;
  phase: number | null;
  task_key: string;
  row_line: number;
  row_raw: string;
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

// ---------------------------------------------------------------------------
// Pure core — exported for testability (TECH-414)
// ---------------------------------------------------------------------------

/**
 * Locate the master-plan row for an issue by reading its yaml record's
 * `parent_plan` + `task_key` fields and scanning the plan file on disk.
 *
 * @param repoRoot - Absolute path to the repository root (injected; no env dependency).
 * @param issueId  - Canonical issue id (e.g. "TECH-413").
 * @throws `{ code, message, hint, details? }` on any error condition.
 */
export function locateMasterPlanRow(
  repoRoot: string,
  issueId: string,
): MasterPlanLocateResult {
  const trimmedId = issueId.trim();
  if (!trimmedId) {
    throw { code: "invalid_input", message: "issue_id is required." };
  }

  const parsed = parseBacklogIssue(repoRoot, trimmedId);
  if (!parsed) {
    throw {
      code: "issue_not_found",
      message: `No issue '${trimmedId}' in BACKLOG.md or BACKLOG-ARCHIVE.md.`,
      hint: "Check ia/backlog/ and ia/backlog-archive/",
    };
  }

  // Guard locator fields
  if (!parsed.parent_plan || !parsed.task_key) {
    const missing = !parsed.parent_plan ? "parent_plan" : "task_key";
    throw {
      code: "missing_locator_fields",
      message: `Issue '${trimmedId}' yaml is missing '${missing}' — cannot locate plan row.`,
      hint: "Populate parent_plan and task_key in the yaml record (schema v2).",
      details: { field: missing },
    };
  }

  // Resolve plan path
  const planPath = path.join(repoRoot, parsed.parent_plan);
  if (!fs.existsSync(planPath)) {
    throw {
      code: "plan_not_found",
      message: `Plan file '${parsed.parent_plan}' not found on disk.`,
      hint: `Expected at: ${planPath}`,
    };
  }

  // Scan lines for task_key
  const content = fs.readFileSync(planPath, "utf8");
  const lines = content.split("\n");
  const pattern = new RegExp(`^\\| ${escapeRegExp(parsed.task_key)} \\|`);
  let rowLine = -1;
  let rowRaw = "";
  for (let i = 0; i < lines.length; i++) {
    if (pattern.test(lines[i]!)) {
      rowLine = i + 1; // 1-based
      rowRaw = lines[i]!;
      break;
    }
  }

  if (rowLine === -1) {
    throw {
      code: "task_key_drift",
      message: `task_key '${parsed.task_key}' not found in plan '${parsed.parent_plan}'.`,
      hint: "Update task_key in the yaml record or add the row to the plan.",
    };
  }

  return {
    plan: parsed.parent_plan,
    step: parsed.step ?? null,
    stage: parsed.stage ?? null,
    phase: parsed.phase ?? null,
    task_key: parsed.task_key,
    row_line: rowLine,
    row_raw: rowRaw,
  };
}

// ---------------------------------------------------------------------------
// Registration
// ---------------------------------------------------------------------------

type LocateArgs = { issue_id?: string };

/**
 * Register the master_plan_locate tool.
 *
 * Thin wrapper around `locateMasterPlanRow` — resolves `repoRoot` from env
 * then delegates to the pure core fn.
 *
 * Edit descriptor → restart Claude Code (or `tsx tools/mcp-ia-server/src/index.ts`
 * script) to refresh in-memory schema cache (N4 — schema-cache restart required
 * after adding this tool).
 */
export function registerMasterPlanLocate(server: McpServer): void {
  server.registerTool(
    "master_plan_locate",
    {
      description:
        "Given an issue_id, reads its yaml record's `parent_plan` + `task_key` " +
        "and locates the matching task row in the master-plan file. " +
        "Returns { plan, step, stage, phase, task_key, row_line (1-based), row_raw }. " +
        "Errors: missing_locator_fields (yaml lacks parent_plan or task_key), " +
        "plan_not_found (plan path absent on disk), " +
        "task_key_drift (task_key not found in plan). " +
        "Schema-cache restart required after adding this tool (N4): " +
        "restart Claude Code or run `tsx tools/mcp-ia-server/src/index.ts`.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("master_plan_locate", async () => {
        const envelope = await wrapTool(
          async (input: LocateArgs | undefined): Promise<MasterPlanLocateResult> => {
            const issueId = (input?.issue_id ?? "").trim();
            if (!issueId) {
              throw { code: "invalid_input", message: "issue_id is required." };
            }
            const repoRoot = resolveRepoRoot();
            return locateMasterPlanRow(repoRoot, issueId);
          },
        )(args as LocateArgs | undefined);

        return jsonResult(envelope);
      }),
  );
}
