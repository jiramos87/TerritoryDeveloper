/**
 * red_stage_proof_* MCP tools — real implementation (Stage 2).
 *
 * Tools:
 *   red_stage_proof_capture  — INSERT a proof row (rejects unexpected_pass)
 *   red_stage_proof_get      — read proof rows for one stage
 *   red_stage_proof_list     — aggregate counts by (stage_id, proof_status) for a slug
 *   red_stage_proof_finalize — flip green_status after green-stage run
 */

import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { getIaDatabasePool } from "../ia-db/pool.js";
import { RedStageProofError } from "./errors.js";
import { parseAnchor } from "./anchor.js";
import {
  RedStageProofCaptureInputSchema,
  RedStageProofGetInputSchema,
  RedStageProofListInputSchema,
  RedStageProofFinalizeInputSchema,
} from "./schema.js";
import { insertProof, getProofsByStage, listProofCountsBySlug, finalizeProof } from "./queries.js";

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

function errorResult(code: string, message: string, details?: unknown) {
  return jsonResult({ ok: false, error: { code, message, details } });
}

// ---------------------------------------------------------------------------
// Capture
// ---------------------------------------------------------------------------

export async function handleRedStageProofCapture(args: unknown) {
  const input = RedStageProofCaptureInputSchema.parse(args);

  if (input.proof_status === "unexpected_pass") {
    return errorResult(
      "unexpected_pass_blocked",
      "proof_status='unexpected_pass' is not capturable — red stage must fail or be not_applicable.",
    );
  }

  try {
    parseAnchor(input.anchor);
  } catch (e) {
    if (e instanceof RedStageProofError) return errorResult(e.code, e.message);
    throw e;
  }

  const pool = getIaDatabasePool();
  if (!pool) return errorResult("slug_stage_unknown", "DB pool unavailable.");

  try {
    const row = await insertProof(pool, input);
    return jsonResult({ ok: true, payload: row });
  } catch (e: unknown) {
    const err = e as { code?: string };
    if (err.code === "23505") return errorResult("anchor_already_captured", `Anchor already captured: ${input.anchor}`);
    if (err.code === "23503") return errorResult("slug_stage_unknown", `No stage found for slug=${input.slug} stage_id=${input.stage_id}`);
    throw e;
  }
}

// ---------------------------------------------------------------------------
// Get
// ---------------------------------------------------------------------------

export async function handleRedStageProofGet(args: unknown) {
  const input = RedStageProofGetInputSchema.parse(args);
  const pool = getIaDatabasePool();
  if (!pool) return errorResult("slug_stage_unknown", "DB pool unavailable.");
  const proofs = await getProofsByStage(pool, input.slug, input.stage_id);
  return jsonResult({ ok: true, payload: { proofs } });
}

// ---------------------------------------------------------------------------
// List
// ---------------------------------------------------------------------------

export async function handleRedStageProofList(args: unknown) {
  const input = RedStageProofListInputSchema.parse(args);
  const pool = getIaDatabasePool();
  if (!pool) return errorResult("slug_stage_unknown", "DB pool unavailable.");
  const counts = await listProofCountsBySlug(pool, input.slug);
  return jsonResult({ ok: true, payload: { counts } });
}

// ---------------------------------------------------------------------------
// Finalize
// ---------------------------------------------------------------------------

export async function handleRedStageProofFinalize(args: unknown) {
  const input = RedStageProofFinalizeInputSchema.parse(args);
  const pool = getIaDatabasePool();
  if (!pool) return errorResult("slug_stage_unknown", "DB pool unavailable.");

  const result = await finalizeProof(pool, input.slug, input.stage_id, input.anchor, input.green_status);

  if (result.kind === "updated") {
    console.log(JSON.stringify({
      event: "red_stage_proof_finalized",
      slug: input.slug,
      stage_id: input.stage_id,
      anchor: input.anchor,
      green_status: result.green_status,
    }));
    return jsonResult({ ok: true, payload: { green_status: result.green_status, finalized_at: result.finalized_at } });
  }
  if (result.kind === "blocked") {
    return errorResult("green_pass_blocked_unexpected_pass", "Cannot mark green_status='passed' when prior proof_status='unexpected_pass'.");
  }
  return errorResult("proof_not_found", `No proof row found for slug=${input.slug} stage_id=${input.stage_id} anchor=${input.anchor}`);
}

// ---------------------------------------------------------------------------
// Registration
// ---------------------------------------------------------------------------

export function registerRedStageProofTools(server: McpServer): void {
  server.registerTool(
    "red_stage_proof_capture",
    {
      title: "red_stage_proof_capture",
      description:
        "Pass A entry gate — capture pre-impl test-run result into ia_red_stage_proofs. " +
        "Rejects proof_status='unexpected_pass'. Validates anchor grammar and target_kind.",
      inputSchema: RedStageProofCaptureInputSchema.shape,
    },
    async (args) =>
      runWithToolTiming("red_stage_proof_capture", () => handleRedStageProofCapture(args)),
  );

  server.registerTool(
    "red_stage_proof_get",
    {
      title: "red_stage_proof_get",
      description:
        "Read proof rows for one (slug, stage_id), sorted ascending by captured_at. " +
        "Returns {ok: true, payload: {proofs: []}} for empty.",
      inputSchema: RedStageProofGetInputSchema.shape,
    },
    async (args) =>
      runWithToolTiming("red_stage_proof_get", () => handleRedStageProofGet(args)),
  );

  server.registerTool(
    "red_stage_proof_list",
    {
      title: "red_stage_proof_list",
      description:
        "Aggregate proof row counts by (stage_id, proof_status) for a whole plan slug. " +
        "Returns {ok: true, payload: {counts: []}} for unknown slug.",
      inputSchema: RedStageProofListInputSchema.shape,
    },
    async (args) =>
      runWithToolTiming("red_stage_proof_list", () => handleRedStageProofList(args)),
  );

  server.registerTool(
    "red_stage_proof_finalize",
    {
      title: "red_stage_proof_finalize",
      description:
        "Flip green_status on a captured proof row after the green-stage run completes. " +
        "Rejects green_status='passed' when prior proof_status='unexpected_pass'.",
      inputSchema: RedStageProofFinalizeInputSchema.shape,
    },
    async (args) =>
      runWithToolTiming("red_stage_proof_finalize", () => handleRedStageProofFinalize(args)),
  );
}
