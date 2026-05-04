/**
 * MCP stub tool: red_stage_proof_capture — Stage 1.0 tracer.
 *
 * Returns canned `{status: 'unexpected_pass'}` for the fixed tracer UUID
 * and a rejection envelope when `proof_status === 'unexpected_pass'`.
 * Real test runner spawn deferred to Stage 2.
 *
 * Edit descriptor → restart Claude Code (or tsx tools/mcp-ia-server/src/index.ts)
 * to refresh in-memory schema cache (N4).
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";

// ---------------------------------------------------------------------------
// Tracer UUID — fixed for Stage 1.0 tracer; Stage 2 replaces with real UUIDs
// ---------------------------------------------------------------------------

export const TRACER_UUID = "00000000-0000-4000-8000-000000000001";

// ---------------------------------------------------------------------------
// Input schema
// ---------------------------------------------------------------------------

const inputSchema = z.object({
  slug: z.string().min(1).describe("Master-plan slug."),
  stage_id: z.string().min(1).describe("Stage id (e.g. '1.0')."),
  target_kind: z
    .enum(["tracer_verb", "visibility_delta", "bug_repro", "design_only"])
    .describe("Which visibility category the proof covers."),
  anchor: z
    .string()
    .min(1)
    .describe(
      "Resolved red_test_anchor in one of the 4 canonical grammar forms " +
        "(tracer-verb-test:{path}::{method} | visibility-delta-test:{path}::{method} | " +
        "BUG-NNNN:{path}::{method} | n/a).",
    ),
  proof_artifact_id: z
    .string()
    .uuid()
    .describe("UUID for the proof row. Use TRACER_UUID for Stage 1.0 tracer."),
  command_kind: z
    .enum(["npm-test", "dotnet-test", "unity-testmode-batch"])
    .describe("Allowlisted test runner kind."),
});

// ---------------------------------------------------------------------------
// Tool logic
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

type InputType = z.infer<typeof inputSchema>;

export function runRedStageProofCapture(input: InputType): {
  ok: boolean;
  status?: string;
  proof_artifact_id?: string;
  error?: string;
  payload?: { proof_artifact_id: string };
} {
  // design_only anchor → not_applicable, no runner needed
  if (input.target_kind === "design_only" || input.anchor === "n/a") {
    return {
      ok: true,
      status: "not_applicable",
      proof_artifact_id: input.proof_artifact_id,
    };
  }

  // Stub: canned response for Stage 1.0 tracer — always returns unexpected_pass
  // to exercise the rejection path end-to-end.
  const status = "unexpected_pass";

  if (status === "unexpected_pass") {
    return {
      ok: false,
      error: "unexpected_pass_rejected",
      payload: { proof_artifact_id: input.proof_artifact_id },
    };
  }

  // unreachable in Stage 1.0 stub — Stage 2 replaces with real runner spawn
  return {
    ok: true,
    status: "failed_as_expected",
    proof_artifact_id: input.proof_artifact_id,
  };
}

// ---------------------------------------------------------------------------
// Registration
// ---------------------------------------------------------------------------

export function registerRedStageProofCapture(server: McpServer): void {
  server.registerTool(
    "red_stage_proof_capture",
    {
      title: "red_stage_proof_capture",
      description:
        "Pass A entry gate — capture pre-impl test-run blob into ia_red_stage_proofs. " +
        "Stage 1.0 stub: returns {status: 'unexpected_pass'} for fixed tracer UUID; " +
        "returns ok:false rejection envelope when unexpected_pass detected so callers " +
        "fail-closed at the entry gate. Real runner spawn deferred to Stage 2.",
      inputSchema: inputSchema.shape,
    },
    async (args) =>
      runWithToolTiming("red_stage_proof_capture", async () => {
        const input = inputSchema.parse(args ?? {});
        const result = runRedStageProofCapture(input);
        return jsonResult(result);
      }),
  );
}
