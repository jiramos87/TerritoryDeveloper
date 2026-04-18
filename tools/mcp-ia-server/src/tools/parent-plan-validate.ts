/**
 * MCP tool: parent_plan_validate — validate parent_plan / task_key locators
 * across all backlog yaml records against the discovered master-plan files.
 *
 * Delegates to the shared core `validateParentPlanLocator` (TECH-406).
 *
 * NOTE: After editing this descriptor, restart Claude Code (or run
 * `tsx tools/mcp-ia-server/src/index.ts` script) to refresh the in-memory
 * schema cache — the MCP server caches tool descriptors at session start.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { validateParentPlanLocator } from "../parser/parent-plan-validator.js";
import { runWithToolTiming } from "../instrumentation.js";

// ---------------------------------------------------------------------------
// Canonical defaults — mirror tools/validate-parent-plan-locator.mjs
// ---------------------------------------------------------------------------

const YAML_DIRS = ["ia/backlog", "ia/backlog-archive"];
const PLAN_GLOB = "ia/projects/*master-plan*.md";

// ---------------------------------------------------------------------------
// Input schema
// ---------------------------------------------------------------------------

const inputShape = {
  strict: z
    .boolean()
    .optional()
    .default(false)
    .describe(
      "When true, warnings are promoted to errors and exit_code reflects them. Default false (advisory mode).",
    ),
};

// ---------------------------------------------------------------------------
// Local helpers
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
// Registration
// ---------------------------------------------------------------------------

/**
 * Register the parent_plan_validate tool.
 *
 * Validates that every backlog yaml record with a `parent_plan` field points to
 * an existing master-plan file, carries a well-formed `task_key`, has a
 * matching row in that plan, and that the row back-references the yaml id.
 *
 * Edit descriptor → restart Claude Code (or `tsx tools/mcp-ia-server/src/index.ts`
 * script) to refresh in-memory schema cache.
 */
export function registerParentPlanValidate(server: McpServer): void {
  server.registerTool(
    "parent_plan_validate",
    {
      description:
        "Validate parent_plan / task_key locators across all backlog yaml records. " +
        "Checks: (a) parent_plan path resolves on disk, (b) task_key matches regex, " +
        "(c) task_key found as a row in the plan, (d) plan row back-references the yaml id. " +
        "Returns { errors, warnings, exit_code }. " +
        "Edit descriptor → restart Claude Code (or `tsx tools/mcp-ia-server/src/index.ts` script) " +
        "to refresh in-memory schema cache (N4).",
      inputSchema: inputShape,
    },
    async (args) =>
      runWithToolTiming("parent_plan_validate", async () => {
        const strict = (args?.strict ?? false) as boolean;
        const repoRoot = process.cwd();
        const result = validateParentPlanLocator({
          repoRoot,
          yamlDirs: YAML_DIRS,
          planGlob: PLAN_GLOB,
          strict,
        });
        return jsonResult(result);
      }),
  );
}
