/**
 * MCP tools (write/mutation, DB-backed) — Step 4 of ia-dev-db-refactor.
 *
 * Registers DB-backed mutation tools on the IA core server bucket:
 *   - task_insert, task_status_flip, task_spec_section_write
 *   - task_raw_markdown_write
 *   - task_dep_register
 *   - stage_closeout_apply
 *   - fix_plan_write, fix_plan_consume
 *   - master_plan_close, master_plan_version_create
 *   - task_status_flip_batch
 *
 * Deleted (async-cron-jobs Stage 6 — async replacements via cron_*_enqueue):
 *   - task_commit_record → cron_task_commit_record_enqueue
 *   - stage_verification_flip → cron_stage_verification_flip_enqueue
 *   - journal_append → cron_journal_append_enqueue
 *
 * All mutations transactional (BEGIN/COMMIT/ROLLBACK). Id reservation uses
 * per-prefix DB sequences (no filesystem `reserve-id.sh`). Row-level locks
 * via `SELECT ... FOR UPDATE` where mutation depends on prior read.
 *
 * Error shape:
 *   - `db_unconfigured` — DB pool not initialised.
 *   - `invalid_input`   — caller arg validation failed (missing/unknown fks,
 *                         bad enum, non-terminal stage closeout, etc.).
 *   - `db_error`        — bare exception fallthrough inside withTx.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { IaDbUnavailableError } from "../ia-db/queries.js";
import {
  IaDbValidationError,
  mutateFixPlanConsume,
  mutateFixPlanWrite,
  mutateMasterPlanClose,
  mutateMasterPlanVersionCreate,
  mutateStageCloseoutApply,
  mutateTaskDepRegister,
  mutateTaskInsert,
  mutateTaskRawMarkdownWrite,
  mutateTaskSpecSectionWrite,
  mutateTaskStatusFlip,
  mutateTaskStatusFlipBatch,
} from "../ia-db/mutations.js";

// ---------------------------------------------------------------------------
// Shared helpers.
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

const PREFIX_ENUM = z.enum(["TECH", "FEAT", "BUG", "ART", "AUDIO"]);
const STATUS_ENUM = z.enum([
  "pending",
  "implemented",
  "verified",
  "done",
  "archived",
]);
const COMMIT_KIND_ENUM = z.enum([
  "feat",
  "fix",
  "chore",
  "docs",
  "refactor",
  "test",
]);
const VERDICT_ENUM = z.enum(["pass", "fail", "partial"]);

// ---------------------------------------------------------------------------
// task_insert
// ---------------------------------------------------------------------------

export function registerTaskInsert(server: McpServer): void {
  server.registerTool(
    "task_insert",
    {
      description:
        "DB-backed: reserve a monotonic id via per-prefix sequence (TECH/FEAT/BUG/ART/AUDIO), insert row + body + deps in one tx. Replaces `reserve-id.sh` + filesystem yaml write + `materialize-backlog.sh`. Returns new task_id plus echoed deps.",
      inputSchema: {
        prefix: PREFIX_ENUM.describe("Id prefix — drives sequence selection."),
        title: z.string().describe("Task title (non-empty)."),
        body: z.string().optional().describe("Markdown body (default empty)."),
        slug: z.string().optional().describe("Master-plan slug. Required when stage_id set."),
        stage_id: z
          .string()
          .optional()
          .describe("Stage id inside the slug. FK-checked against ia_stages."),
        type: z.string().optional().describe("Optional task type label."),
        priority: z.string().optional().describe("Optional priority label."),
        notes: z.string().optional().describe("Optional notes column."),
        depends_on: z
          .array(z.string())
          .optional()
          .describe("Task ids this task depends on. All must exist."),
        related: z
          .array(z.string())
          .optional()
          .describe("Related task ids. All must exist."),
        status: STATUS_ENUM.optional().describe("Initial status (default `pending`)."),
        raw_markdown: z
          .string()
          .optional()
          .describe(
            "Verbatim BACKLOG.md row block (checklist line + sub-bullets). Persisted to `ia_tasks.raw_markdown` (migration 0017) so the DB-sourced BACKLOG.md generator can emit byte-identical output. Omit to leave column null.",
          ),
      },
    },
    async (args) =>
      runWithToolTiming("task_insert", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  prefix?: "TECH" | "FEAT" | "BUG" | "ART" | "AUDIO";
                  title?: string;
                  body?: string;
                  slug?: string;
                  stage_id?: string;
                  type?: string;
                  priority?: string;
                  notes?: string;
                  depends_on?: string[];
                  related?: string[];
                  status?:
                    | "pending"
                    | "implemented"
                    | "verified"
                    | "done"
                    | "archived";
                  raw_markdown?: string;
                }
              | undefined,
          ) => {
            const prefix = input?.prefix;
            const title = (input?.title ?? "").trim();
            if (!prefix) {
              throw { code: "invalid_input", message: "prefix is required." };
            }
            if (!title) {
              throw { code: "invalid_input", message: "title is required." };
            }
            if (input?.stage_id && !input?.slug) {
              throw {
                code: "invalid_input",
                message: "slug is required when stage_id is set.",
              };
            }
            try {
              return await mutateTaskInsert({
                prefix,
                title,
                body: input?.body,
                slug: input?.slug,
                stage_id: input?.stage_id,
                type: input?.type ?? null,
                priority: input?.priority ?? null,
                notes: input?.notes ?? null,
                depends_on: input?.depends_on,
                related: input?.related,
                status: input?.status,
                raw_markdown: input?.raw_markdown ?? null,
              });
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(
          args as
            | {
                prefix?: "TECH" | "FEAT" | "BUG" | "ART" | "AUDIO";
                title?: string;
                body?: string;
                slug?: string;
                stage_id?: string;
                type?: string;
                priority?: string;
                notes?: string;
                depends_on?: string[];
                related?: string[];
                status?:
                  | "pending"
                  | "implemented"
                  | "verified"
                  | "done"
                  | "archived";
                raw_markdown?: string;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// task_status_flip
// ---------------------------------------------------------------------------

export function registerTaskStatusFlip(server: McpServer): void {
  server.registerTool(
    "task_status_flip",
    {
      description:
        "DB-backed: flip one task's status with row-level lock (`SELECT FOR UPDATE`). Stamps `completed_at` when status=done, `archived_at` when status=archived. Returns prev + new status.",
      inputSchema: {
        task_id: z.string().describe("Task id e.g. TECH-776."),
        new_status: STATUS_ENUM.describe(
          "Target status: pending|implemented|verified|done|archived.",
        ),
      },
    },
    async (args) =>
      runWithToolTiming("task_status_flip", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  task_id?: string;
                  new_status?:
                    | "pending"
                    | "implemented"
                    | "verified"
                    | "done"
                    | "archived";
                }
              | undefined,
          ) => {
            const id = (input?.task_id ?? "").trim().toUpperCase();
            const ns = input?.new_status;
            if (!id || !ns) {
              throw {
                code: "invalid_input",
                message: "task_id and new_status are required.",
              };
            }
            try {
              return await mutateTaskStatusFlip(id, ns);
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(
          args as
            | {
                task_id?: string;
                new_status?:
                  | "pending"
                  | "implemented"
                  | "verified"
                  | "done"
                  | "archived";
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// task_spec_section_write
// ---------------------------------------------------------------------------

export function registerTaskSpecSectionWrite(server: McpServer): void {
  server.registerTool(
    "task_spec_section_write",
    {
      description:
        "DB-backed: replace (or append) one section of a task body, snapshotting the old body into ia_task_spec_history first. Content must begin with a heading line matching the section level. If section missing → appended at end with blank separator. Heading match is §-tolerant (`§Plan Digest` matches `Plan Digest` and vice versa); when the literal § marker on the content heading line differs from the `section` arg, the heading is auto-normalized to the canonical form and `heading_normalized: true` is returned so callers can detect authoring drift.",
      inputSchema: {
        task_id: z.string().describe("Task id e.g. TECH-776."),
        section: z
          .string()
          .describe(
            "Canonical section heading text (case-insensitive; § prefix tolerated). Example: `§Plan Digest`.",
          ),
        content: z
          .string()
          .describe(
            "Replacement section incl. its heading line. The heading line is auto-normalized to match the `section` arg's literal § presence.",
          ),
        actor: z.string().optional().describe("Who performed the edit (optional)."),
        git_sha: z.string().optional().describe("Current commit sha (optional)."),
        change_reason: z
          .string()
          .optional()
          .describe("Short reason string e.g. `plan-digest write`."),
      },
    },
    async (args) =>
      runWithToolTiming("task_spec_section_write", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  task_id?: string;
                  section?: string;
                  content?: string;
                  actor?: string;
                  git_sha?: string;
                  change_reason?: string;
                }
              | undefined,
          ) => {
            const id = (input?.task_id ?? "").trim().toUpperCase();
            const section = (input?.section ?? "").trim();
            const content = input?.content ?? "";
            if (!id || !section || !content) {
              throw {
                code: "invalid_input",
                message: "task_id, section, and content are required.",
              };
            }
            try {
              return await mutateTaskSpecSectionWrite(id, section, content, {
                actor: input?.actor,
                git_sha: input?.git_sha,
                change_reason: input?.change_reason,
              });
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(
          args as
            | {
                task_id?: string;
                section?: string;
                content?: string;
                actor?: string;
                git_sha?: string;
                change_reason?: string;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// task_dep_register (TECH-2976)
// ---------------------------------------------------------------------------

export function registerTaskDepRegister(server: McpServer): void {
  server.registerTool(
    "task_dep_register",
    {
      description:
        "DB-backed: atomically register `depends_on` edges into `ia_task_deps` with Tarjan SCC cycle detection inside a single PG transaction. Inserts use `ON CONFLICT DO NOTHING` (idempotent re-register). Cycle-inducing edges → ROLLBACK + structured `{ok:false, error:{code:'cycle_detected', scc_members:[...]}}` (non-throw). Self-references rejected pre-Tarjan.",
      inputSchema: {
        task_id: z.string().describe("Task id e.g. TECH-776 (must exist)."),
        depends_on: z
          .array(z.string())
          .describe(
            "Existing task ids this task depends on. Empty array allowed (no-op).",
          ),
      },
    },
    async (args) =>
      runWithToolTiming("task_dep_register", async () => {
        const envelope = await wrapTool(
          async (
            input: { task_id?: string; depends_on?: string[] } | undefined,
          ) => {
            const id = (input?.task_id ?? "").trim().toUpperCase();
            const deps = Array.isArray(input?.depends_on)
              ? input!.depends_on!.map((s) => (s ?? "").trim().toUpperCase())
              : [];
            if (!id) {
              throw {
                code: "invalid_input",
                message: "task_id is required.",
              };
            }
            try {
              return await mutateTaskDepRegister(id, deps);
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(args as { task_id?: string; depends_on?: string[] } | undefined);
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// task_raw_markdown_write
// ---------------------------------------------------------------------------

export function registerTaskRawMarkdownWrite(server: McpServer): void {
  server.registerTool(
    "task_raw_markdown_write",
    {
      description:
        "DB-backed: persist verbatim BACKLOG.md row block into `ia_tasks.raw_markdown`. Single-row UPDATE, idempotent. Replaces the Pass-A-null + Pass-B-backfill workaround in `/stage-file` (TECH-2973). Body string is stored byte-identical so the DB-sourced BACKLOG.md generator can emit unchanged output.",
      inputSchema: {
        task_id: z.string().describe("Task id e.g. TECH-776."),
        body: z
          .string()
          .describe(
            "Verbatim BACKLOG.md row block (checklist line + sub-bullets). Empty string accepted; null is reserved for `never written` sentinel.",
          ),
      },
    },
    async (args) =>
      runWithToolTiming("task_raw_markdown_write", async () => {
        const envelope = await wrapTool(
          async (
            input: { task_id?: string; body?: string } | undefined,
          ) => {
            const id = (input?.task_id ?? "").trim().toUpperCase();
            const body = input?.body;
            if (!id) {
              throw {
                code: "invalid_input",
                message: "task_id is required.",
              };
            }
            if (typeof body !== "string") {
              throw {
                code: "invalid_input",
                message: "body is required and must be a string.",
              };
            }
            try {
              return await mutateTaskRawMarkdownWrite(id, body);
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(args as { task_id?: string; body?: string } | undefined);
        return jsonResult(envelope);
      }),
  );
}

// task_commit_record — DELETED (async-cron-jobs Stage 6). Use cron_task_commit_record_enqueue.
// stage_verification_flip — DELETED (async-cron-jobs Stage 6). Use cron_stage_verification_flip_enqueue.

// ---------------------------------------------------------------------------
// stage_closeout_apply
// ---------------------------------------------------------------------------

export function registerStageCloseoutApply(server: McpServer): void {
  server.registerTool(
    "stage_closeout_apply",
    {
      description:
        "DB-backed: close a stage. Guard — every task in the stage must already be `done` or `archived`. Flips all `done` → `archived` (stamps archived_at) and sets stage status → done. Rejects with invalid_input if any non-terminal tasks remain.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug."),
        stage_id: z.string().describe("Stage id."),
      },
    },
    async (args) =>
      runWithToolTiming("stage_closeout_apply", async () => {
        const envelope = await wrapTool(
          async (
            input: { slug?: string; stage_id?: string } | undefined,
          ) => {
            const slug = (input?.slug ?? "").trim();
            const stage_id = (input?.stage_id ?? "").trim();
            if (!slug || !stage_id) {
              throw {
                code: "invalid_input",
                message: "slug and stage_id are required.",
              };
            }
            try {
              return await mutateStageCloseoutApply(slug, stage_id);
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(args as { slug?: string; stage_id?: string } | undefined);
        return jsonResult(envelope);
      }),
  );
}

// journal_append — DELETED (async-cron-jobs Stage 6). Use cron_journal_append_enqueue.

// ---------------------------------------------------------------------------
// fix_plan_write
// ---------------------------------------------------------------------------

export function registerFixPlanWrite(server: McpServer): void {
  server.registerTool(
    "fix_plan_write",
    {
      description:
        "DB-backed: write fix-plan tuples for (task_id, round). Deletes prior unapplied tuples for same key first (rewrite semantics), then inserts new tuples with increasing tuple_index. Applied tuples (applied_at IS NOT NULL) are preserved for 30-day TTL soft-delete.",
      inputSchema: {
        task_id: z.string().describe("Task id e.g. TECH-776."),
        round: z
          .number()
          .int()
          .nonnegative()
          .describe("Fix round (0, 1, 2…)."),
        tuples: z
          .array(z.record(z.string(), z.unknown()))
          .min(1)
          .describe("Non-empty array of tuple objects (jsonb each)."),
      },
    },
    async (args) =>
      runWithToolTiming("fix_plan_write", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  task_id?: string;
                  round?: number;
                  tuples?: Array<Record<string, unknown>>;
                }
              | undefined,
          ) => {
            const id = (input?.task_id ?? "").trim().toUpperCase();
            const round = input?.round;
            const tuples = input?.tuples;
            if (!id || round === undefined || !Array.isArray(tuples)) {
              throw {
                code: "invalid_input",
                message:
                  "task_id, round (int ≥0), and tuples (non-empty array) are required.",
              };
            }
            try {
              return await mutateFixPlanWrite(id, round, tuples);
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(
          args as
            | {
                task_id?: string;
                round?: number;
                tuples?: Array<Record<string, unknown>>;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// fix_plan_consume
// ---------------------------------------------------------------------------

export function registerFixPlanConsume(server: McpServer): void {
  server.registerTool(
    "fix_plan_consume",
    {
      description:
        "DB-backed: mark all unapplied tuples for (task_id, round) as applied (stamps applied_at=now). Idempotent — re-run returns consumed=0 once all are applied.",
      inputSchema: {
        task_id: z.string().describe("Task id e.g. TECH-776."),
        round: z
          .number()
          .int()
          .nonnegative()
          .describe("Fix round (must match a prior fix_plan_write)."),
      },
    },
    async (args) =>
      runWithToolTiming("fix_plan_consume", async () => {
        const envelope = await wrapTool(
          async (
            input: { task_id?: string; round?: number } | undefined,
          ) => {
            const id = (input?.task_id ?? "").trim().toUpperCase();
            const round = input?.round;
            if (!id || round === undefined) {
              throw {
                code: "invalid_input",
                message: "task_id and round are required.",
              };
            }
            try {
              return await mutateFixPlanConsume(id, round);
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(args as { task_id?: string; round?: number } | undefined);
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// task_status_flip_batch (ship-protocol Stage 3 / TECH-12642)
// ---------------------------------------------------------------------------

export function registerTaskStatusFlipBatch(server: McpServer): void {
  server.registerTool(
    "task_status_flip_batch",
    {
      description:
        "DB-backed: single-tx batch flip of N tasks belonging to one (slug, stage_id) Stage. Used by ship-cycle (Sonnet stage-atomic batch) to atomically flip all stage tasks pending→implemented (or implemented→done). When task_ids omitted → flips ALL non-terminal tasks of the stage. Returns `{flipped: [{task_id, prev_status, new_status}], skipped: [{task_id, reason}]}` where reason ∈ not_found | already_target | not_in_stage. Single Postgres tx with row-level locks (SELECT FOR UPDATE). Stamps completed_at when status=done, archived_at when status=archived. Schema-cache restart required after adding this tool (N4).",
      inputSchema: {
        slug: z.string().describe("Master-plan slug e.g. 'ship-protocol'."),
        stage_id: z.string().describe("Stage id e.g. '3'."),
        new_status: STATUS_ENUM.describe(
          "Target status: pending|implemented|verified|done|archived.",
        ),
        task_ids: z
          .array(z.string())
          .optional()
          .describe(
            "Subset filter. Omit → all non-terminal tasks of the stage.",
          ),
      },
    },
    async (args) =>
      runWithToolTiming("task_status_flip_batch", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  slug?: string;
                  stage_id?: string;
                  new_status?:
                    | "pending"
                    | "implemented"
                    | "verified"
                    | "done"
                    | "archived";
                  task_ids?: string[];
                }
              | undefined,
          ) => {
            const slug = (input?.slug ?? "").trim();
            const stage_id = (input?.stage_id ?? "").trim();
            const ns = input?.new_status;
            if (!slug || !stage_id || !ns) {
              throw {
                code: "invalid_input",
                message: "slug, stage_id, new_status are required.",
              };
            }
            try {
              return await mutateTaskStatusFlipBatch({
                slug,
                stage_id,
                new_status: ns,
                task_ids: input?.task_ids,
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
                new_status?:
                  | "pending"
                  | "implemented"
                  | "verified"
                  | "done"
                  | "archived";
                task_ids?: string[];
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// Bucket registrar.
// ---------------------------------------------------------------------------

/** Register all 11 DB-backed write tools on the IA core bucket. */
// ---------------------------------------------------------------------------
// master_plan_close
// (ship-protocol Stage 4 / TECH-12644 — flips ia_master_plans.closed_at = now().
// Idempotent: when closed_at already set, returns already_closed=true with the
// existing timestamp; never errors. Must precede master_plan_version_create.)
// ---------------------------------------------------------------------------

