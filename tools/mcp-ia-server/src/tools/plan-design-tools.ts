/**
 * MCP tools — ia_plan_designs CRUD (mig 0158).
 *
 * DB-primary design seed surface. Replaces the YAML frontmatter at
 * `docs/explorations/{slug}.md` as the source of truth for the
 * design-explore → ship-plan handoff.
 *
 * Registers 5 tools on the IA core server bucket:
 *   - plan_design_insert   — design-explore Phase 0 reserves draft row
 *   - plan_design_update   — design-explore Phase 4 finalizes (status=ready)
 *   - plan_design_get      — ship-plan Phase A.0 reads priority + design_id
 *   - plan_design_list     — triage view (sorted by priority asc, updated_at desc)
 *   - plan_design_promote  — manual flip to consumed + optional FK link
 *
 * Schema-cache restart required after adding this file: restart Claude Code
 * or reload the MCP host (server caches descriptors at session start — N4).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { IaDbUnavailableError } from "../ia-db/queries.js";
import {
  IaDbValidationError,
  mutatePlanDesignInsert,
  mutatePlanDesignUpdate,
  mutatePlanDesignPromote,
  queryPlanDesignGet,
  queryPlanDesignList,
} from "../ia-db/mutations.js";

// ---------------------------------------------------------------------------
// Shared helpers
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
    throw { code: e.code, message: e.message };
  }
  if (e instanceof IaDbValidationError) {
    throw { code: e.code, message: e.message };
  }
  if (e && typeof e === "object" && "code" in e) {
    const code = String((e as { code: unknown }).code);
    const message =
      e instanceof Error
        ? e.message
        : String((e as { message?: unknown }).message ?? e);
    throw { code: `pg_${code}`, message };
  }
  const msg = e instanceof Error ? e.message : String(e);
  throw { code: "db_error", message: msg };
}

// ---------------------------------------------------------------------------
// plan_design_insert
// ---------------------------------------------------------------------------

export function registerPlanDesignInsert(server: McpServer): void {
  server.registerTool(
    "plan_design_insert",
    {
      description:
        "DB-backed: create one new ia_plan_designs row (slug + title + optional priority + optional parent_plan_slug + optional target_version). " +
        "Used by design-explore Phase 0 to reserve a draft seed row before the exploration runs. " +
        "Status starts as 'draft'. Errors on duplicate slug (slug_collision). " +
        "Slug must be kebab-case. Priority defaults to P2.",
      inputSchema: {
        slug: z
          .string()
          .describe(
            "Final master-plan slug (kebab-case). Will own the slug used by ship-plan.",
          ),
        title: z.string().describe("Plan title (display heading)."),
        priority: z
          .enum(["P0", "P1", "P2", "P3"])
          .optional()
          .describe("Triage priority. Default P2."),
        parent_plan_slug: z
          .string()
          .nullable()
          .optional()
          .describe(
            "Parent master-plan slug when this seed is an extension/version-bump.",
          ),
        target_version: z
          .number()
          .int()
          .optional()
          .describe("Target master-plan version. Default 1."),
      },
    },
    async (args) =>
      runWithToolTiming("plan_design_insert", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  slug?: string;
                  title?: string;
                  priority?: "P0" | "P1" | "P2" | "P3";
                  parent_plan_slug?: string | null;
                  target_version?: number;
                }
              | undefined,
          ) => {
            if (!input?.slug || !input?.title) {
              throw {
                code: "invalid_input",
                message: "slug and title are required.",
              };
            }
            try {
              return await mutatePlanDesignInsert({
                slug: input.slug,
                title: input.title,
                priority: input.priority,
                parent_plan_slug: input.parent_plan_slug ?? null,
                target_version: input.target_version,
              });
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(
          args as
            | {
                slug?: string;
                title?: string;
                priority?: "P0" | "P1" | "P2" | "P3";
                parent_plan_slug?: string | null;
                target_version?: number;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// plan_design_update
// ---------------------------------------------------------------------------

export function registerPlanDesignUpdate(server: McpServer): void {
  server.registerTool(
    "plan_design_update",
    {
      description:
        "DB-backed: update one ia_plan_designs row by slug. " +
        "Used by design-explore Phase 0.5 (priority poll), Phase 4 (status='ready' + body_md + stages_yaml), and manual edits. " +
        "All fields optional; at least one must be provided. Errors when the seed row does not exist.",
      inputSchema: {
        slug: z.string().describe("Seed slug (kebab-case)."),
        title: z.string().optional(),
        priority: z.enum(["P0", "P1", "P2", "P3"]).optional(),
        status: z.enum(["draft", "ready", "consumed", "archived"]).optional(),
        body_md: z
          .string()
          .nullable()
          .optional()
          .describe("Full exploration markdown body (DB source of truth)."),
        stages_yaml: z
          .unknown()
          .nullable()
          .optional()
          .describe(
            "Structured stages[]+tasks[] payload (jsonb). Mirrors handoff YAML shape.",
          ),
        parent_plan_slug: z.string().nullable().optional(),
        target_version: z.number().int().optional(),
      },
    },
    async (args) =>
      runWithToolTiming("plan_design_update", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  slug?: string;
                  title?: string;
                  priority?: "P0" | "P1" | "P2" | "P3";
                  status?: "draft" | "ready" | "consumed" | "archived";
                  body_md?: string | null;
                  stages_yaml?: unknown | null;
                  parent_plan_slug?: string | null;
                  target_version?: number;
                }
              | undefined,
          ) => {
            if (!input?.slug) {
              throw { code: "invalid_input", message: "slug is required." };
            }
            try {
              return await mutatePlanDesignUpdate({
                slug: input.slug,
                title: input.title,
                priority: input.priority,
                status: input.status,
                body_md: input.body_md,
                stages_yaml: input.stages_yaml,
                parent_plan_slug: input.parent_plan_slug,
                target_version: input.target_version,
              });
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(
          args as
            | {
                slug?: string;
                title?: string;
                priority?: "P0" | "P1" | "P2" | "P3";
                status?: "draft" | "ready" | "consumed" | "archived";
                body_md?: string | null;
                stages_yaml?: unknown | null;
                parent_plan_slug?: string | null;
                target_version?: number;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// plan_design_get
// ---------------------------------------------------------------------------

export function registerPlanDesignGet(server: McpServer): void {
  server.registerTool(
    "plan_design_get",
    {
      description:
        "DB-backed: read one ia_plan_designs row by slug. " +
        "Used by ship-plan Phase A.0 to fetch priority + design_id + status before bundle dispatch. " +
        "Returns null when slug is not found.",
      inputSchema: {
        slug: z.string().describe("Seed slug (kebab-case)."),
      },
    },
    async (args) =>
      runWithToolTiming("plan_design_get", async () => {
        const envelope = await wrapTool(
          async (input: { slug?: string } | undefined) => {
            if (!input?.slug) {
              throw { code: "invalid_input", message: "slug is required." };
            }
            try {
              const row = await queryPlanDesignGet(input.slug);
              return { row };
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
// plan_design_list
// ---------------------------------------------------------------------------

export function registerPlanDesignList(server: McpServer): void {
  server.registerTool(
    "plan_design_list",
    {
      description:
        "DB-backed: list ia_plan_designs rows. " +
        "Filterable by status + priority. Ordered by priority asc (P0 first), then updated_at desc. " +
        "Returns up to 500 rows; default limit 100.",
      inputSchema: {
        status: z.enum(["draft", "ready", "consumed", "archived"]).optional(),
        priority: z.enum(["P0", "P1", "P2", "P3"]).optional(),
        limit: z.number().int().min(1).max(500).optional(),
      },
    },
    async (args) =>
      runWithToolTiming("plan_design_list", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | {
                  status?: "draft" | "ready" | "consumed" | "archived";
                  priority?: "P0" | "P1" | "P2" | "P3";
                  limit?: number;
                }
              | undefined,
          ) => {
            try {
              const rows = await queryPlanDesignList({
                status: input?.status,
                priority: input?.priority,
                limit: input?.limit,
              });
              return { rows, count: rows.length };
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(
          args as
            | {
                status?: "draft" | "ready" | "consumed" | "archived";
                priority?: "P0" | "P1" | "P2" | "P3";
                limit?: number;
              }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// plan_design_promote
// ---------------------------------------------------------------------------

export function registerPlanDesignPromote(server: McpServer): void {
  server.registerTool(
    "plan_design_promote",
    {
      description:
        "DB-backed: manual flip seed status='consumed' + optional FK link to ia_master_plans.design_id. " +
        "Normally master_plan_bundle_apply (mig 0159) does this inline; this tool is the out-of-band " +
        "escape hatch for retries, backfills, or manual seed→plan reconciliation. Idempotent.",
      inputSchema: {
        slug: z.string().describe("Seed slug (kebab-case)."),
        master_plan_slug: z
          .string()
          .nullable()
          .optional()
          .describe(
            "Optional master-plan slug. When provided, sets ia_master_plans.design_id FK on that row.",
          ),
      },
    },
    async (args) =>
      runWithToolTiming("plan_design_promote", async () => {
        const envelope = await wrapTool(
          async (
            input:
              | { slug?: string; master_plan_slug?: string | null }
              | undefined,
          ) => {
            if (!input?.slug) {
              throw { code: "invalid_input", message: "slug is required." };
            }
            try {
              return await mutatePlanDesignPromote(
                input.slug,
                input.master_plan_slug ?? null,
              );
            } catch (e) {
              mapDbErrors(e);
            }
          },
        )(
          args as
            | { slug?: string; master_plan_slug?: string | null }
            | undefined,
        );
        return jsonResult(envelope);
      }),
  );
}

// ---------------------------------------------------------------------------
// Bucket registrar
// ---------------------------------------------------------------------------

export function registerPlanDesignTools(server: McpServer): void {
  registerPlanDesignInsert(server);
  registerPlanDesignUpdate(server);
  registerPlanDesignGet(server);
  registerPlanDesignList(server);
  registerPlanDesignPromote(server);
}
