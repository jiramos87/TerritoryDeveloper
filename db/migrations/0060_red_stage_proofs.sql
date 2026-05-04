BEGIN;

CREATE TABLE IF NOT EXISTS ia_red_stage_proofs (
  slug            TEXT        NOT NULL,
  stage_id        TEXT        NOT NULL,
  target_kind     TEXT        NOT NULL,
  anchor          TEXT        NOT NULL,
  proof_artifact_id UUID      NOT NULL,
  proof_status    TEXT        NOT NULL DEFAULT 'pending',
  green_status    TEXT,
  captured_at     TIMESTAMPTZ NOT NULL DEFAULT NOW(),

  PRIMARY KEY (slug, stage_id, anchor),

  CONSTRAINT ia_red_stage_proofs_fk_stage
    FOREIGN KEY (slug, stage_id)
    REFERENCES ia_stages (slug, stage_id)
    ON DELETE CASCADE,

  CONSTRAINT ia_red_stage_proofs_target_kind_check
    CHECK (target_kind IN ('tracer_verb', 'visibility_delta', 'bug_repro', 'design_only')),

  CONSTRAINT ia_red_stage_proofs_proof_status_check
    CHECK (proof_status IN ('pending', 'failed_as_expected', 'unexpected_pass', 'not_applicable')),

  CONSTRAINT ia_red_stage_proofs_green_status_check
    CHECK (green_status IS NULL OR green_status IN ('passed', 'failed'))
);

COMMENT ON TABLE ia_red_stage_proofs IS
  'Sidecar table — one row per (slug, stage_id, anchor) tracking pre-impl red-stage proof blobs. '
  'proof_status captures pre-impl test run result; green_status set by red_stage_proof_finalize '
  'MCP at Pass B after test goes green. FK → ia_stages with cascade delete.';

COMMIT;
