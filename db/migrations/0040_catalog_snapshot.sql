-- 0040_catalog_snapshot.sql
--
-- DEC-A9 + DEC-A39 — asset-pipeline Stage 13.1 / TECH-2673.
--
-- `catalog_snapshot` tracks each per-kind JSON export written under
-- `Assets/StreamingAssets/catalog/`. Schema-version 2 supersedes the
-- legacy single-file v1 snapshots (`grid-asset-catalog-snapshot.json` +
-- `token-catalog-snapshot.json`) which this migration superficially
-- documents as retired (file deletes happen in the same Stage commit;
-- no SQL action needed).
--
-- Idempotent: IF NOT EXISTS on table + index; re-applying is a no-op.
-- No data seeds.

BEGIN;

CREATE TABLE IF NOT EXISTS catalog_snapshot (
  id                  uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  hash                text        NOT NULL,
  manifest_path       text        NOT NULL,
  entity_counts_json  jsonb       NOT NULL DEFAULT '{}'::jsonb,
  schema_version      int         NOT NULL DEFAULT 2,
  status              text        NOT NULL DEFAULT 'active'
    CHECK (status IN ('active', 'retired')),
  created_at          timestamptz NOT NULL DEFAULT now(),
  created_by          uuid        NOT NULL REFERENCES users (id),
  retired_at          timestamptz NULL
);

COMMENT ON TABLE catalog_snapshot IS
  'Per-export provenance row for the per-kind snapshot pipeline (DEC-A9 + DEC-A39, Stage 13.1).';
COMMENT ON COLUMN catalog_snapshot.hash IS
  'sha256 hex over kind-ordered concatenation of per-kind JSON bytes (sprite/asset/button/panel/audio/pool/token/archetype).';
COMMENT ON COLUMN catalog_snapshot.manifest_path IS
  'Repo-relative path of the manifest file (e.g. Assets/StreamingAssets/catalog/manifest.json).';
COMMENT ON COLUMN catalog_snapshot.entity_counts_json IS
  'Per-kind row counts at export time. Shape: { sprite, asset, button, panel, audio, pool, token, archetype } as ints.';

-- Hot list query: "active snapshots newest-first" + retire scan.
CREATE INDEX IF NOT EXISTS catalog_snapshot_status_created_idx
  ON catalog_snapshot (status, created_at);

COMMIT;

-- Rollback (dev only):
--   DROP INDEX IF EXISTS catalog_snapshot_status_created_idx;
--   DROP TABLE IF EXISTS catalog_snapshot;
