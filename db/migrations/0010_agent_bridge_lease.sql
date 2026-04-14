-- Multi-agent Play Mode lease: one active lease per kind at a time (crash-safe via expires_at TTL).
-- Also adds agent_id audit column to agent_bridge_job for multi-agent tracing.

BEGIN;

CREATE TABLE IF NOT EXISTS agent_bridge_lease (
  id          bigserial   PRIMARY KEY,
  lease_id    uuid        NOT NULL DEFAULT gen_random_uuid(),
  agent_id    text        NOT NULL,
  kind        text        NOT NULL CHECK (kind IN ('play_mode')),
  status      text        NOT NULL DEFAULT 'active'
                          CHECK (status IN ('active', 'released', 'expired')),
  acquired_at timestamptz NOT NULL DEFAULT now(),
  expires_at  timestamptz NOT NULL,
  released_at timestamptz
);

-- Only one active lease per kind at a time (partial unique index).
-- Acquire via INSERT ... ON CONFLICT DO NOTHING; rowCount 0 = another agent holds it.
CREATE UNIQUE INDEX IF NOT EXISTS uq_bridge_lease_one_active
  ON agent_bridge_lease (kind)
  WHERE status = 'active';

COMMENT ON TABLE agent_bridge_lease IS
  'Play Mode ownership lease — one active per kind; expires_at provides TTL crash-safety for multi-agent sessions. Sweep expired rows via: UPDATE agent_bridge_lease SET status=''expired'' WHERE status=''active'' AND expires_at < now().';

-- Audit column: which agent enqueued each bridge job (non-breaking; legacy rows get default).
ALTER TABLE agent_bridge_job
  ADD COLUMN IF NOT EXISTS agent_id text NOT NULL DEFAULT 'anonymous';

COMMIT;
