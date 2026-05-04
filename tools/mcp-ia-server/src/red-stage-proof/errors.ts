/** RedStageProofError — structured error for all red_stage_proof_* tools. */

export type RedStageProofErrorCode =
  | "unexpected_pass_blocked"
  | "target_kind_invalid"
  | "proof_status_invalid"
  | "command_kind_invalid"
  | "anchor_grammar_invalid"
  | "slug_stage_unknown"
  | "anchor_already_captured"
  | "green_pass_blocked_unexpected_pass"
  | "green_status_invalid"
  | "proof_not_found";

export class RedStageProofError extends Error {
  readonly code: RedStageProofErrorCode;
  readonly details?: unknown;

  constructor(code: RedStageProofErrorCode, message: string, details?: unknown) {
    super(message);
    this.name = "RedStageProofError";
    this.code = code;
    this.details = details;
  }
}
