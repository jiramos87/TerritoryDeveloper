-- 0027_job_queue_render_run.sql
--
-- DEC-A40 + DEC-A26 + DEC-A41 — render pipeline durable storage.
--
-- Generic `job_queue` table powering both render runs and future
-- snapshot rebuilds (kind discriminator). `render_run` is the
-- per-success provenance row tracking archetype + params + outputs +
-- replay lineage. The single-FIFO worker (TECH-1468) and the render
-- API routes (TECH-1469 / TECH-1470) consume both surfaces.
--
-- Idempotent: IF NOT EXISTS on tables/indexes; re-applying is a no-op.
-- No data seeds.

BEGIN;

-- job_queue — generic background-job spine (DEC-A40 / DEC-A39 reuse).
-- `kind` discriminates queue families ('render_run', future
-- 'snapshot_rebuild'); `payload_json` carries kind-specific payload.
-- For kind='render_run' the payload shape is:
--   { archetype_id, archetype_version_id, params_json,
--     parent_run_id?, mode? }   where mode ∈ {'standard','identical','replay'}.
-- `re_run_requested` reserved for DEC-A40 retry button.
-- `heartbeat_at` independent of `started_at` so the stale-row sweep
-- (worker concern, TECH-1468) can detect orphaned `running` rows even
-- when the original claim is hours old.
-- `idempotency_key` per DEC-A48 — partial unique index handles dedupe
-- within the 24h replay window (composite with actor_user_id).
-- `archetype_version_id` / `archetype_id` on render_run intentionally
-- carry NO FK constraint — catalog spine (`0021_catalog_spine.sql`)
-- owns those tables and a hard FK here would couple Stage 4.1
-- migration ordering to catalog migration state.
CREATE TABLE IF NOT EXISTS job_queue (
  job_id              uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  kind                text        NOT NULL,
  status              text        NOT NULL DEFAULT 'queued',
  payload_json        jsonb       NOT NULL DEFAULT '{}'::jsonb,
  re_run_requested    boolean     NOT NULL DEFAULT false,
  enqueued_at         timestamptz NOT NULL DEFAULT now(),
  started_at          timestamptz NULL,
  finished_at         timestamptz NULL,
  heartbeat_at        timestamptz NULL,
  error               text        NULL,
  actor_user_id       uuid        NULL REFERENCES users(id),
  idempotency_key     text        NULL,
  CONSTRAINT job_queue_status_check
    CHECK (status IN ('queued','running','done','failed'))
);

-- FIFO claim path: worker SELECT ... FOR UPDATE SKIP LOCKED orders by
-- enqueued_at within (kind, status). Composite index covers the hot
-- claim query.
CREATE INDEX IF NOT EXISTS job_queue_status_kind_idx
  ON job_queue (kind, status, enqueued_at);

-- DEC-A48 idempotency lookup. Partial index on (actor_user_id,
-- idempotency_key) WHERE idempotency_key IS NOT NULL — sub-ms hit on
-- replay; zero footprint when callers omit the header.
CREATE INDEX IF NOT EXISTS job_queue_idempotency_idx
  ON job_queue (actor_user_id, idempotency_key)
  WHERE idempotency_key IS NOT NULL;

-- render_run — success-only provenance row (DEC-A26).
-- `params_hash` is sha256 hex of canonicalized `params_json` (sorted
-- keys, no whitespace) — worker computes on insert.
-- `build_fingerprint` = git sha + sprite-gen tool version (worker
-- composes; "unknown" fallback when neither available).
-- `parent_run_id` self-reference enables replay/identical lineage
-- queries; nullable for fresh runs.
-- `variant_disposition_json` reserved for DEC-A41 GC + retire flags
-- (e.g. `{ "v0": "kept", "v1": "discarded" }`); default '{}' lets
-- callers omit on insert.
CREATE TABLE IF NOT EXISTS render_run (
  run_id                    uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  archetype_id              uuid        NOT NULL,
  archetype_version_id      uuid        NOT NULL,
  params_json               jsonb       NOT NULL,
  params_hash               text        NOT NULL,
  output_uris               text[]      NOT NULL,
  build_fingerprint         text        NOT NULL,
  duration_ms               int         NOT NULL,
  triggered_by              uuid        NULL REFERENCES users(id),
  created_at                timestamptz NOT NULL DEFAULT now(),
  parent_run_id             uuid        NULL REFERENCES render_run(run_id),
  variant_disposition_json  jsonb       NOT NULL DEFAULT '{}'::jsonb
);

COMMENT ON COLUMN render_run.archetype_id IS
  'FK-by-convention to catalog_entity.id (no DB constraint; catalog spine owns the table).';
COMMENT ON COLUMN render_run.archetype_version_id IS
  'FK-by-convention to entity_version.id (no DB constraint; catalog spine owns the table).';
COMMENT ON COLUMN render_run.params_hash IS
  'sha256 hex of canonicalized params_json (sorted keys, no whitespace).';
COMMENT ON COLUMN render_run.build_fingerprint IS
  'git sha + sprite-gen tool version concatenation (worker composes).';

-- Replay lineage queries hit (parent_run_id) — index covers the
-- "show all children of run X" query path.
CREATE INDEX IF NOT EXISTS render_run_parent_idx
  ON render_run (parent_run_id);

COMMIT;
