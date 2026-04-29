-- 0051 — pg_trgm GIN indexes on catalog_entity for cross-kind similarity search.
--
-- pg_trgm extension already exists (0015_ia_tasks_core.sql) — idempotent here.
-- Two indexes:
--   name_trgm  — lower(display_name)  for name-prefix / fuzzy match
--   slug_trgm  — slug                 for slug prefix search
--
-- The original draft included a third index `tags_trgm` over
-- `lower(tags::text)`. `tags` is text[]; both `text[]::text` and
-- `array_to_string(tags, …)` are STABLE (not IMMUTABLE) per `pg_proc`,
-- so postgres rejected the index with `functions in index expression
-- must be marked IMMUTABLE`. The migration has therefore never
-- successfully applied in any environment — no consumer can have
-- depended on `catalog_entity_tags_trgm_idx`. Dropping that index from
-- 0051 unblocks the runner. If tag substring search becomes a real
-- requirement later, the canonical fix is a SQL-level IMMUTABLE
-- wrapper function (`CREATE FUNCTION immutable_tags_text(text[])
-- RETURNS text LANGUAGE sql IMMUTABLE …`) shipped in a follow-up
-- migration; deferred until a real consumer surfaces.

CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE INDEX IF NOT EXISTS catalog_entity_name_trgm_idx
  ON catalog_entity USING GIN (lower(display_name) gin_trgm_ops);

CREATE INDEX IF NOT EXISTS catalog_entity_slug_trgm_idx
  ON catalog_entity USING GIN (slug gin_trgm_ops);
