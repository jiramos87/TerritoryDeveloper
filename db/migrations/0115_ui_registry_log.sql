-- 0115_ui_registry_log.sql
-- Wave A0 (TECH-27061) — UI action+bind registration log tables.
-- Written on Awake by UiActionRegistry / UiBindRegistry via Editor bridge.
-- Queried by action_registry_list + bind_registry_list MCP slices.

CREATE TABLE IF NOT EXISTS action_registry_log (
  id              BIGSERIAL PRIMARY KEY,
  registry_kind   TEXT NOT NULL DEFAULT 'action',
  ref_id          TEXT NOT NULL,
  handler_bound   BOOLEAN NOT NULL DEFAULT FALSE,
  last_updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  CONSTRAINT action_registry_log_ref_id_unique UNIQUE (ref_id)
);

COMMENT ON TABLE action_registry_log IS
  'Wave A0 (TECH-27061) — live snapshot of UiActionRegistry.Register calls. '
  'Written by Editor bridge Awake hook. ref_id = action id, handler_bound = stub vs real handler.';

CREATE TABLE IF NOT EXISTS bind_registry_log (
  id               BIGSERIAL PRIMARY KEY,
  registry_kind    TEXT NOT NULL DEFAULT 'bind',
  ref_id           TEXT NOT NULL,
  handler_bound    BOOLEAN NOT NULL DEFAULT FALSE,
  value_json       JSONB,
  subscriber_count INT NOT NULL DEFAULT 0,
  last_updated_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
  CONSTRAINT bind_registry_log_ref_id_unique UNIQUE (ref_id)
);

COMMENT ON TABLE bind_registry_log IS
  'Wave A0 (TECH-27061) — live snapshot of UiBindRegistry bind ids. '
  'Written by Editor bridge Awake hook. subscriber_count = active subscriber count.';

-- rollback: DROP TABLE IF EXISTS bind_registry_log; DROP TABLE IF EXISTS action_registry_log;
