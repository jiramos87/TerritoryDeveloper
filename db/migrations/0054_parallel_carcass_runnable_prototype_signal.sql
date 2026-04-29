-- 0054_parallel_carcass_runnable_prototype_signal.sql
--
-- parallel-carcass-rollout Stage 1.3 / TECH-5072.
-- Closes the carcass3 acceptance loop: persist the `runnable_prototype`
-- signal binding `(slug='parallel-carcass-rollout', stage_id='1.3')`
-- in `stage_carcass_signals`, evidencing that Stage 1.3's
-- section-closeout flow ships a working prototype humans can run.
--
-- Slot note: §Plan Digest names slot 0053; slot 0053 was taken by
-- `0053_publish_lint_finding.sql` (asset-pipeline Stage 15.1 /
-- TECH-4183). Numbering corrected to 0054 per §Implementer Latitude
-- (last applied migration assumption in spec was stale).
--
-- Idempotent: ON CONFLICT DO NOTHING on PK
-- `(slug, stage_id, signal_kind)` from migration 0049.
--
-- Pre-condition: `runnable_prototype` row already seeded in
-- `carcass_signal_kinds` by migration 0049.
--
-- Rollback (manual):
--   DELETE FROM stage_carcass_signals
--    WHERE slug = 'parallel-carcass-rollout'
--      AND stage_id = '1.3'
--      AND signal_kind = 'runnable_prototype';

INSERT INTO stage_carcass_signals (slug, stage_id, signal_kind)
VALUES ('parallel-carcass-rollout', '1.3', 'runnable_prototype')
ON CONFLICT DO NOTHING;
