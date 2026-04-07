-- IDE agent bridge (Postgres): MCP enqueues jobs; Unity dequeues via Node scripts.

BEGIN;

CREATE TABLE IF NOT EXISTS agent_bridge_job (
  id              bigserial PRIMARY KEY,
  command_id      uuid NOT NULL UNIQUE,
  kind            text NOT NULL,
  status          text NOT NULL,
  request         jsonb NOT NULL,
  response        jsonb,
  error           text,
  created_at      timestamptz NOT NULL DEFAULT now(),
  updated_at      timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT agent_bridge_job_status_check CHECK (
    status IN ('pending', 'processing', 'completed', 'failed')
  )
);

CREATE INDEX IF NOT EXISTS agent_bridge_job_status_created_idx
  ON agent_bridge_job (status, created_at);

COMMENT ON TABLE agent_bridge_job IS
  'IDE agent bridge — territory-ia MCP inserts pending rows; Unity Editor runs agent-bridge-dequeue.mjs / agent-bridge-complete.mjs.';

COMMIT;
