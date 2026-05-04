/** Zod schemas for red_stage_proof_* tools. */

import { z } from "zod";

export const TARGET_KINDS = ["tracer_verb", "visibility_delta", "bug_repro", "design_only"] as const;
export const PROOF_STATUSES = ["pending", "failed_as_expected", "unexpected_pass", "not_applicable"] as const;
export const COMMAND_KINDS = ["npm-test", "dotnet-test", "unity-testmode-batch"] as const;
export const GREEN_STATUSES = ["passed", "failed"] as const;

export const RedStageProofCaptureInputSchema = z.object({
  slug: z.string().min(1).describe("Master-plan slug."),
  stage_id: z.string().min(1).describe("Stage id."),
  target_kind: z.enum(TARGET_KINDS).describe("Visibility category."),
  anchor: z.string().min(1).describe("Canonical anchor in one of the 4 grammar forms."),
  proof_artifact_id: z.string().uuid().describe("UUID for the proof row."),
  command_kind: z.enum(COMMAND_KINDS).describe("Allowlisted test runner kind."),
  proof_status: z
    .enum(PROOF_STATUSES)
    .describe("Pre-impl test-run result."),
});

export const RedStageProofGetInputSchema = z.object({
  slug: z.string().min(1).describe("Master-plan slug."),
  stage_id: z.string().min(1).describe("Stage id."),
});

export const RedStageProofListInputSchema = z.object({
  slug: z.string().min(1).describe("Master-plan slug."),
});

export const RedStageProofFinalizeInputSchema = z.object({
  slug: z.string().min(1).describe("Master-plan slug."),
  stage_id: z.string().min(1).describe("Stage id."),
  anchor: z.string().min(1).describe("Proof anchor (PK component)."),
  green_status: z.enum(GREEN_STATUSES).describe("Green-stage outcome."),
});
