-- DB Lifecycle Extensions Stage 1 / TECH-2974 — change-log dedup constraint.
-- Source: docs/db-lifecycle-extensions-exploration.md §Q9
--
-- Two-step DDL:
--
--   1. Add nullable `stage_id` column on `ia_master_plan_change_log`. Audit
--      revealed legitimate per-stage entries sharing `(slug, kind,
--      commit_sha)` (e.g. `stage-backfill-done` rows for Stage 0.1 + 1.1
--      under same backfill commit). Schema needs the per-stage axis to
--      distinguish them; otherwise UNIQUE on `(slug, kind, commit_sha)`
--      alone would either reject real data or force lossy dedup.
--
--   2. Add UNIQUE `(slug, stage_id, kind, commit_sha)` so repeat closeout /
--      sha-backfill chains can rely on `INSERT ... ON CONFLICT DO NOTHING`
--      idempotent appends. NULLs treated as distinct (PG default), so
--      legacy plan-scope entries with no `stage_id` keep coexisting.
--
-- Pre-migration audit:
--   `npm run audit:master-plan-change-log-dups`
-- must exit 0 before this migration runs. CI / operator chain enforces.
--
-- Forward-only (BF=forward-only lock); no historical row mutation. The new
-- column is nullable + has no default, so existing rows materialise NULL —
-- consistent with PG UNIQUE distinct-NULL semantics + preserves audit shape.

BEGIN;

ALTER TABLE ia_master_plan_change_log
  ADD COLUMN IF NOT EXISTS stage_id text;

ALTER TABLE ia_master_plan_change_log
  ADD CONSTRAINT ia_master_plan_change_log_unique
  UNIQUE (slug, stage_id, kind, commit_sha);

COMMIT;
