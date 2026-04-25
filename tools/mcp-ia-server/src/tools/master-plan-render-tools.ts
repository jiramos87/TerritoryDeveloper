/**
 * MCP tools — master-plan + stage RENDER + change-log surfaces.
 *
 * Step 9.6.8 of ia-dev-db-refactor (Option A — full DB pivot).
 *
 * Replaces filesystem reads/writes against `ia/projects/{slug}/index.md` and
 * `ia/projects/{slug}/stage-X.Y-*.md` with structured DB-backed render +
 * write surfaces. After Step 9.6.11 those folders are deleted entirely.
 *
 * Registers 4 tools on the IA core server bucket:
 *   - master_plan_render          — read: preamble + per-stage rendered blocks
 *   - stage_render                — read: one stage rendered as markdown
 *   - master_plan_preamble_write  — write: UPDATE preamble + optional changelog
 *   - master_plan_change_log_append — write: INSERT change log row
 *
 * Source of truth for decisions: docs/ia-dev-db-refactor-implementation.md
 * §Step 9.6.8. After editing this descriptor, restart Claude Code to refresh
 * the in-memory schema cache (N4).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import {
  IaDbUnavailableError,
  queryMasterPlanRender,
  queryStageRender,
} from "../ia-db/queries.js";
import {
  IaDbValidationError,
  mutateMasterPlanChangeLogAppend,
  mutateMasterPlanInsert,
  mutateMasterPlanPreambleWrite,
  mutateStageBodyWrite,
  mutateStageInsert,
  mutateStageUpdate,
} from "../ia-db/mutations.js";

// ---------------------------------------------------------------------------
// Shared helpers (mirrors ia-db-reads / ia-db-writes pattern).
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

// ---------------------------------------------------------------------------
// master_plan_render
// ---------------------------------------------------------------------------

export function registerMasterPlanRender(server: McpServer): void {
  server.registerTool(
    "master_plan_render",
    {
      description:
        "DB-backed: return master plan preamble (verbatim) + per-stage rendered markdown blocks (heading + status + objective + exit + tasks). Replaces filesystem reads of `ia/projects/{slug}/index.md` and `stage-X.Y-*.md`. Optional `include_change_log` adds latest N change log entries (DESC by ts).",
      inputSchema: {
        slug: z.string().describe("Master-plan slug e.g. `multi-scale`."),
        include_change_log: z
          .boolean()
          .optional()
          .describe("When true, include latest N change log entries."),
        change_log_limit: z
          .number()
          .int()
          .positive()
          .optional()
          .describe("Cap on change log entries (1–500, default 50)."),
      },
    },
    async (args) =>
      runWithToolTiming("master_plan_render", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  slug?: string;
                  include_change_log?: boolean;
                  change_log_limit?: number;
                }
              | undefined,
          ) => {
            const slug = (input?.slug ?? "").trim();
            if (!slug) {
              throw { code: "invalid_input", message: "slug is required." };
            }
            try {
              const row = await queryMasterPlanRender(slug, {
                include_change_log: input?.include_change_log,
                change_log_limit: input?.change_log_limit,
              });
              if (!row) {
                throw {
                  code: "master_plan_not_found",
                  message: `No master plan '${slug}' in ia_master_plans.`,
                };
              }
              return row;
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(
          args as
            | {
                slug?: string;
                include_change_log?: boolean;
                change_log_limit?: number;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// stage_render
// ---------------------------------------------------------------------------

export function registerStageRender(server: McpServer): void {
  server.registerTool(
    "stage_render",
    {
      description:
        "DB-backed: render one stage block as markdown (heading + status + objective + exit + tasks table) plus structured fields. Replaces filesystem reads of `ia/projects/{slug}/stage-X.Y-*.md`.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug."),
        stage_id: z.string().describe("Stage id e.g. `7` or `3.1`."),
      },
    },
    async (args) =>
      runWithToolTiming("stage_render", async () => {
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
              const row = await queryStageRender(slug, stage_id);
              if (!row) {
                throw {
                  code: "stage_not_found",
                  message: `No stage '${slug}/${stage_id}' in ia_stages.`,
                };
              }
              return row;
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(args as { slug?: string; stage_id?: string } | undefined);
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// master_plan_preamble_write
// ---------------------------------------------------------------------------

export function registerMasterPlanPreambleWrite(server: McpServer): void {
  server.registerTool(
    "master_plan_preamble_write",
    {
      description:
        "DB-backed: replace the verbatim preamble blob for a master plan (everything-above-`## Stages`). Optional `change_log` arg appends a structured history row in the same tx. Replaces direct edits to `ia/projects/{slug}/index.md` (retired Step 9.6.11).",
      inputSchema: {
        slug: z.string().describe("Master-plan slug."),
        preamble: z
          .string()
          .describe(
            "Full preamble markdown (title heading, status, scope, vision pointers, hierarchy, sibling orchestrators, etc.).",
          ),
        change_log: z
          .object({
            kind: z
              .string()
              .describe("Short tag e.g. `preamble-rewrite`, `closeout-digest`."),
            body: z.string().describe("Markdown body of the change-log entry."),
            actor: z.string().optional().describe("Who performed the edit."),
            commit_sha: z.string().optional().describe("Commit sha (optional)."),
          })
          .optional()
          .describe(
            "Optional change-log row appended in the same tx for audit trail.",
          ),
      },
    },
    async (args) =>
      runWithToolTiming("master_plan_preamble_write", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  slug?: string;
                  preamble?: string;
                  change_log?: {
                    kind?: string;
                    body?: string;
                    actor?: string;
                    commit_sha?: string;
                  };
                }
              | undefined,
          ) => {
            const slug = (input?.slug ?? "").trim();
            const preamble = input?.preamble;
            if (!slug || typeof preamble !== "string") {
              throw {
                code: "invalid_input",
                message: "slug and preamble are required.",
              };
            }
            const cl = input?.change_log;
            const clArg =
              cl && cl.kind && cl.body
                ? {
                    kind: cl.kind,
                    body: cl.body,
                    actor: cl.actor ?? null,
                    commit_sha: cl.commit_sha ?? null,
                  }
                : null;
            try {
              return await mutateMasterPlanPreambleWrite(slug, preamble, clArg);
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(
          args as
            | {
                slug?: string;
                preamble?: string;
                change_log?: {
                  kind?: string;
                  body?: string;
                  actor?: string;
                  commit_sha?: string;
                };
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// master_plan_change_log_append
// ---------------------------------------------------------------------------

export function registerMasterPlanChangeLogAppend(server: McpServer): void {
  server.registerTool(
    "master_plan_change_log_append",
    {
      description:
        "DB-backed: append one append-only history row to ia_master_plan_change_log. Replaces manual edits to the `Change log` sections previously scattered through index.md / stage-*.md files.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug."),
        kind: z
          .string()
          .describe(
            "Short tag e.g. `closeout-digest`, `sha-backfill`, `status-flip`, `retired-skill`.",
          ),
        body: z.string().describe("Markdown body of the entry."),
        actor: z.string().optional().describe("Who recorded the entry."),
        commit_sha: z.string().optional().describe("Commit sha (optional)."),
      },
    },
    async (args) =>
      runWithToolTiming("master_plan_change_log_append", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  slug?: string;
                  kind?: string;
                  body?: string;
                  actor?: string;
                  commit_sha?: string;
                }
              | undefined,
          ) => {
            const slug = (input?.slug ?? "").trim();
            const kind = (input?.kind ?? "").trim();
            const body = input?.body ?? "";
            if (!slug || !kind || !body) {
              throw {
                code: "invalid_input",
                message: "slug, kind, and body are required.",
              };
            }
            try {
              return await mutateMasterPlanChangeLogAppend(slug, kind, body, {
                actor: input?.actor ?? null,
                commit_sha: input?.commit_sha ?? null,
              });
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(
          args as
            | {
                slug?: string;
                kind?: string;
                body?: string;
                actor?: string;
                commit_sha?: string;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// master_plan_insert
// ---------------------------------------------------------------------------

export function registerMasterPlanInsert(server: McpServer): void {
  server.registerTool(
    "master_plan_insert",
    {
      description:
        "DB-backed: create one new ia_master_plans row (slug + title + optional preamble). Used by master-plan-new at orchestrator authoring time. Errors on duplicate slug. Slug must be kebab-case.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug (kebab-case)."),
        title: z.string().describe("Master-plan title (display heading)."),
        preamble: z
          .string()
          .optional()
          .describe("Optional initial preamble markdown."),
      },
    },
    async (args) =>
      runWithToolTiming("master_plan_insert", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | { slug?: string; title?: string; preamble?: string }
              | undefined,
          ) => {
            const slug = (input?.slug ?? "").trim();
            const title = (input?.title ?? "").trim();
            if (!slug || !title) {
              throw {
                code: "invalid_input",
                message: "slug and title are required.",
              };
            }
            try {
              return await mutateMasterPlanInsert(
                slug,
                title,
                input?.preamble ?? null,
              );
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(
          args as
            | { slug?: string; title?: string; preamble?: string }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// stage_insert
// ---------------------------------------------------------------------------

export function registerStageInsert(server: McpServer): void {
  server.registerTool(
    "stage_insert",
    {
      description:
        "DB-backed: create one new ia_stages row under an existing master plan. Stage_id must match N or N.M (e.g. `5` or `5.4`). Title/objective/exit_criteria/body optional at insert; back-fill via stage_update + stage_body_write. `body` carries the full canonical Stage block markdown (see `docs/MASTER-PLAN-STRUCTURE.md`).",
      inputSchema: {
        slug: z.string().describe("Master-plan slug (must exist)."),
        stage_id: z.string().describe("Stage id e.g. `5` or `5.4`."),
        title: z.string().optional().describe("Stage title."),
        objective: z.string().optional().describe("Stage objective prose."),
        exit_criteria: z
          .string()
          .optional()
          .describe("Stage exit criteria prose."),
        body: z
          .string()
          .optional()
          .describe(
            "Optional full Stage block markdown blob (canonical shape per `docs/MASTER-PLAN-STRUCTURE.md`).",
          ),
        status: z
          .enum(["pending", "in_progress", "done"])
          .optional()
          .describe("Initial status (default `pending`)."),
      },
    },
    async (args) =>
      runWithToolTiming("stage_insert", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  slug?: string;
                  stage_id?: string;
                  title?: string;
                  objective?: string;
                  exit_criteria?: string;
                  body?: string;
                  status?: "pending" | "in_progress" | "done";
                }
              | undefined,
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
              return await mutateStageInsert({
                slug,
                stage_id,
                title: input?.title ?? null,
                objective: input?.objective ?? null,
                exit_criteria: input?.exit_criteria ?? null,
                body: input?.body ?? null,
                status: input?.status,
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
                title?: string;
                objective?: string;
                exit_criteria?: string;
                body?: string;
                status?: "pending" | "in_progress" | "done";
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// stage_update
// ---------------------------------------------------------------------------

export function registerStageUpdate(server: McpServer): void {
  server.registerTool(
    "stage_update",
    {
      description:
        "DB-backed: update structured stage fields (title / objective / exit_criteria) on an existing ia_stages row. Pass any subset; pass null to clear a field. Status transitions go through task_status_flip + stage_closeout_apply, not this tool.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug."),
        stage_id: z.string().describe("Stage id e.g. `5.4`."),
        title: z
          .string()
          .nullable()
          .optional()
          .describe("Stage title (null clears)."),
        objective: z
          .string()
          .nullable()
          .optional()
          .describe("Objective prose (null clears)."),
        exit_criteria: z
          .string()
          .nullable()
          .optional()
          .describe("Exit criteria prose (null clears)."),
      },
    },
    async (args) =>
      runWithToolTiming("stage_update", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  slug?: string;
                  stage_id?: string;
                  title?: string | null;
                  objective?: string | null;
                  exit_criteria?: string | null;
                }
              | undefined,
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
              return await mutateStageUpdate({
                slug,
                stage_id,
                title: input?.title,
                objective: input?.objective,
                exit_criteria: input?.exit_criteria,
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
                title?: string | null;
                objective?: string | null;
                exit_criteria?: string | null;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// stage_body_write
// ---------------------------------------------------------------------------

export function registerStageBodyWrite(server: McpServer): void {
  server.registerTool(
    "stage_body_write",
    {
      description:
        "DB-backed: replace the verbatim Stage block body blob (canonical shape per `docs/MASTER-PLAN-STRUCTURE.md` — Notes / Backlog state / Art / Relevant surfaces / 5-col Task table / §Stage File Plan / §Plan Fix / §Stage Audit / §Stage Closeout Plan). Mirror of `master_plan_preamble_write`. Used by `master-plan-new` Phase 7, `stage-decompose` Phase 4, `master-plan-extend`, `stage_closeout_apply`. Renderer `stage_render` returns body verbatim when non-empty; falls back to structured-field synthesis otherwise.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug."),
        stage_id: z.string().describe("Stage id e.g. `5.4`."),
        body: z
          .string()
          .describe(
            "Full Stage block markdown (everything below the `### Stage X.Y — Name` heading through the last subsection).",
          ),
      },
    },
    async (args) =>
      runWithToolTiming("stage_body_write", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | { slug?: string; stage_id?: string; body?: string }
              | undefined,
          ) => {
            const slug = (input?.slug ?? "").trim();
            const stage_id = (input?.stage_id ?? "").trim();
            const body = input?.body;
            if (!slug || !stage_id || typeof body !== "string") {
              throw {
                code: "invalid_input",
                message: "slug, stage_id, and body are required.",
              };
            }
            try {
              return await mutateStageBodyWrite(slug, stage_id, body);
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(
          args as
            | { slug?: string; stage_id?: string; body?: string }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// Bucket registrar.
// ---------------------------------------------------------------------------

/** Register all master-plan render + change-log + author-side tools on the IA core bucket. */
export function registerMasterPlanRenderTools(server: McpServer): void {
  registerMasterPlanRender(server);
  registerStageRender(server);
  registerMasterPlanPreambleWrite(server);
  registerMasterPlanChangeLogAppend(server);
  registerMasterPlanInsert(server);
  registerStageInsert(server);
  registerStageUpdate(server);
  registerStageBodyWrite(server);
}
