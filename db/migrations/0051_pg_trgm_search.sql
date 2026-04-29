-- 0051 — pg_trgm GIN indexes on catalog_entity for cross-kind similarity search.
--
-- pg_trgm extension already exists (0015_ia_tasks_core.sql) — idempotent here.
-- Three indexes:
--   name_trgm  — lower(display_name)  for name-prefix / fuzzy match
--   slug_trgm  — slug                 for slug prefix search
--   tags_trgm  — lower(tags::text)    for tag substring search

CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE INDEX IF NOT EXISTS catalog_entity_name_trgm_idx
  ON catalog_entity USING GIN (lower(display_name) gin_trgm_ops);

CREATE INDEX IF NOT EXISTS catalog_entity_slug_trgm_idx
  ON catalog_entity USING GIN (slug gin_trgm_ops);

CREATE INDEX IF NOT EXISTS catalog_entity_tags_trgm_idx
  ON catalog_entity USING GIN (lower(tags::text) gin_trgm_ops);
