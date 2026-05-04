/**
 * Tracer-verb test: red_stage_proof_capture stub — Stage 1.0
 *
 * anchor: tracer-verb-test:tools/mcp-ia-server/tests/tools/red-stage-proof.test.ts::TracerCapturesUnexpectedPass
 */

import test from "node:test";
import assert from "node:assert/strict";
import { runRedStageProofCapture, TRACER_UUID } from "../../src/tools/red-stage-proof.js";

const TRACER_INPUT = {
  slug: "tdd-red-green-methodology",
  stage_id: "1.0",
  target_kind: "tracer_verb" as const,
  anchor: `tracer-verb-test:tools/mcp-ia-server/tests/tools/red-stage-proof.test.ts::TracerCapturesUnexpectedPass`,
  proof_artifact_id: TRACER_UUID,
  command_kind: "npm-test" as const,
};

test("TracerCapturesUnexpectedPass — stub rejects on unexpected_pass", () => {
  const result = runRedStageProofCapture(TRACER_INPUT);

  // Entry gate must fail-closed when stub returns unexpected_pass
  assert.equal(result.ok, false, "expected ok:false on unexpected_pass");
  assert.equal(result.error, "unexpected_pass_rejected");
});

test("StubReturnsFixedUuid — payload carries fixed tracer UUID", () => {
  const result = runRedStageProofCapture(TRACER_INPUT);

  assert.equal(result.ok, false);
  assert.ok(result.payload, "expected payload on rejection envelope");
  assert.equal(
    result.payload!.proof_artifact_id,
    TRACER_UUID,
    "rejection payload must carry the fixed tracer UUID",
  );
});

test("design_only anchor — returns not_applicable without rejection", () => {
  const result = runRedStageProofCapture({
    ...TRACER_INPUT,
    target_kind: "design_only",
    anchor: "n/a",
  });

  assert.equal(result.ok, true);
  assert.equal(result.status, "not_applicable");
});
