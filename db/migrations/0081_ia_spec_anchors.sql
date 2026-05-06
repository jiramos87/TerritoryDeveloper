-- 0081_ia_spec_anchors.sql
-- Stage 1 chain-token-cut — anchor registry table.
-- Replaces ship-plan Phase 5 file-scan anchor resolution with a SQL JOIN.
-- TECH-15899

CREATE TABLE IF NOT EXISTS ia_spec_anchors (
  id              bigserial   PRIMARY KEY,
  slug            text        NOT NULL,
  section_id      text        NOT NULL,
  sha256          text        NOT NULL,
  body_text       text        NOT NULL DEFAULT '',
  last_indexed_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_ia_spec_anchors_slug_section UNIQUE (slug, section_id),
  CONSTRAINT ck_ia_spec_anchors_slug_ne      CHECK (slug <> ''),
  CONSTRAINT ck_ia_spec_anchors_section_ne   CHECK (section_id <> ''),
  CONSTRAINT ck_ia_spec_anchors_sha256_ne    CHECK (sha256 <> '')
);

CREATE INDEX IF NOT EXISTS ix_ia_spec_anchors_slug_section
  ON ia_spec_anchors (slug, section_id);

COMMENT ON TABLE ia_spec_anchors IS
  'Anchor registry: pre-indexed ia/specs/** sections keyed by (slug, section_id). '
  'Populated by generate-ia-indexes --write-anchors pass. '
  'Replaces per-call file scan in plan_digest_resolve_anchor (TECH-15899). '
  'sha256 = whole-file SHA-256 (locked decision #11). '
  'body_text = rendered section body (heading + content). '
  'Resolver falls back to file scan when table is empty.';

COMMENT ON COLUMN ia_spec_anchors.slug IS 'Spec doc key, e.g. geo, roads, glossary.';
COMMENT ON COLUMN ia_spec_anchors.section_id IS 'Section id, e.g. 13.4 or heading slug.';
COMMENT ON COLUMN ia_spec_anchors.sha256 IS 'Whole-file SHA-256 hex (for staleness detection).';
COMMENT ON COLUMN ia_spec_anchors.body_text IS 'Rendered section body stored for zero-read JOIN resolution.';
COMMENT ON COLUMN ia_spec_anchors.last_indexed_at IS 'UTC timestamp of last populate/refresh.';
