/**
 * MCP tool: master_plan_bundle_apply
 *
 * Atomic bulk plan author. Single Postgres tx that inserts (or version-bumps)
 * one master plan + N stages + M tasks from a single jsonb bundle. Wrapper
 * around SQL function `master_plan_bundle_apply(jsonb)` (migs 0067 / 0078).
 *
 * Three apply paths (decided by the SQL fn):
 *   - insert_new        — slug not present
 *   - backfill_replace  — existing row has backfilled=true and same version
 *   - version_bump      — incoming version > existing version
 *
 * Bundle JSONB shape:
 *   {
 *     "plan":   {"slug": text, "title": text, "description"?: text,
 *                "preamble"?: text, "parent_plan_slug"?: text,
 *                "version"?: int, "backfilled"?: bool},
 *     "stages": [{"stage_id": text, "title"?: text, "objective"?: text,
 *                 "exit_criteria"?: text, "status"?: stage_status,
 *                 "section_id"?: text, "carcass_role"?: text,
 *                 "visibility_delta"?: text}],
 *     "tasks":  [{"task_key": text, "stage_id": text, "prefix": text,
 *                 "title": text, "body"?: text, "digest_body"?: text,
 *                 "status"?: task_status, "type"?: text, "priority"?: text,
 *                 "notes"?: text, "backfilled"?: bool}]
 *   }
 *
 * Returns: {plan_slug, apply_path, plan_version, stages_inserted, tasks_inserted}.
 *
 * Any constraint failure inside the SQL fn rolls the entire bundle back.
 *
 * Schema-cache restart required after adding this tool: restart Claude Code or
 * reload the MCP host (server caches descriptors at session start — N4).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { IaDbUnavailableError } from "../ia-db/queries.js";

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

const planShape = z
  .object({
    slug: z.string().min(1).describe("Master-plan slug (DB primary key)."),
    title: z.string().optional().describe("Plan title (defaults to '')."),
    description: z.string().optional().describe("Plan description / mission line."),
    preamble: z.string().optional().describe("Plan preamble markdown body."),
    parent_plan_slug: z
      .string()
      .optional()
      .describe("Parent plan slug for child / umbrella relationships."),
    version: z
      .number()
      .int()
      .optional()
      .describe(
        "Plan version. Default 1. To version-bump an existing plan, supply a value > current version.",
      ),
    backfilled: z
      .boolean()
      .optional()
      .describe("Mark plan row as backfilled placeholder. Default false."),
    priority: z
      .enum(["P0", "P1", "P2", "P3"])
      .optional()
      .describe("Master-plan priority (mig 0158). Default 'P2'."),
    design_id: z
      .number()
      .int()
      .nullable()
      .optional()
      .describe(
        "ia_plan_designs.id FK (mig 0158/0159). When set, the SQL fn auto-flips seed status='consumed' inside the same tx.",
      ),
  })
  .passthrough();

const redStageProofBlockShape = z
  .object({
    red_test_anchor: z
      .string()
      .optional()
      .describe("Red test anchor `{path}::{symbol}`. Defaults to 'design_only' when omitted."),
    target_kind: z
      .string()
      .optional()
      .describe("Surface kind tag (e.g. mcp_tool, sql_fn, c_sharp_method, design_only)."),
    proof_artifact_id: z
      .string()
      .optional()
      .describe("Linked red-stage proof id (when surfaced via red_stage_proof_capture)."),
    proof_status: z
      .string()
      .optional()
      .describe("'pending' | 'captured' | 'finalized' | 'not_applicable'."),
  })
  .passthrough();

const stageShape = z
  .object({
    stage_id: z.string().min(1).describe("Stage id e.g. 'stage-1-tracer'."),
    title: z.string().optional(),
    objective: z.string().optional(),
    exit_criteria: z.string().optional(),
    status: z.string().optional().describe("stage_status enum value."),
    section_id: z.string().optional(),
    carcass_role: z.string().optional(),
    visibility_delta: z.string().optional(),
    body: z
      .string()
      .optional()
      .describe(
        "Stage body markdown. When omitted + red_stage_proof_block present, server-side renders via format_stage_body() (mig 0136).",
      ),
    red_stage_proof_block: redStageProofBlockShape
      .optional()
      .describe(
        "Structured §Red-Stage Proof block (mig 0136). Server-side rendered when stage.body is absent.",
      ),
  })
  .passthrough();

const taskShape = z
  .object({
    task_key: z
      .string()
      .optional()
      .describe("Human-facing T-coordinate e.g. 'T1.0.1' (embedded in body comment)."),
    stage_id: z.string().min(1).describe("Foreign-key stage_id within plan."),
    prefix: z
      .string()
      .min(1)
      .describe("Issue prefix: TECH | FEAT | BUG | ART | AUDIO."),
    title: z.string().describe("Task title."),
    body: z.string().optional().describe("§Plan Digest body (canonical key)."),
    digest_body: z
      .string()
      .optional()
      .describe(
        "§Plan Digest body (legacy alias key). SQL fn COALESCEs body→digest_body→''.",
      ),
    status: z.string().optional().describe("task_status enum value (default pending)."),
    type: z.string().optional(),
    priority: z.string().optional(),
    notes: z.string().optional(),
    backfilled: z.boolean().optional(),
  })
  .passthrough();

const inputShape = {
  bundle: z
    .object({
      plan: planShape,
      stages: z.array(stageShape).optional().default([]),
      tasks: z.array(taskShape).optional().default([]),
    })
    .describe(
      "Atomic plan bundle. {plan, stages[], tasks[]}. SQL fn decides apply_path: insert_new | backfill_replace | version_bump.",
    ),
};

interface BundleInput {
  bundle?: unknown;
}

export function registerMasterPlanBundleApply(server: McpServer): void {
  server.registerTool(
    "master_plan_bundle_apply",
    {
      description:
        "Atomic bulk plan author: SELECT master_plan_bundle_apply($1::jsonb). " +
        "One Postgres tx inserts ia_master_plans + N ia_stages + M ia_tasks. " +
        "Apply paths: insert_new | backfill_replace | version_bump (decided by SQL fn). " +
        "Task body resolution: body | digest_body | '' (mig 0078 dual-key COALESCE). " +
        "Stage body resolution: body | format_stage_body(red_stage_proof_block) | '' (mig 0136). " +
        "Inside tx: promote_drift_lint_staged(slug, version) flips pre-staged drift findings → queued (mig 0136). " +
        "task_id minted via per-prefix nextval(tech_id_seq | feat_id_seq | bug_id_seq | art_id_seq | audio_id_seq). " +
        "task_key embedded in body via <!-- task_key: T{N.M.K} --> header. " +
        "Returns {plan_slug, apply_path, plan_version, stages_inserted, tasks_inserted, drift_lint_promoted}. " +
        "Any constraint failure rolls back the entire bundle. " +
        "Used by /ship-plan Phase 7. Schema-cache restart required after adding this tool (N4).",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("master_plan_bundle_apply", async () => {
        const envelope = await wrapTool(async (input: BundleInput | undefined) => {
          const bundle = input?.bundle;
          if (!bundle || typeof bundle !== "object") {
            throw {
              code: "invalid_input",
              message: "bundle is required and must be an object {plan, stages?, tasks?}.",
            };
          }
          const plan = (bundle as { plan?: { slug?: string } }).plan;
          if (!plan?.slug || typeof plan.slug !== "string" || plan.slug.trim().length === 0) {
            throw {
              code: "invalid_input",
              message: "bundle.plan.slug is required.",
            };
          }
          const pool = getIaDatabasePool();
          if (!pool) {
            throw new IaDbUnavailableError();
          }
          try {
            const res = await pool.query<{ result: unknown }>(
              "SELECT master_plan_bundle_apply($1::jsonb) AS result",
              [JSON.stringify(bundle)],
            );
            const result = res.rows[0]?.result;
            return result ?? {};
          } catch (e) {
            if (e && typeof e === "object" && "code" in e) {
              const code = String((e as { code: unknown }).code);
              const message =
                e instanceof Error ? e.message : String((e as { message?: unknown }).message ?? e);
              // Postgres SQLSTATE 23505 = unique_violation; surface verbatim so
              // callers can detect duplicate plan slug + retry as version_bump.
              throw { code: `pg_${code}`, message };
            }
            const msg = e instanceof Error ? e.message : String(e);
            throw { code: "db_error", message: msg };
          }
        })(args as BundleInput | undefined);
        return jsonResult(envelope);
      }),
  );
}
