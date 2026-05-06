-- 0076_audio_catalog.sql
-- Stage 9.5 game-ui-catalog-bake — audio_catalog DB tier stub.
-- Schema only: no bake-to-clip logic. Manually populated until Stage N audio bake.
-- Mirrors sprite_catalog (0075) shape with audio-specific columns.
-- TECH-15226

CREATE TABLE IF NOT EXISTS audio_catalog (
  id          bigserial   PRIMARY KEY,
  slug        text        NOT NULL,
  kind        text        NOT NULL DEFAULT 'sfx',
  clip_path   text        NOT NULL,
  tier        text        NOT NULL DEFAULT 'audio-catalog',
  created_at  timestamptz NOT NULL DEFAULT now(),
  updated_at  timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT ck_audio_catalog_kind    CHECK (kind IN ('sfx', 'music', 'ambient')),
  CONSTRAINT ck_audio_catalog_tier    CHECK (tier = 'audio-catalog'),
  CONSTRAINT ck_audio_catalog_slug_ne CHECK (slug <> ''),
  CONSTRAINT ck_audio_catalog_path_ne CHECK (clip_path <> ''),
  CONSTRAINT uq_audio_catalog_path    UNIQUE (clip_path)
);

COMMENT ON TABLE audio_catalog IS
  'audio-catalog tier: UI SFX + music clip registry (schema stub, no bake-to-clip yet). '
  'slug = descriptive name (e.g. notification_show); '
  'kind IN (sfx, music, ambient); '
  'clip_path = repo-relative Unity asset path (e.g. Assets/Audio/SFX/notification_show.wav). '
  'TECH-15226 — Stage 9.5 game-ui-catalog-bake.';

COMMENT ON COLUMN audio_catalog.slug IS 'Descriptive name / token slug (e.g. sfx_notification_show).';
COMMENT ON COLUMN audio_catalog.kind IS 'Audio kind: sfx | music | ambient.';
COMMENT ON COLUMN audio_catalog.clip_path IS 'Repo-relative Unity asset path. Unique.';
COMMENT ON COLUMN audio_catalog.tier IS 'Fixed: audio-catalog. Constraint-enforced.';
