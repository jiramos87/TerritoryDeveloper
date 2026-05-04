/** Anchor grammar parser for red_stage_proof_capture. */

import { RedStageProofError } from "./errors.js";

// 4 canonical grammar forms:
//   tracer-verb-test:{path}::{method}
//   visibility-delta-test:{path}::{method}
//   BUG-NNNN:{path}::{method}
//   n/a

const TRACER_VERB_RE = /^tracer-verb-test:.+::.+$/;
const VISIBILITY_DELTA_RE = /^visibility-delta-test:.+::.+$/;
const BUG_REPRO_RE = /^BUG-\d+:.+::.+$/;
const NA = "n/a";

export function parseAnchor(anchor: string): { valid: true } {
  if (
    anchor === NA ||
    TRACER_VERB_RE.test(anchor) ||
    VISIBILITY_DELTA_RE.test(anchor) ||
    BUG_REPRO_RE.test(anchor)
  ) {
    return { valid: true };
  }
  throw new RedStageProofError(
    "anchor_grammar_invalid",
    `Anchor does not match any of the 4 canonical grammar forms: "${anchor}"`,
  );
}