export function registerMasterPlanClose(server: McpServer): void {
  server.registerTool(
    "master_plan_close",
    {
      description:
        "DB-backed: flip ia_master_plans.closed_at = now() for `slug`. Idempotent — returns `already_closed=true` when closed_at is already non-null. Used by ship-final Phase 6 (after assert-stages-done + cumulative validate:all + git tag). MUST precede the journal_append(payload_kind='version_close') row for audit ordering.",
      inputSchema: {
        slug: z
          .string()
          .describe("Master-plan slug (matches ia_master_plans.slug)."),
      },
    },
    async (args) =>
      runWithToolTiming("master_plan_close", async () => {
        const envelope = await wrapTool(
          async (input: { slug?: string } | undefined) => {
            const slug = (input?.slug ?? "").trim();
            if (!slug) {
              throw { code: "invalid_input", message: "slug is required." };
            }
            try {
              return await mutateMasterPlanClose(slug);
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(args as { slug?: string } | undefined);
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// master_plan_version_create
// (ship-protocol Stage 4 / TECH-12644 — emit v(N+1) row chained to a closed
// parent. Fails with `parent_not_closed` when parent.closed_at IS NULL or
// `slug_collision` when child slug already exists.)
// ---------------------------------------------------------------------------

export function registerMasterPlanVersionCreate(server: McpServer): void {
  server.registerTool(
    "master_plan_version_create",
    {
      description:
        "DB-backed: emit a new ia_master_plans row with parent_plan_slug=`parent_slug`, version=parent.version+1, closed_at=NULL. Fails with code `parent_not_closed` when parent.closed_at is NULL (run ship-final first). Defaults child_slug to `{parent}-v{N+1}` and title to parent.title when omitted. Use to start a new version cycle after ship-final closes the prior version.",
      inputSchema: {
        parent_slug: z
          .string()
          .describe("Closed parent master-plan slug."),
        child_slug: z
          .string()
          .optional()
          .describe(
            "Optional override slug for the new row. Defaults to `{parent}-v{N+1}`. Must be kebab-case [a-z0-9-].",
          ),
        title: z
          .string()
          .optional()
          .describe("Optional override title. Defaults to parent.title."),
      },
    },
    async (args) =>
      runWithToolTiming("master_plan_version_create", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | { parent_slug?: string; child_slug?: string; title?: string }
              | undefined,
          ) => {
            const parent_slug = (input?.parent_slug ?? "").trim();
            if (!parent_slug) {
              throw {
                code: "invalid_input",
                message: "parent_slug is required.",
              };
            }
            try {
              return await mutateMasterPlanVersionCreate({
                parent_slug,
                child_slug: input?.child_slug,
                title: input?.title,
              });
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(
          args as
            | { parent_slug?: string; child_slug?: string; title?: string }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}

export function registerIaDbWriteTools(server: McpServer): void {
  registerTaskInsert(server);
  registerTaskStatusFlip(server);
  registerTaskStatusFlipBatch(server);
  registerTaskSpecSectionWrite(server);
  registerTaskRawMarkdownWrite(server);
  registerTaskDepRegister(server);
  registerStageCloseoutApply(server);
  registerFixPlanWrite(server);
  registerFixPlanConsume(server);
  registerMasterPlanClose(server);
  registerMasterPlanVersionCreate(server);
}
