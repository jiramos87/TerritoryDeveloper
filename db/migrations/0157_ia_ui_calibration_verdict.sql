-- Migration 0157: ia_ui_calibration_verdict — mirror table for legacy JSONL calibration rows.
-- ui-visual-regression Stage 3.0 — TECH-31896

CREATE TABLE IF NOT EXISTS ia_ui_calibration_verdict (
  id          bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  source_file text NOT NULL,
  line_idx    integer NOT NULL,
  panel_slug  text,
  payload     jsonb NOT NULL,
  migrated_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (source_file, line_idx)
);

CREATE INDEX IF NOT EXISTS ia_ui_calibration_verdict_slug_idx
  ON ia_ui_calibration_verdict (panel_slug);
CREATE INDEX IF NOT EXISTS ia_ui_calibration_verdict_source_idx
  ON ia_ui_calibration_verdict (source_file);
