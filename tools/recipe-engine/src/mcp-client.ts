/**
 * Stdio MCP client for recipe-engine CLI mode — DEC-A19 Phase B injector.
 *
 * Spawns the territory-ia MCP server (`tools/mcp-ia-server/src/index.ts`) over
 * stdio and exposes an `McpInvoker` matching `steps/mcp.ts` `setMcpInvoker()`.
 *
 * Subagent (Phase C) runtime intercepts mcp steps before the engine sees them
 * and never spawns a child server — this client is CLI-mode-only.
 *
 * Tool result envelope handling mirrors `tools/mcp-ia-server/scripts/verify-mcp.ts`:
 *   - Single text content block parsed as JSON.
 *   - `wrapTool` happy-path `{ ok:true, payload, meta }` → unwrap to `payload`
 *     (merged with meta when payload is a plain object).
 *   - `wrapTool` error `{ ok:false, error:{ code, ... } }` → throw with code
 *     so the recipe step records `{ ok:false, error:{ code:"mcp_error" } }`.
 *   - Anything else returned as-is.
 */

import path from "node:path";
import { fileURLToPath } from "node:url";
import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StdioClientTransport } from "@modelcontextprotocol/sdk/client/stdio.js";
import type { McpInvoker } from "./steps/mcp.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..", "..");

export interface StdioMcpClient {
  invoke: McpInvoker;
  close: () => Promise<void>;
}

export interface CreateStdioMcpClientOptions {
  cwd?: string;
  env?: NodeJS.ProcessEnv;
}

function unwrapToolResult(raw: { content?: Array<{ type: string; text?: string }> }): unknown {
  const block = raw.content?.find((c) => c.type === "text");
  if (!block?.text) return raw;
  let parsed: unknown;
  try {
    parsed = JSON.parse(block.text);
  } catch {
    return block.text;
  }
  if (parsed === null || typeof parsed !== "object") return parsed;
  if ("ok" in parsed) {
    const obj = parsed as { ok: unknown; payload?: unknown; meta?: unknown; error?: unknown };
    if (obj.ok === true && "payload" in obj) {
      const { payload, meta } = obj;
      if (
        meta &&
        typeof meta === "object" &&
        !Array.isArray(meta) &&
        payload !== null &&
        typeof payload === "object" &&
        !Array.isArray(payload)
      ) {
        return { ...(payload as Record<string, unknown>), ...(meta as Record<string, unknown>) };
      }
      return payload;
    }
    if (obj.ok === false && obj.error && typeof obj.error === "object") {
      const e = obj.error as { code?: string; message?: string; details?: unknown };
      const err = new Error(e.message || `MCP tool error (${e.code ?? "unknown"})`);
      Object.assign(err, { code: e.code, details: e.details });
      throw err;
    }
  }
  return parsed;
}

export async function createStdioMcpClient(
  options: CreateStdioMcpClientOptions = {},
): Promise<StdioMcpClient> {
  const cwd = options.cwd ?? REPO_ROOT;
  const transport = new StdioClientTransport({
    command: "npx",
    args: ["-y", "tsx", "tools/mcp-ia-server/src/index.ts"],
    cwd,
    env: {
      ...process.env,
      ...(options.env ?? {}),
      REPO_ROOT: cwd,
    },
    stderr: "inherit",
  });

  const client = new Client(
    { name: "territory-recipe-engine", version: "0.1.0" },
    { capabilities: {} },
  );

  await client.connect(transport);

  const invoke: McpInvoker = async (tool, args) => {
    const result = await client.callTool({ name: tool, arguments: args });
    return unwrapToolResult(result as { content?: Array<{ type: string; text?: string }> });
  };

  const close = async (): Promise<void> => {
    try {
      await client.close();
    } catch {
      // best-effort shutdown
    }
  };

  return { invoke, close };
}
