-- publish_lint_rule non-audio seed (TECH-2568 / Stage 12.1).
--
-- DEC-A30 publish lint framework — Layer 1 rule registry seeds for the 7
-- non-audio kinds. Default severity = 'warn' per DEC-A30; `token.no_consumers`
-- overridden to 'info' per DEC-A30 spec block. All seeds ship with empty
-- `config_json` and `enabled=true`. Audit fns ship as stubs in Stage 12.1
-- (`web/lib/lint/runner.ts`); real audit logic lands in later stages per
-- DEC-A30 §Layer 1 ("hard gates ship first, soft lints accrue").
--
-- Idempotent via `ON CONFLICT (rule_id) DO NOTHING` — re-runs safe.
--
-- @see ia/projects/asset-pipeline/stage-12.1 — TECH-2568 §Plan Digest
-- @see db/migrations/0033_publish_lint_rule_audio_seed.sql — pattern reference

BEGIN;

INSERT INTO publish_lint_rule (rule_id, kind, severity, enabled, config_json)
VALUES
  ('sprite.missing_ppu',           'sprite',    'warn', true, '{}'::jsonb),
  ('sprite.missing_pivot',         'sprite',    'warn', true, '{}'::jsonb),
  ('asset.no_sprite_bound',        'asset',     'warn', true, '{}'::jsonb),
  ('button.missing_icon',          'button',    'warn', true, '{}'::jsonb),
  ('button.missing_label',         'button',    'warn', true, '{}'::jsonb),
  ('panel.empty_slot_below_min',   'panel',     'warn', true, '{}'::jsonb),
  ('panel.unfilled_required_slot', 'panel',     'warn', true, '{}'::jsonb),
  ('pool.empty',                   'pool',      'warn', true, '{}'::jsonb),
  ('pool.no_primary_subtype',      'pool',      'warn', true, '{}'::jsonb),
  ('token.no_consumers',           'token',     'info', true, '{}'::jsonb),
  ('archetype.unpinned_dependency','archetype', 'warn', true, '{}'::jsonb)
ON CONFLICT (rule_id) DO NOTHING;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM publish_lint_rule WHERE rule_id IN (
--     'sprite.missing_ppu','sprite.missing_pivot','asset.no_sprite_bound',
--     'button.missing_icon','button.missing_label','panel.empty_slot_below_min',
--     'panel.unfilled_required_slot','pool.empty','pool.no_primary_subtype',
--     'token.no_consumers','archetype.unpinned_dependency'
--   );
