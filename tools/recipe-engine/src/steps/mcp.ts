/**
 * mcp step — Phase B MVP stub.
 *
 * Real MCP client dispatch lives inside the territory-ia MCP server process.
 * The recipe engine runs as a Node CLI (Phase B) and as a subagent body
 * (Phase C). In CLI mode the engine has no MCP client; in subagent mode the
 * subagent already has tool access and would intercept these steps before the
 * engine sees them.
 *
 * Phase B contract:
 *   - dry_run mode: returns { tool, args } so recipes can be plan-printed.
 *   - live mode: returns ok:false with code "phase_b_no_mcp_client" — recipes
 *     that depend on MCP must run inside a subagent (Phase C wiring).
 *
 * Phase C will replace this with a tool-callback injected by the subagent
 * runtime (engine accepts { mcpInvoke?: (tool, args) => Promise<result> }).
 */

import type { McpStep, RunContext, StepResult } from "../types.js";
import { resolveTree } from "../template.js";

export type McpInvoker = (tool: string, args: Record<string, unknown>) => Promise<unknown>;

let injectedInvoker: McpInvoker | undefined;

export function setMcpInvoker(invoker: McpInvoker | undefined): void {
  injectedInvoker = invoker;
}

export async function runMcpStep(step: McpStep, ctx: RunContext): Promise<StepResult> {
  const tool = String(resolveTree(ctx.vars, step.mcp));
  const args = (resolveTree(ctx.vars, step.args ?? {}) as Record<string, unknown>) ?? {};

  if (ctx.dry_run) {
    return { ok: true, value: { dry_run: true, tool, args } };
  }

  if (!injectedInvoker) {
    return {
      ok: false,
      error: {
        code: "phase_b_no_mcp_client",
        message:
          `mcp.${tool} step requires an MCP client. Phase B CLI mode has no client; ` +
          `inject one via setMcpInvoker() before calling runRecipe (Phase C subagent runtime does this).`,
      },
    };
  }

  try {
    const result = await injectedInvoker(tool, args);
    return { ok: true, value: result };
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    return { ok: false, error: { code: "mcp_error", message: msg, details: err } };
  }
}
