-- Button detail (TECH-1885 / Stage 8.1).
-- Per DEC-A7 hybrid binding model: button entities bind 6 typed sprite slots
-- (idle/hover/pressed/disabled/icon/badge) + 4 token slots (palette/frame_style/font/illumination)
-- + size_variant enum + action_id text + enable_predicate_json. Pure additive.
--
-- Token sub-kind discrimination: `catalog_entity.kind='token'` rows carry
-- `params_json.token_kind ∈ {palette, frame_style, font, illumination}` on
-- their pinned entity_version. Enforcement is at render-time / authoring-UI
-- (picker filters); button_detail FKs only assert kind='token' via app code.

BEGIN;

CREATE TABLE IF NOT EXISTS button_detail (
  entity_id                       bigint PRIMARY KEY REFERENCES catalog_entity (id) ON DELETE CASCADE,
  -- 6 sprite slots per DEC-A7
  sprite_idle_entity_id           bigint REFERENCES catalog_entity (id) ON DELETE SET NULL,
  sprite_hover_entity_id          bigint REFERENCES catalog_entity (id) ON DELETE SET NULL,
  sprite_pressed_entity_id        bigint REFERENCES catalog_entity (id) ON DELETE SET NULL,
  sprite_disabled_entity_id       bigint REFERENCES catalog_entity (id) ON DELETE SET NULL,
  sprite_icon_entity_id           bigint REFERENCES catalog_entity (id) ON DELETE SET NULL,
  sprite_badge_entity_id          bigint REFERENCES catalog_entity (id) ON DELETE SET NULL,
  -- 4 token slots per DEC-A7
  token_palette_entity_id         bigint REFERENCES catalog_entity (id) ON DELETE SET NULL,
  token_frame_style_entity_id     bigint REFERENCES catalog_entity (id) ON DELETE SET NULL,
  token_font_entity_id            bigint REFERENCES catalog_entity (id) ON DELETE SET NULL,
  token_illumination_entity_id    bigint REFERENCES catalog_entity (id) ON DELETE SET NULL,
  -- size variant + action wiring + enable predicate
  size_variant                    text   NOT NULL DEFAULT 'md'
    CHECK (size_variant IN ('sm', 'md', 'lg')),
  action_id                       text   NOT NULL DEFAULT '',
  enable_predicate_json           jsonb  NOT NULL DEFAULT '{}'::jsonb,
  updated_at                      timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS button_detail_size_variant_idx
  ON button_detail (size_variant);
CREATE INDEX IF NOT EXISTS button_detail_action_id_idx
  ON button_detail (action_id)
  WHERE action_id <> '';

DROP TRIGGER IF EXISTS trg_button_detail_touch ON button_detail;
CREATE TRIGGER trg_button_detail_touch
  BEFORE UPDATE ON button_detail
  FOR EACH ROW EXECUTE FUNCTION catalog_touch_updated_at();

COMMIT;

-- Rollback: DROP TABLE button_detail (dev only — production rollback via snapshot restore).
