-- TECH-15904: Stage-bundle digest cache
-- Caches stage_bundle payload by content-hash for resume acceleration.
-- Hash-gated, no TTL. UNIQUE (stage_id, content_hash).

CREATE TABLE IF NOT EXISTS ia_stage_bundle_cache (
  id              BIGSERIAL PRIMARY KEY,
  stage_id        TEXT        NOT NULL,
  slug            TEXT        NOT NULL,
  content_hash    TEXT        NOT NULL,
  payload         JSONB       NOT NULL,
  fetched_at      TIMESTAMPTZ NOT NULL DEFAULT now(),

  UNIQUE (stage_id, slug, content_hash)
);

COMMENT ON TABLE ia_stage_bundle_cache IS
  'Hash-gated cache for stage_bundle resolver. Key=(stage_id, slug, content_hash). '
  'No TTL — invalidated by content change only.';

-- Rollback: DROP TABLE IF EXISTS ia_stage_bundle_cache;
