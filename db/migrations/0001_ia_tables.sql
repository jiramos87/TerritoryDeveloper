-- TECH-44b — IA core tables (normalized; no JSONB in milestone 1).
-- Apply via docs/postgres-ia-dev-setup.md or tools/postgres-ia/apply-migrations.mjs

BEGIN;

CREATE TABLE IF NOT EXISTS schema_migrations (
  version     text PRIMARY KEY,
  applied_at  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS glossary (
  id          bigserial PRIMARY KEY,
  term_key    text NOT NULL UNIQUE,
  term        text NOT NULL,
  definition  text,
  spec_key    text,
  category    text,
  created_at  timestamptz NOT NULL DEFAULT now(),
  updated_at  timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS glossary_spec_key_idx ON glossary (spec_key) WHERE spec_key IS NOT NULL;

CREATE TABLE IF NOT EXISTS spec_sections (
  id           bigserial PRIMARY KEY,
  spec_key     text NOT NULL,
  section_id   text NOT NULL,
  title        text,
  line_start   integer,
  source_path  text,
  UNIQUE (spec_key, section_id)
);

CREATE INDEX IF NOT EXISTS spec_sections_spec_key_idx ON spec_sections (spec_key);

CREATE TABLE IF NOT EXISTS invariants (
  id            bigserial PRIMARY KEY,
  sort_order    integer NOT NULL DEFAULT 0,
  invariant_key text UNIQUE,
  title         text,
  body          text NOT NULL,
  source        text NOT NULL DEFAULT 'invariants.mdc'
);

CREATE INDEX IF NOT EXISTS invariants_sort_order_idx ON invariants (sort_order);

CREATE TABLE IF NOT EXISTS relationships (
  id             bigserial PRIMARY KEY,
  from_ref       text NOT NULL,
  to_ref         text NOT NULL,
  relation_kind  text NOT NULL,
  notes          text,
  UNIQUE (from_ref, to_ref, relation_kind)
);

CREATE INDEX IF NOT EXISTS relationships_from_ref_idx ON relationships (from_ref);
CREATE INDEX IF NOT EXISTS relationships_to_ref_idx ON relationships (to_ref);

COMMIT;
