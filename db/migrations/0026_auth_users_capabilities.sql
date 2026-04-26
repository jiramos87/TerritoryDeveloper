-- 0026_auth_users_capabilities.sql
--
-- DEC-A33 — auth spine for asset-pipeline MVP. Users + capability matrix as
-- data + append-only audit_log. Hosted-ready columns: users.org_id reserved
-- (NULL until multi-tenant), users.retired_at soft-retire.
--
-- Idempotent: IF NOT EXISTS on tables/indexes; ON CONFLICT DO NOTHING on
-- seeds. Re-running migrate after first apply is a no-op.

BEGIN;

CREATE EXTENSION IF NOT EXISTS citext;

-- users — hosted-ready (org_id reserved); single-admin MVP defaults role='admin'.
CREATE TABLE IF NOT EXISTS users (
  id              uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  email           citext      UNIQUE NOT NULL,
  display_name    text        NOT NULL,
  role            text        NOT NULL DEFAULT 'admin',
  org_id          uuid        NULL,
  last_login_at   timestamptz NULL,
  created_at      timestamptz NOT NULL DEFAULT now(),
  retired_at      timestamptz NULL
);

-- capability — DEC-A33 capability matrix as data.
CREATE TABLE IF NOT EXISTS capability (
  capability_id   text PRIMARY KEY
);

-- role_capability — role -> capability mapping (composite PK).
CREATE TABLE IF NOT EXISTS role_capability (
  role            text NOT NULL,
  capability_id   text NOT NULL REFERENCES capability(capability_id),
  PRIMARY KEY (role, capability_id)
);

-- audit_log — append-only mutation trail; no UPDATE/DELETE API by convention.
-- target_id is text (not uuid) so it can carry either uuid (users) or bigserial
-- (catalog_asset) ids depending on target_kind. Caller normalises to string.
CREATE TABLE IF NOT EXISTS audit_log (
  id              bigserial   PRIMARY KEY,
  actor_user_id   uuid        REFERENCES users(id),
  action          text        NOT NULL,
  target_kind     text        NULL,
  target_id       text        NULL,
  payload         jsonb       NULL,
  created_at      timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS audit_log_actor_created_idx
  ON audit_log (actor_user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS audit_log_target_idx
  ON audit_log (target_kind, target_id);

-- Seed capability set (DEC-A33).
INSERT INTO capability (capability_id) VALUES
  ('catalog.entity.create'),
  ('catalog.entity.edit'),
  ('catalog.entity.publish'),
  ('catalog.entity.retire'),
  ('catalog.entity.delete'),
  ('catalog.snapshot.export'),
  ('catalog.snapshot.retire'),
  ('render.run'),
  ('preview.unity_push'),
  ('lint.config_edit'),
  ('auth.role_assign'),
  ('gc.trigger'),
  ('audit.read')
ON CONFLICT (capability_id) DO NOTHING;

-- Seed role_capability — admin gets all 13.
INSERT INTO role_capability (role, capability_id)
  SELECT 'admin', capability_id FROM capability
ON CONFLICT (role, capability_id) DO NOTHING;

-- Seed role_capability — author (6 rows).
INSERT INTO role_capability (role, capability_id) VALUES
  ('author', 'catalog.entity.create'),
  ('author', 'catalog.entity.edit'),
  ('author', 'catalog.entity.publish'),
  ('author', 'render.run'),
  ('author', 'preview.unity_push'),
  ('author', 'lint.config_edit')
ON CONFLICT (role, capability_id) DO NOTHING;

-- Seed role_capability — viewer (1 row).
INSERT INTO role_capability (role, capability_id) VALUES
  ('viewer', 'audit.read')
ON CONFLICT (role, capability_id) DO NOTHING;

COMMIT;
