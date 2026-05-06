/**
 * MCP tool: master_plan_next_pending — scans a master-plan task table and
 * returns the first row whose Status is `_pending_` or `Draft` (top-of-table
 * tie-break; deterministic).
 *
 * Input:  { plan: string, stage?: string }
 * Output: { issue_id: string | null, task_key: string, row_line: number, status: string } | null
 *
 * DB path (use_db=true, TECH-15908): reads ia_stage_facet_view and returns
 * all parallel-ready tasks via Kahn's algorithm (in-degree zero).
 * Input:  { slug: string, stage_id?: string, use_db: true }
 * Output: { parallel_ready: [{task_id, title, stage_id}], slug }
 *
 * `issue_id` is null when the Issue cell is `_pending_` (task not yet filed).
 * Returns null when no pending / Draft rows exist (not an error).
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
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";

// ---------------------------------------------------------------------------
// Input schema
// ---------------------------------------------------------------------------

const inputShape = {
  plan: z
    .string()
    .optional()
    .describe(
      "Path to the master-plan file. Absolute path or REPO_ROOT-relative " +
        "(e.g. \"ia/projects/foo-master-plan.md\"). Required when use_db=false (default).",
    ),
  stage: z
    .string()
    .optional()
    .describe(
      "Optional stage id to narrow the scan (e.g. \"4.1\"). " +
        "When given, only the matching `#### Stage X.Y` section is searched. " +
        "Missing stage heading → returns null (poller-friendly, not an error).",
    ),
  // DB path (TECH-15908 — Kahn's algorithm via ia_stage_facet_view)
  use_db: z
    .boolean()
    .optional()
    .describe(
      "When true, reads from ia_stage_facet_view (DB) instead of the plan file. " +
        "Returns all parallel-ready tasks (Kahn's in-degree-zero). Requires `slug`.",
    ),
  slug: z
    .string()
    .optional()
    .describe("Master-plan slug. Required when use_db=true."),
  stage_id: z
    .string()
    .optional()
    .describe("Stage id filter for DB path (e.g. '2'). Optional when use_db=true."),
};

// ---------------------------------------------------------------------------
// Output shape
// ---------------------------------------------------------------------------

export interface NextPendingResult {
  issue_id: string | null; // null when Issue cell is "_pending_" (unfiled)
  task_key: string; // e.g. "T4.1.3"
  row_line: number; // 1-based
  status: string; // "_pending_" | "Draft"
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

// ---------------------------------------------------------------------------
// Regex constants
// ---------------------------------------------------------------------------

/**
 * Matches a canonical task-table row.
 * Group 1 = task_key (e.g. "T4.1.3")
 * Group 2 = issue cell raw (e.g. "**TECH-415**" or "_pending_")
 * Group 3 = status cell raw (e.g. "Draft", "_pending_", "Done (archived)")
 */
const TASK_ROW_RE =
  /^\|\s*(T[\d.]+)\s*\|[^|]*\|[^|]*\|\s*(\*\*[A-Z]+-\d+\*\*|_pending_)\s*\|\s*([^|]+?)\s*\|/;

/**
 * Matches `#### Stage X.Y` heading lines.
 * Group 1 = stage id (e.g. "4.1")
 */
const STAGE_HEADING_RE = /^####\s+Stage\s+([\d.]+)\b/;

/** Pending predicate — only these statuses are returned. */
function isPending(status: string): boolean {
  return status === "_pending_" || status === "Draft";
}

// ---------------------------------------------------------------------------
// Pure core — exported for testability (TECH-416)
// ---------------------------------------------------------------------------

/**
 * Scan a master-plan file and return the first task row whose Status is
 * `_pending_` or `Draft`.
 *
 * @param repoRoot - Absolute path to the repository root (injected; no env dependency).
 * @param plan     - Absolute or REPO_ROOT-relative path to the master-plan file.
 * @param stage    - Optional stage id to narrow the scan (e.g. "4.1").
 * @returns        - First matching row, or null when no pending row found.
 * @throws         - `{ code, message, hint }` on invalid_input or plan_not_found.
 */
export function findNextPendingRow(
  repoRoot: string,
  plan: string,
  stage?: string,
): NextPendingResult | null {
  const trimmedPlan = plan.trim();
  if (!trimmedPlan) {
    throw { code: "invalid_input", message: "plan is required." };
  }

  // Resolve path: absolute → use as-is; relative → join with repoRoot.
  const planPath = path.isAbsolute(trimmedPlan)
    ? trimmedPlan
    : path.join(repoRoot, trimmedPlan);

  if (!fs.existsSync(planPath)) {
    throw {
      code: "plan_not_found",
      message: `Plan file '${trimmedPlan}' not found on disk.`,
      hint: `Expected at: ${planPath}`,
    };
  }

  const content = fs.readFileSync(planPath, "utf8");
  const lines = content.split("\n");

  // Determine scan range.
  let startIdx = 0;
  let endIdx = lines.length;

  if (stage !== undefined && stage.trim() !== "") {
    const targetStage = stage.trim();
    let found = false;

    for (let i = 0; i < lines.length; i++) {
      const m = STAGE_HEADING_RE.exec(lines[i]!);
      if (m && m[1] === targetStage) {
        startIdx = i + 1; // scan starts after the heading line
        found = true;

        // Find end boundary: next stage heading, Step heading, or h2 heading.
        for (let j = i + 1; j < lines.length; j++) {
          const line = lines[j]!;
          if (
            STAGE_HEADING_RE.test(line) ||
            /^###\s+Step\s+/.test(line) ||
            /^##\s+/.test(line)
          ) {
            endIdx = j;
            break;
          }
        }
        break;
      }
    }

    if (!found) {
      // Stage heading absent — poller-friendly null (not an error).
      return null;
    }
  }

  // Linear scan within [startIdx, endIdx).
  for (let i = startIdx; i < endIdx; i++) {
    const m = TASK_ROW_RE.exec(lines[i]!);
    if (!m) continue;

    const taskKey = m[1]!.trim();
    const issueRaw = m[2]!.trim();
    const statusRaw = m[3]!.trim();

    if (!isPending(statusRaw)) continue;

    // Normalize issue_id: strip ** from "**TECH-415**"; null for "_pending_".
    const issueId =
      issueRaw === "_pending_" ? null : issueRaw.replace(/^\*\*|\*\*$/g, "");

    return {
      issue_id: issueId,
      task_key: taskKey,
      row_line: i + 1, // 1-based
      status: statusRaw,
    };
  }

  return null;
}

