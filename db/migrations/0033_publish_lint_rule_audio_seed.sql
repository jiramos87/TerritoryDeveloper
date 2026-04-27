-- publish_lint_rule + audio seed (TECH-1959 / Stage 9.1).
--
-- DEC-A30 publish lint framework:
--   publish_lint_rule — registry table for runtime-configurable lint rules.
--   Rule shape per docs/asset-pipeline-architecture.md DEC-A30 spec block:
--     publish_lint_rule (
--       rule_id      TEXT PK,
--       kind         TEXT NOT NULL,
--       severity     TEXT NOT NULL ('warn' | 'info' | 'block'),
--       enabled      BOOLEAN DEFAULT true,
--       config_json  JSONB DEFAULT '{}'
--     )
--
-- Audio carve-out per DEC-A30 + DEC-A31 spec literal "Hard lint blocks publish":
--   audio.loudness_out_of_range  severity='block' default window [-23, -10] LUFS
--   audio.peak_clipping          severity='block' threshold peak_db > -1.0 dB
-- (DEC-A30 default for new rules is 'warn'; audio explicitly hard-gates because
--  out-of-range loudness leaks into shipped builds — listener fatigue + clip).
--
-- @see ia/projects/asset-pipeline/stage-9.1 — TECH-1959 §Plan Digest

BEGIN;

CREATE TABLE IF NOT EXISTS publish_lint_rule (
  rule_id      text PRIMARY KEY,
  kind         text NOT NULL,
  severity     text NOT NULL CHECK (severity IN ('block', 'warn', 'info')),
  enabled      boolean NOT NULL DEFAULT true,
  config_json  jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at   timestamptz NOT NULL DEFAULT now(),
  updated_at   timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS publish_lint_rule_kind_idx
  ON publish_lint_rule (kind, enabled);

DROP TRIGGER IF EXISTS trg_publish_lint_rule_touch ON publish_lint_rule;
CREATE TRIGGER trg_publish_lint_rule_touch
  BEFORE UPDATE ON publish_lint_rule
  FOR EACH ROW EXECUTE FUNCTION catalog_touch_updated_at();

-- Audio rule seeds (DEC-A31 carve-out).
INSERT INTO publish_lint_rule (rule_id, kind, severity, enabled, config_json)
VALUES
  (
    'audio.loudness_out_of_range',
    'audio',
    'block',
    true,
    '{"min_loudness_lufs": -23, "max_loudness_lufs": -10}'::jsonb
  ),
  (
    'audio.peak_clipping',
    'audio',
    'block',
    true,
    '{"max_peak_db": -1.0}'::jsonb
  )
ON CONFLICT (rule_id) DO NOTHING;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM publish_lint_rule WHERE rule_id IN ('audio.loudness_out_of_range', 'audio.peak_clipping');
--   DROP TABLE IF EXISTS publish_lint_rule;
