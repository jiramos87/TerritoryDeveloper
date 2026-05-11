-- 0147_ia_ui_action_sinks.sql
-- Layer 1 author-gate: action-id sink uniqueness (TECH-28357).
-- Tracks which panel owns each action_id; catalog_panel_publish blocks
-- collision when a new row's child.action_id is already claimed by a
-- different owner (error code: action_id_sink_collision).

BEGIN;

CREATE TABLE IF NOT EXISTS ia_ui_action_sinks (
  action_id        text        PRIMARY KEY,
  owner_panel_slug text        NOT NULL,
  registered_at    timestamptz NOT NULL DEFAULT now()
);

DO $$
BEGIN
  RAISE NOTICE '0147 OK: ia_ui_action_sinks table created (action-id sink uniqueness gate)';
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DROP TABLE IF EXISTS ia_ui_action_sinks;
