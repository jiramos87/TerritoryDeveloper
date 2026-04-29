/**
 * MCP tools (atomic batch authoring) — db-lifecycle-extensions Stage 3 /
 * TECH-3404 + TECH-3405.
 *
 *   - `task_batch_insert(slug, stage_id, tasks[])` — atomic N-task insert
 *     with intra-batch label-based dep resolve in single PG transaction.
 *     Pre-flight collision/missing-label/cycle checks. Returns
 *     `{ok, ids[], id_map}` on success or structured `{ok:false, error}`
 *     on rejection (no DB writes).
 *
 *   - `stage_decompose_apply(slug, stage_id, prose_block, tasks[],
 *     commit_sha?)` — wraps stage prose write + batch task insert in
 *     single PG transaction. Idempotent on re-call with matching
 *     `commit_sha` (UNIQUE on `ia_master_plan_change_log`).
 *
 * Both share `mutateTaskBatchInsert` core logic in
 * `tools/mcp-ia-server/src/ia-db/mutations.ts`.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { IaDbUnavailableError } from "../ia-db/queries.js";
import {
  IaDbValidationError,
  mutateStageDecomposeApply,
  mutateTaskBatchInsert,
  type TaskBatchInsertItem,
} from "../ia-db/mutations.js";

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

function mapDbErrors(e: unknown): never {
  if (e instanceof IaDbUnavailableError) {
    throw {
      code: e.code,
      message: e.message,
      hint: "Set DATABASE_URL or config/postgres-dev.json and restart the MCP server.",
    };
  }
  if (e instanceof IaDbValidationError) {
    throw { code: e.code, message: e.message };
  }
  throw e;
}

const TASK_ITEM_SHAPE = z.object({
  label: z.string().describe("Intra-batch label for dep resolution."),
  prefix: z.enum(["TECH", "FEAT", "BUG", "ART", "AUDIO"]).optional().describe("Id prefix (default TECH)."),
  title: z.string().describe("Task title (non-empty)."),
  body: z.string().optional().describe("Markdown body (default empty)."),
  type: z.string().optional(),
  priority: z.string().optional(),
  notes: z.string().optional(),
  depends_on_labels: z
    .array(z.string())
    .optional()
    .describe("Intra-batch label refs; resolved to ids server-side."),
  status: z
    .enum(["pending", "implemented", "verified", "done", "archived"])
    .optional()
    .describe("Initial status (default pending)."),
});

export function registerTaskBatchInsert(server: McpServer): void {
  server.registerTool(
    "task_batch_insert",
    {
      description:
        "DB-backed atomic batch task insert with intra-batch label-based dep resolution in single PG transaction. Pre-flight collision + unknown-label + cycle checks BEFORE any DB write. Rollback on failure leaves zero residual ia_tasks rows. Returns `{ok:true, ids[], id_map}` on success or structured `{ok:false, error}` on rejection. Honors Invariant 13 — id reservation flows through DB-MCP successor (per-prefix `*_id_seq`).",
      inputSchema: {
        slug: z.string().describe("Master-plan slug."),
        stage_id: z.string().describe("Stage id (must exist in ia_stages)."),
        tasks: z.array(TASK_ITEM_SHAPE).describe("Task items keyed by label."),
      },
    },
    async (args) =>
      runWithToolTiming("task_batch_insert", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  slug?: string;
                  stage_id?: string;
                  tasks?: TaskBatchInsertItem[];
                }
              | undefined,
          ) => {
            const slug = (input?.slug ?? "").trim();
            const stage_id = (input?.stage_id ?? "").trim();
            if (!slug) throw { code: "invalid_input", message: "slug is required." };
            if (!stage_id) throw { code: "invalid_input", message: "stage_id is required." };
            if (!Array.isArray(input?.tasks)) {
              throw { code: "invalid_input", message: "tasks array is required." };
            }
            try {
              return await mutateTaskBatchInsert({
                slug,
                stage_id,
                tasks: input!.tasks!,
              });
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(args as { slug?: string; stage_id?: string; tasks?: TaskBatchInsertItem[] } | undefined);
        return jsonResult(envelope);
      }),
  );
}

const PROSE_BLOCK_SHAPE = z.object({
  title: z.string().nullable().optional(),
  objective: z.string().nullable().optional(),
  exit_criteria: z.string().nullable().optional(),
  body: z.string().optional(),
});

export function registerStageDecomposeApply(server: McpServer): void {
  server.registerTool(
    "stage_decompose_apply",
    {
      description:
        "DB-backed atomic stage decompose: wraps stage prose write (title/objective/exit_criteria/body on `ia_stages`) + batch task insert (via shared `mutateTaskBatchInsert` path) in single PG transaction. Collapses `/stage-decompose` + `/stage-file` into one atomic call. Idempotent on re-call with matching `(slug, stage_id, commit_sha)` — uses UNIQUE constraint on `ia_master_plan_change_log` (kind=`stage-decompose-apply`). Rollback on any sub-step failure rolls back ALL writes (prose + tasks + deps + audit row).",
      inputSchema: {
        slug: z.string().describe("Master-plan slug."),
        stage_id: z.string().describe("Stage id (must exist)."),
        prose_block: PROSE_BLOCK_SHAPE.optional().describe(
          "Optional structured prose update (any subset of title/objective/exit_criteria/body).",
        ),
        tasks: z.array(TASK_ITEM_SHAPE).describe("Task items keyed by label."),
        commit_sha: z
          .string()
          .optional()
          .describe(
            "Optional commit sha for idempotency dedup (UNIQUE on `ia_master_plan_change_log`). Re-call with same sha returns `deduped:true`.",
          ),
      },
    },
    async (args) =>
      runWithToolTiming("stage_decompose_apply", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  slug?: string;
                  stage_id?: string;
                  prose_block?: {
                    title?: string | null;
                    objective?: string | null;
                    exit_criteria?: string | null;
                    body?: string;
                  };
                  tasks?: TaskBatchInsertItem[];
                  commit_sha?: string;
                }
              | undefined,
          ) => {
            const slug = (input?.slug ?? "").trim();
            const stage_id = (input?.stage_id ?? "").trim();
            if (!slug) throw { code: "invalid_input", message: "slug is required." };
            if (!stage_id) throw { code: "invalid_input", message: "stage_id is required." };
            if (!Array.isArray(input?.tasks)) {
              throw { code: "invalid_input", message: "tasks array is required." };
            }
            try {
              return await mutateStageDecomposeApply({
                slug,
                stage_id,
                prose_block: input?.prose_block,
                tasks: input!.tasks!,
                commit_sha: input?.commit_sha,
              });
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(
          args as
            | {
                slug?: string;
                stage_id?: string;
                prose_block?: {
                  title?: string | null;
                  objective?: string | null;
                  exit_criteria?: string | null;
                  body?: string;
                };
                tasks?: TaskBatchInsertItem[];
                commit_sha?: string;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}