// ---------------------------------------------------------------------------
// DB path — Kahn's algorithm via ia_stage_facet_view (TECH-15908)
// ---------------------------------------------------------------------------

export interface ParallelReadyTask {
  task_id: string;
  title: string;
  stage_id: string | null;
  dep_count: number;
  unresolved_dep_count: number;
}

export interface KahnReadyResult {
  slug: string;
  stage_id: string | null;
  parallel_ready: ParallelReadyTask[];
}

/**
 * Kahn's algorithm in-degree-zero resolver via ia_stage_facet_view.
 * Returns all tasks with status='pending' AND unresolved_dep_count=0
 * for the given slug (optionally filtered to one stage_id).
 *
 * Reads from the MV directly — single query, no N+1 dep checks.
 * MV is refreshed after each task_status_flip (TECH-15907).
 */
export async function findParallelReadyTasksDB(
  slug: string,
  stage_id?: string,
): Promise<KahnReadyResult> {
  const pool = getIaDatabasePool();
  if (!pool) {
    throw { code: "ia_db_unavailable", message: "DATABASE_URL not configured." };
  }

  const params: unknown[] = [slug];
  let stageFilter = "";
  if (stage_id) {
    params.push(stage_id);
    stageFilter = `AND stage_id = $${params.length}`;
  }

  const res = await pool.query<{
    task_id: string;
    title: string;
    stage_id: string | null;
    dep_count: number;
    unresolved_dep_count: number;
  }>(
    `SELECT task_id, title, stage_id, dep_count, unresolved_dep_count
       FROM ia_stage_facet_view
      WHERE slug = $1
        AND parallel_ready = TRUE
        ${stageFilter}
      ORDER BY task_id ASC`,
    params,
  );

  return {
    slug,
    stage_id: stage_id ?? null,
    parallel_ready: res.rows,
  };
}

// ---------------------------------------------------------------------------
// Registration
// ---------------------------------------------------------------------------

type NextPendingArgs = {
  plan?: string;
  stage?: string;
  use_db?: boolean;
  slug?: string;
  stage_id?: string;
};

/**
 * Register the master_plan_next_pending tool.
 *
 * Thin wrapper around `findNextPendingRow` — resolves `repoRoot` from env
 * then delegates to the pure core fn.
 *
 * Edit descriptor → restart Claude Code (or `tsx tools/mcp-ia-server/src/index.ts`
 * script) to refresh in-memory schema cache (N4 — schema-cache restart required
 * after adding this tool).
 */
export function registerMasterPlanNextPending(server: McpServer): void {
  server.registerTool(
    "master_plan_next_pending",
    {
      description:
        "Scan a master-plan task table and return the first row whose Status " +
        "is `_pending_` or `Draft` (top-of-table tie-break; deterministic). " +
        "Primary use case: `/ship` next-task lookup — find the next task to " +
        "advance without manually scanning the plan. " +
        "Input: { plan: string, stage?: string }. " +
        "`plan` is an absolute or REPO_ROOT-relative path to the master-plan file. " +
        "`stage` (optional) narrows the scan to a single `#### Stage X.Y` section; " +
        "missing stage heading returns null (poller-friendly, not an error). " +
        "Result shape: { issue_id: string | null, task_key: string, row_line: number, status: string } | null. " +
        "`issue_id` is null when the Issue cell is `_pending_` (task not yet filed via /project-new). " +
        "Returns null when no pending/Draft rows exist (stage or plan fully Done). " +
        "Errors: invalid_input (empty plan), plan_not_found (path absent on disk). " +
        "DB path (use_db=true, TECH-15908): requires `slug`; reads `ia_stage_facet_view` via " +
        "Kahn's algorithm and returns all parallel-ready tasks (in-degree zero). " +
        "Result shape when use_db=true: { slug, stage_id, parallel_ready: [{task_id, title, stage_id, dep_count, unresolved_dep_count}] }. " +
        "Schema-cache restart required after adding this tool (N4): " +
        "restart Claude Code or run `tsx tools/mcp-ia-server/src/index.ts`.",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("master_plan_next_pending", async () => {
        const envelope = await wrapTool(
          async (
            input: NextPendingArgs | undefined,
          ): Promise<NextPendingResult | KahnReadyResult | null> => {
            // DB path — Kahn's algorithm (TECH-15908)
            if (input?.use_db === true) {
              const slug = (input?.slug ?? "").trim();
              if (!slug) {
                throw { code: "invalid_input", message: "slug is required when use_db=true." };
              }
              return findParallelReadyTasksDB(slug, input?.stage_id);
            }

            // Filesystem path (original behavior)
            const plan = (input?.plan ?? "").trim();
            if (!plan) {
              throw { code: "invalid_input", message: "plan is required." };
            }
            const repoRoot = resolveRepoRoot();
            return findNextPendingRow(repoRoot, plan, input?.stage);
          },
        )(args as NextPendingArgs | undefined);

        return jsonResult(envelope);
      }),
  );
}
