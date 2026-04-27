-- audio_detail (TECH-1956 / Stage 9.1).
--
-- Per DEC-A31 audio detail block + DEC-A26 measurement-on-promote semantic:
--   audio_detail — 1:1 with catalog_entity (kind=audio); carries source URI,
--                  Assets path, render measurements (LUFS / peak_db / duration),
--                  fingerprint of source bytes (sha256 hex).
--
-- LUFS + peak_db NULL allowed pre-promote (DEC-A31 spec literal "measured at
-- promote time"); TECH-1957 fills them via pyloudnorm + numpy.
--
-- Clip flag: NOT a column; TECH-1959 lint rule audio.peak_clipping reads
-- peak_db > -1.0 directly from this row (DEC-A31).
--
-- PK + FK: bigint (matches catalog_entity.id bigserial spine; sprite_detail /
-- button_detail / panel_detail use the same shape — DEC-A8).
--
-- @see ia/projects/asset-pipeline/stage-9.1 — TECH-1956 §Plan Digest

BEGIN;

CREATE TABLE IF NOT EXISTS audio_detail (
  entity_id        bigint  PRIMARY KEY REFERENCES catalog_entity (id) ON DELETE CASCADE,
  source_uri       text    NOT NULL,                                  -- gen://run_id/idx
  assets_path      text,                                              -- Assets/Audio/Generated/{slug}.ogg (post-promote)
  duration_ms      int     NOT NULL CHECK (duration_ms >= 0),
  sample_rate      int     NOT NULL CHECK (sample_rate > 0),
  channels         int     NOT NULL CHECK (channels > 0),
  loudness_lufs    real,                                              -- pyloudnorm BS.1770; NULL pre-promote (DEC-A31)
  peak_db          real,                                              -- 20*log10(max(abs(samples))); NULL pre-promote
  fingerprint      text    NOT NULL,                                  -- sha256 hex of source bytes (DEC-A31)
  updated_at       timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS audio_detail_fingerprint_idx
  ON audio_detail (fingerprint);

DROP TRIGGER IF EXISTS trg_audio_detail_touch ON audio_detail;
CREATE TRIGGER trg_audio_detail_touch
  BEFORE UPDATE ON audio_detail
  FOR EACH ROW EXECUTE FUNCTION catalog_touch_updated_at();

COMMIT;

-- Rollback (dev only): DROP TABLE IF EXISTS audio_detail;
