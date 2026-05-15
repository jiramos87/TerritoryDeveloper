-- 0164_ia_review_findings.sql
--
-- Wave E (vibe-coding-safety stage-6-0) — multi-agent critic pipeline findings table.
-- Stores findings emitted by /critic-style, /critic-logic, /critic-security subagents
-- during /ship-final Pass B.
--
-- Idempotent: CREATE TABLE IF NOT EXISTS.

BEGIN;

CREATE TABLE IF NOT EXISTS ia_review_findings (
  id          SERIAL PRIMARY KEY,
  plan_slug   TEXT    NOT NULL,
  stage_id    INTEGER NULL,
  critic_kind TEXT    NOT NULL CHECK (critic_kind IN ('style', 'logic', 'security')),
  severity    TEXT    NOT NULL CHECK (severity IN ('low', 'medium', 'high')),
  body        TEXT    NOT NULL,
  file_path   TEXT    NULL,
  line_range  TEXT    NULL,
  created_at  TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ia_review_findings_slug_idx
  ON ia_review_findings (plan_slug);

CREATE INDEX IF NOT EXISTS ia_review_findings_severity_idx
  ON ia_review_findings (plan_slug, severity);

COMMENT ON TABLE ia_review_findings IS
  'Multi-agent critic findings per plan close (Wave E / vibe-coding-safety).';
COMMENT ON COLUMN ia_review_findings.critic_kind IS
  'Emitting critic subagent kind: style | logic | security.';
COMMENT ON COLUMN ia_review_findings.severity IS
  'Finding severity: low | medium | high. severity=high blocks plan close.';

COMMIT;
