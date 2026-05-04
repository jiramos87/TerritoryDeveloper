-- 0059_ia_stages_prototype_first_columns.sql
--
-- Adds two nullable columns to ia_stages for the prototype-first-methodology
-- validator (Stage 1.3, T1.3.1):
--   * tracer_slice_block jsonb — Stage 1.0/1.1 tracer slice block
--     (5 fields: name, verb, surface, evidence, gate). NULL for grandfathered
--     plans (created_at < 2026-05-03 per D3).
--   * visibility_delta text — single-line description of new player-visible
--     surface added by the stage. NULL for grandfathered plans + Stage 1.x.
--     Validator (T1.3.2) enforces non-empty + unique within plan for Stages 2+.
--
-- Idempotent (IF NOT EXISTS). No backfill — existing rows stay NULL.
-- Invariant #13 (column-add only; no id-counter mutation).

BEGIN;

ALTER TABLE ia_stages
  ADD COLUMN IF NOT EXISTS tracer_slice_block jsonb;

ALTER TABLE ia_stages
  ADD COLUMN IF NOT EXISTS visibility_delta text;

COMMIT;
