/**
 * Stub for `seams_run` MCP tool calls in recipe-engine tests.
 *
 * Inject via `setMcpInvoker(makeSeamsRunStub(responses))` before running the seam step.
 */

import type { McpInvoker } from "../../src/steps/mcp.js";

export interface SeamsRunStubResponse {
  /** When set, return this as the result payload */
  payload?: unknown;
  /** When set, throw an Error with this message */
  errorMessage?: string;
}

export type StubMode = "happy" | "dispatch_unavailable" | "schema_out_bad" | "invoke_error";

export function makeSeamsRunStub(mode: StubMode, overrideOutput?: unknown): McpInvoker {
  return async (tool: string, _args: Record<string, unknown>) => {
    if (tool !== "seams_run") throw new Error(`Unexpected MCP tool call: ${tool}`);

    switch (mode) {
      case "happy":
        return {
          seam: "align-arch-decision",
          dispatch_mode: "subagent",
          validated: true,
          input: {},
          output: overrideOutput ?? {
            aligned_record: { title: "T", status: "active", body: "B" },
            change_kind: "noop",
            rationale: "No change needed.",
          },
          token_totals: { input_tokens: 10, output_tokens: 5 },
        };
      case "dispatch_unavailable":
        return {
          seam: "align-arch-decision",
          dispatch_mode: "subagent",
          dispatch_unavailable: true,
          input: {},
        };
      case "schema_out_bad":
        return {
          seam: "align-arch-decision",
          dispatch_mode: "subagent",
          validated: false,
          output: { wrong_field: "bad" },
        };
      case "invoke_error":
        throw new Error("MCP invoke failed");
    }
  };
}
