/**
 * MCP sugar tools: thin wrappers around export_cell_chunk and export_sorting_debug bridge kinds.
 * Enqueues via the same Postgres path as unity_bridge_command and waits for a terminal job status.
 *
 * Prefer raw unity_bridge_command for other kinds, mutations, custom timeout tuning, or manual
 * enqueue + unity_bridge_get polling.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool, dbUnconfiguredError } from "../envelope.js";
import {
  enqueueUnityBridgeJob,
  pollUnityBridgeJobUntilTerminal,
  resolveExportSugarTimeoutMs,
  unityBridgeCommandInputSchema,
  UNITY_BRIDGE_TIMEOUT_MS_MAX,
} from "./unity-bridge-command.js";

const exportSugarTimeoutMsSchema = z
  .number()
  .int()
  .min(1000)
  .max(UNITY_BRIDGE_TIMEOUT_MS_MAX)
  .optional()
  .describe(
    "Max wait for Unity to complete the job (ms). Omit to use BRIDGE_TIMEOUT_MS env or 40000 default (agent-led verification policy). Max 120000. On timeout, run `npm run unity:ensure-editor` then retry with a higher value.",
  );

export const unityExportCellChunkInputSchema = z.object({
  origin_x: z.number().int().min(0).default(0).describe("Chunk origin X."),
  origin_y: z.number().int().min(0).default(0).describe("Chunk origin Y."),
  chunk_width: z.number().int().min(1).max(128).default(8).describe("Chunk width (cells)."),
  chunk_height: z.number().int().min(1).max(128).default(8).describe("Chunk height (cells)."),
  timeout_ms: exportSugarTimeoutMsSchema,
  agent_id: z
    .string()
    .optional()
    .describe("Optional audit id stored on agent_bridge_job (e.g. TECH-571)."),
});

export const unityExportSortingDebugInputSchema = z.object({
  seed_cell: z
    .string()
    .optional()
    .describe('Optional Moore center "x,y"; omit for bridge default (selected cell or 0,0).'),
  timeout_ms: exportSugarTimeoutMsSchema,
  agent_id: z.string().optional().describe("Optional audit id stored on agent_bridge_job."),
});

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

function throwBridgeFailure(
  result: {
    ok: false;
    error: string;
    message: string;
    command_id?: string;
    last_output_preview?: string;
  },
): never {
  if (result.error === "timeout") {
    throw {
      code: "bridge_timeout" as const,
      message: result.message,
      hint: "Run `npm run unity:ensure-editor` then retry with timeout_ms 60000.",
      details: {
        command_id: result.command_id,
        last_output_preview: result.last_output_preview ?? "",
      },
    };
  }
  throw {
    code: result.error as string,
    message: result.message,
    details: result.command_id ? { command_id: result.command_id } : undefined,
  };
}

/**
 * Register unity_export_cell_chunk and unity_export_sorting_debug.
 */
export function registerUnityBridgeExportSugarTools(server: McpServer): void {
  server.registerTool(
    "unity_export_cell_chunk",
    {
      description:
        "IDE agent bridge (sugar): run export_cell_chunk with one call — same queue + wait semantics as unity_bridge_command but only chunk bounds + timeout. Returns the completed bridge response JSON (artifact paths, registry metadata). Requires DATABASE_URL, migration 0008, Unity on REPO_ROOT. Prefer raw unity_bridge_command for other kinds, Play Mode / mutation workflows, or when you need to poll unity_bridge_get yourself.",
      inputSchema: unityExportCellChunkInputSchema,
    },
    async (args) =>
      runWithToolTiming("unity_export_cell_chunk", async () => {
        const envelope = await wrapTool(async (raw: z.infer<typeof unityExportCellChunkInputSchema>) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const parsed = unityExportCellChunkInputSchema.parse(raw ?? {});
          const timeout_ms = resolveExportSugarTimeoutMs(parsed.timeout_ms);
          const input = unityBridgeCommandInputSchema.parse({
            kind: "export_cell_chunk",
            origin_x: parsed.origin_x,
            origin_y: parsed.origin_y,
            chunk_width: parsed.chunk_width,
            chunk_height: parsed.chunk_height,
            timeout_ms,
            agent_id: parsed.agent_id,
          });
          const enq = await enqueueUnityBridgeJob(input, pool);
          if (!enq.ok) {
            throwBridgeFailure({
              ok: false,
              error: enq.error,
              message: enq.message,
              command_id: enq.command_id,
            });
          }
          const result = await pollUnityBridgeJobUntilTerminal(enq.command_id, timeout_ms, pool);
          if (!result.ok) throwBridgeFailure(result);
          return result.response;
        })(args ?? {});
        return jsonResult(envelope);
      }),
  );

  server.registerTool(
    "unity_export_sorting_debug",
    {
      description:
        "IDE agent bridge (sugar): run export_sorting_debug with one call — optional seed_cell + timeout. Returns the completed bridge response JSON. Requires DATABASE_URL, migration 0008, Unity on REPO_ROOT. Prefer raw unity_bridge_command for sorting_order_debug, debug_context_bundle, mutations, or custom kinds.",
      inputSchema: unityExportSortingDebugInputSchema,
    },
    async (args) =>
      runWithToolTiming("unity_export_sorting_debug", async () => {
        const envelope = await wrapTool(async (raw: z.infer<typeof unityExportSortingDebugInputSchema>) => {
          const pool = getIaDatabasePool();
          if (!pool) throw dbUnconfiguredError();

          const parsed = unityExportSortingDebugInputSchema.parse(raw ?? {});
          const timeout_ms = resolveExportSugarTimeoutMs(parsed.timeout_ms);
          const input = unityBridgeCommandInputSchema.parse({
            kind: "export_sorting_debug",
            seed_cell: parsed.seed_cell,
            timeout_ms,
            agent_id: parsed.agent_id,
          });
          const enq = await enqueueUnityBridgeJob(input, pool);
          if (!enq.ok) {
            throwBridgeFailure({
              ok: false,
              error: enq.error,
              message: enq.message,
              command_id: enq.command_id,
            });
          }
          const result = await pollUnityBridgeJobUntilTerminal(enq.command_id, timeout_ms, pool);
          if (!result.ok) throwBridgeFailure(result);
          return result.response;
        })(args ?? {});
        return jsonResult(envelope);
      }),
  );
}
