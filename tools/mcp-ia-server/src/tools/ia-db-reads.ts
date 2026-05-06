/**
 * MCP tools (read-only, DB-backed) — Step 3 of ia-dev-db-refactor.
 *
 * Registers 9 tools on the IA core server bucket:
 *   - task_state, stage_state, master_plan_state
 *   - task_spec_body, task_spec_section, task_spec_search
 *   - stage_bundle, task_bundle
 *   - stage_closeout_diagnose
 *
 * Filesystem is never touched; all reads route through
 * `src/ia-db/queries.ts` → singleton `pg.Pool`.
 *
 * Error shape convention: when the IA DB pool is not configured the tool
 * throws an envelope-typed error `{ code: "ia_db_unavailable" }` (caught by
 * `wrapTool`). Not-found results return `null` on the happy path so callers
 * can distinguish "empty" from "error".
 *
 * Source of truth for decisions: docs/ia-dev-db-refactor-implementation.md
 * §Step 3. After editing this descriptor, restart Claude Code to refresh the
 * in-memory schema cache (N4).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import {
  IaDbUnavailableError,
  getMasterPlanLineage,
  queryMasterPlanState,
  queryStageBundle,
  queryStageBundleCached,
  queryStageCloseoutDiagnose,
  queryStageState,
  queryTaskBody,
  queryTaskBundle,
  queryTaskSection,
  queryTaskSpecSearch,
  queryTaskState,
} from "../ia-db/queries.js";

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

function mapUnavailable(e: unknown): never {
  if (e instanceof IaDbUnavailableError) {
    throw {
      code: e.code,
      message: e.message,
      hint: "Set DATABASE_URL or config/postgres-dev.json and restart the MCP server.",
    };
  }
  throw e;
}

// ---------------------------------------------------------------------------
// task_state
// ---------------------------------------------------------------------------

export function registerTaskState(server: McpServer): void {
  server.registerTool(
    "task_state",
    {
      description:
        "DB-backed: one task row + deps (depends_on / related) + cited-id status map + commit history. Canonical replacement for `backlog_issue` once Step 9 retires yaml. Call first when starting work on TECH-XX / FEAT-XX / BUG-XX under the new DB surface.",
      inputSchema: {
        task_id: z
          .string()
          .describe("Task id e.g. TECH-776, FEAT-37b (case-normalized)."),
      },
    },
    async (args) =>
      runWithToolTiming("task_state", async () => {
        const envelope = await wrapTool(
          async (input: { task_id?: string } | undefined) => {
            const id = (input?.task_id ?? "").trim().toUpperCase();
            if (!id) throw { code: "invalid_input", message: "task_id is required." };
            try {
              const row = await queryTaskState(id);
              if (!row) {
                throw {
                  code: "task_not_found",
                  message: `No task '${id}' in ia_tasks.`,
                  hint: "Run `npm run ia:db-import` to re-sync from filesystem.",
                };
              }
              return row;
            } catch (e) {
              mapUnavailable(e);
            }
          },
        )(args as { task_id?: string } | undefined);
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// stage_state
// ---------------------------------------------------------------------------

export function registerStageState(server: McpServer): void {
  server.registerTool(
    "stage_state",
    {
      description:
        "DB-backed: one stage row + its tasks (id, title, status) + status counts + next_pending + latest verification verdict. Supersedes filesystem stage introspection for active work.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug e.g. `sprite-gen`."),
        stage_id: z.string().describe("Stage id e.g. `7` or `3.1` or `6 addendum`."),
      },
    },
    async (args) =>
      runWithToolTiming("stage_state", async () => {
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
              const row = await queryStageState(slug, stage_id);
              if (!row) {
                throw {
                  code: "stage_not_found",
                  message: `No stage '${slug}/${stage_id}' in ia_stages.`,
                };
              }
              return row;
            } catch (e) {
              mapUnavailable(e);
            }
          },
        )(args as { slug?: string; stage_id?: string } | undefined);
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// master_plan_state
// ---------------------------------------------------------------------------

export function registerMasterPlanState(server: McpServer): void {
  server.registerTool(
    "master_plan_state",
    {
      description:
        "DB-backed: one master plan + all its stages with per-status task counts. Rollup view for a `{slug}-master-plan.md` equivalent. Use when orienting to a plan's overall progress.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug e.g. `unity-agent-bridge`."),
      },
    },
    async (args) =>
      runWithToolTiming("master_plan_state", async () => {
        const envelope = await wrapTool(
          async (input: { slug?: string } | undefined) => {
            const slug = (input?.slug ?? "").trim();
            if (!slug) {
              throw { code: "invalid_input", message: "slug is required." };
            }
            try {
              const row = await queryMasterPlanState(slug);
              if (!row) {
                throw {
                  code: "master_plan_not_found",
                  message: `No master plan '${slug}' in ia_master_plans.`,
                };
              }
              return row;
            } catch (e) {
              mapUnavailable(e);
            }
          },
        )(args as { slug?: string } | undefined);
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// task_spec_body
// ---------------------------------------------------------------------------

export function registerTaskSpecBody(server: McpServer): void {
  server.registerTool(
    "task_spec_body",
    {
      description:
        "DB-backed: full markdown body of a task spec (replaces reading `ia/projects/{id}.md` from disk). Prefer `task_spec_section` when a single section suffices.",
      inputSchema: {
        task_id: z.string().describe("Task id e.g. TECH-776."),
      },
    },
    async (args) =>
      runWithToolTiming("task_spec_body", async () => {
        const envelope = await wrapTool(
          async (input: { task_id?: string } | undefined) => {
            const id = (input?.task_id ?? "").trim().toUpperCase();
            if (!id) throw { code: "invalid_input", message: "task_id is required." };
            try {
              const body = await queryTaskBody(id);
              if (body === null) {
                throw {
                  code: "task_not_found",
                  message: `No task '${id}' in ia_tasks.`,
                };
              }
              return { task_id: id, body, bytes: Buffer.byteLength(body, "utf8") };
            } catch (e) {
              mapUnavailable(e);
            }
          },
        )(args as { task_id?: string } | undefined);
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// task_spec_section
// ---------------------------------------------------------------------------

export function registerTaskSpecSection(server: McpServer): void {
  server.registerTool(
    "task_spec_section",
    {
      description:
        "DB-backed: extract one section of a task body by heading text (case-insensitive, trimmed). Returns `{heading, level, content}` spanning the heading through the line before the next heading of the same or shallower level.",
      inputSchema: {
        task_id: z.string().describe("Task id e.g. TECH-776."),
        section: z
          .string()
          .describe("Heading text e.g. 'Acceptance Criteria', 'Plan Digest'."),
      },
    },
    async (args) =>
      runWithToolTiming("task_spec_section", async () => {
        const envelope = await wrapTool(
          async (
            input: { task_id?: string; section?: string } | undefined,
          ) => {
            const id = (input?.task_id ?? "").trim().toUpperCase();
            const section = (input?.section ?? "").trim();
            if (!id || !section) {
              throw {
                code: "invalid_input",
                message: "task_id and section are required.",
              };
            }
            try {
              const sec = await queryTaskSection(id, section);
              if (!sec) {
                throw {
                  code: "section_not_found",
                  message: `Section '${section}' not found in task '${id}'.`,
                };
              }
              return { task_id: id, ...sec };
            } catch (e) {
              mapUnavailable(e);
            }
          },
        )(args as { task_id?: string; section?: string } | undefined);
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// task_spec_search
// ---------------------------------------------------------------------------

export function registerTaskSpecSearch(server: McpServer): void {
  server.registerTool(
    "task_spec_search",
    {
      description:
        "DB-backed search over ia_tasks. `fts` (default) = full-text over body with ts_rank + ts_headline snippet. `trgm` = fuzzy similarity over title (better for id/name typos; body trgm is near-zero at default thresholds). Optional status filter narrows to e.g. `pending`.",
      inputSchema: {
        query: z.string().describe("Search phrase (plain text; no tsquery syntax)."),
        kind: z
          .enum(["fts", "trgm"])
          .optional()
          .describe("Search mode: `fts` (default, stemmed) or `trgm` (fuzzy)."),
        limit: z
          .number()
          .int()
          .optional()
          .describe("Max hits (1–200, default 20)."),
        status: z
          .string()
          .optional()
          .describe("Optional task_status filter: pending|implemented|verified|done|archived."),
      },
    },
    async (args) =>
      runWithToolTiming("task_spec_search", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  query?: string;
                  kind?: "fts" | "trgm";
                  limit?: number;
                  status?: string;
                }
              | undefined,
          ) => {
            const q = (input?.query ?? "").trim();
            if (!q) throw { code: "invalid_input", message: "query is required." };
            try {
              const hits = await queryTaskSpecSearch(q, {
                kind: input?.kind,
                limit: input?.limit,
                status: input?.status,
              });
              return { query: q, kind: input?.kind ?? "fts", count: hits.length, hits };
            } catch (e) {
              mapUnavailable(e);
            }
          },
        )(
          args as
            | {
                query?: string;
                kind?: "fts" | "trgm";
                limit?: number;
                status?: string;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// stage_bundle
// ---------------------------------------------------------------------------

export function registerStageBundle(server: McpServer): void {
  server.registerTool(
    "stage_bundle",
    {
      description:
        "DB-backed: stage_state payload plus master_plan_title in one call. One-shot context bundle for agents entering a stage.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug."),
        stage_id: z.string().describe("Stage id."),
      },
    },
    async (args) =>
      runWithToolTiming("stage_bundle", async () => {
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
              const row = await queryStageBundleCached(slug, stage_id);
              if (!row) {
                throw {
                  code: "stage_not_found",
                  message: `No stage '${slug}/${stage_id}' in ia_stages.`,
                };
              }
              return row;
            } catch (e) {
              mapUnavailable(e);
            }
          },
        )(args as { slug?: string; stage_id?: string } | undefined);
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// task_bundle
// ---------------------------------------------------------------------------

export function registerTaskBundle(server: McpServer): void {
  server.registerTool(
    "task_bundle",
    {
      description:
        "DB-backed: task_state payload plus its owning stage row + master_plan_title in one call. One-shot context bundle for agents entering a task.",
      inputSchema: {
        task_id: z.string().describe("Task id e.g. TECH-776."),
      },
    },
    async (args) =>
      runWithToolTiming("task_bundle", async () => {
        const envelope = await wrapTool(
          async (input: { task_id?: string } | undefined) => {
            const id = (input?.task_id ?? "").trim().toUpperCase();
            if (!id) throw { code: "invalid_input", message: "task_id is required." };
            try {
              const row = await queryTaskBundle(id);
              if (!row) {
                throw {
                  code: "task_not_found",
                  message: `No task '${id}' in ia_tasks.`,
                };
              }
              return row;
            } catch (e) {
              mapUnavailable(e);
            }
          },
        )(args as { task_id?: string } | undefined);
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// Bucket registrar.
// ---------------------------------------------------------------------------

// ---------------------------------------------------------------------------
// stage_closeout_diagnose (TECH-2975)
// ---------------------------------------------------------------------------

export function registerStageCloseoutDiagnose(server: McpServer): void {
  server.registerTool(
    "stage_closeout_diagnose",
    {
      description:
        "DB-backed read-only: per-step audit trail for one (slug, stage_id) closeout, ordered by ts ASC. Returns `[{step_name, ok, error, ts}]`. Empty array for legacy stages without audit rows (closeouts predating TECH-2975). Sources rows from ia_ship_stage_journal where `payload_kind LIKE 'closeout_step.%'`.",
      inputSchema: {
        slug: z.string().describe("Master-plan slug."),
        stage_id: z.string().describe("Stage id e.g. `1` or `3.1`."),
      },
    },
    async (args) =>
      runWithToolTiming("stage_closeout_diagnose", async () => {
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
              const trail = await queryStageCloseoutDiagnose(slug, stage_id);
              return { slug, stage_id, trail };
            } catch (e) {
              mapUnavailable(e);
            }
          },
        )(args as { slug?: string; stage_id?: string } | undefined);
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// master_plan_lineage (TECH-14103)
// ---------------------------------------------------------------------------

function registerMasterPlanLineage(server: McpServer): void {
  server.registerTool(
    "master_plan_lineage",
    {
      description:
        "DB-backed: return version-ordered lineage rows for a master-plan slug. Returns [{version, parent_plan_slug, created_at, closed_at}] ASC. Requires migration 0066 (version + closed_at columns) and 0073 (backfill column). Used by design-explore --resume mode to compute target_version = existing_max_version + 1. Schema-cache restart required after adding this tool (N4).",
      inputSchema: {
        slug: z.string().describe("Master-plan slug (e.g. 'ship-protocol')."),
      },
    },
    async (args) =>
      runWithToolTiming("master_plan_lineage", async () => {
        const envelope = await wrapTool(
          async (input: { slug?: string } | undefined) => {
            const slug = (input?.slug ?? "").trim();
            if (!slug) {
              throw { code: "invalid_input", message: "slug is required." };
            }
            try {
              const rows = await getMasterPlanLineage(slug);
              return { slug, lineage: rows };
            } catch (e) {
              mapUnavailable(e);
            }
          },
        )(args as { slug?: string } | undefined);
        return jsonResult(envelope);
      }),
  );
}

/** Register all DB-backed read tools on the IA core bucket. */
export function registerIaDbReadTools(server: McpServer): void {
  registerTaskState(server);
  registerStageState(server);
  registerMasterPlanState(server);
  registerTaskSpecBody(server);
  registerTaskSpecSection(server);
  registerTaskSpecSearch(server);
  registerStageBundle(server);
  registerTaskBundle(server);
  registerStageCloseoutDiagnose(server);
  registerMasterPlanLineage(server);
}
