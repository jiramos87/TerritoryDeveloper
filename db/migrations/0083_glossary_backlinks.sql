-- 0083_glossary_backlinks.sql
-- Stage 1 chain-token-cut — glossary back-link table.
-- Auto-populated by glossary-backlink-enrich.mjs post-ship-plan script (TECH-15903).
-- Depends on ia_mcp_context_cache (0082) for cache-backed glossary_discover.

CREATE TABLE IF NOT EXISTS ia_glossary_backlinks (
  id         bigserial   PRIMARY KEY,
  plan_id    text        NOT NULL,
  term       text        NOT NULL,
  section_id text        NOT NULL DEFAULT '',
  count      integer     NOT NULL DEFAULT 1,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_ia_glossary_backlinks_plan_term_section UNIQUE (plan_id, term, section_id),
  CONSTRAINT ck_ia_glossary_backlinks_plan_ne  CHECK (plan_id <> ''),
  CONSTRAINT ck_ia_glossary_backlinks_term_ne  CHECK (term <> ''),
  CONSTRAINT ck_ia_glossary_backlinks_count_pos CHECK (count > 0)
);

CREATE INDEX IF NOT EXISTS ix_ia_glossary_backlinks_plan_term
  ON ia_glossary_backlinks (plan_id, term);

COMMENT ON TABLE ia_glossary_backlinks IS
  'Glossary back-link table (TECH-15903). '
  'Populated by tools/scripts/glossary-backlink-enrich.mjs post-ship-plan hook. '
  'One row per (plan_id, term, section_id) — count = mention count in that section. '
  'Reduces per-plan enrichment token overhead (~3k tokens saved per plan). '
  'Uses ia_mcp_context_cache for cache-backed glossary_discover lookups.';

COMMENT ON COLUMN ia_glossary_backlinks.plan_id IS 'Master-plan slug.';
COMMENT ON COLUMN ia_glossary_backlinks.term IS 'Glossary term (canonical casing from glossary-index.json).';
COMMENT ON COLUMN ia_glossary_backlinks.section_id IS 'Task id or section anchor where term appears.';
COMMENT ON COLUMN ia_glossary_backlinks.count IS 'Mention count for (plan_id, term, section_id).';
