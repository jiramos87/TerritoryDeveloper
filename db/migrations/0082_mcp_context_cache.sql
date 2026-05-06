-- 0082_mcp_context_cache.sql
-- Stage 1 chain-token-cut — shared MCP context cache.
-- Caches router_for_task + glossary_lookup + invariants_summary per plan_id.
-- Source-content-hash gating (no TTL) per locked decision #6.
-- TECH-15902

CREATE TABLE IF NOT EXISTS ia_mcp_context_cache (
  id           bigserial   PRIMARY KEY,
  plan_id      text        NOT NULL,
  key          text        NOT NULL,
  payload      jsonb       NOT NULL,
  content_hash text        NOT NULL,
  created_at   timestamptz NOT NULL DEFAULT now(),
  updated_at   timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_ia_mcp_context_cache_plan_key UNIQUE (plan_id, key),
  CONSTRAINT ck_ia_mcp_context_cache_plan_ne  CHECK (plan_id <> ''),
  CONSTRAINT ck_ia_mcp_context_cache_key_ne   CHECK (key <> ''),
  CONSTRAINT ck_ia_mcp_context_cache_hash_ne  CHECK (content_hash <> '')
);

CREATE INDEX IF NOT EXISTS ix_ia_mcp_context_cache_content_hash
  ON ia_mcp_context_cache (content_hash);

COMMENT ON TABLE ia_mcp_context_cache IS
  'Shared MCP context cache per plan_id (TECH-15902). '
  'Caches router_for_task + glossary_lookup + invariants_summary payloads across lifecycle skills. '
  'No TTL — invalidation via source content_hash (SHA-256 of source doc). '
  'UNIQUE (plan_id, key) — one row per (plan, tool_call_key). '
  'Read via mcp_cache_get / written via mcp_cache_set MCP tools.';

COMMENT ON COLUMN ia_mcp_context_cache.plan_id IS 'Master-plan slug or ad-hoc session id.';
COMMENT ON COLUMN ia_mcp_context_cache.key IS 'Cache key, e.g. router_for_task:{domain} or glossary_lookup:{term}.';
COMMENT ON COLUMN ia_mcp_context_cache.payload IS 'Cached MCP tool response payload (jsonb).';
COMMENT ON COLUMN ia_mcp_context_cache.content_hash IS 'SHA-256 of the source doc at cache write time. Mismatch = stale.';
