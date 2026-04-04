-- TECH-55b — full document JSONB + optional backlog_issue_id (nullable).
-- Apply after 0004 via tools/postgres-ia/apply-migrations.mjs

BEGIN;

-- Allow optional metadata label (unlabeled exports use NULL).
ALTER TABLE editor_export_agent_context ALTER COLUMN backlog_issue_id DROP NOT NULL;
ALTER TABLE editor_export_sorting_debug ALTER COLUMN backlog_issue_id DROP NOT NULL;
ALTER TABLE editor_export_terrain_cell_chunk ALTER COLUMN backlog_issue_id DROP NOT NULL;
ALTER TABLE editor_export_world_snapshot_dev ALTER COLUMN backlog_issue_id DROP NOT NULL;

ALTER TABLE editor_export_agent_context ADD COLUMN IF NOT EXISTS document jsonb;
ALTER TABLE editor_export_sorting_debug ADD COLUMN IF NOT EXISTS document jsonb;
ALTER TABLE editor_export_terrain_cell_chunk ADD COLUMN IF NOT EXISTS document jsonb;
ALTER TABLE editor_export_world_snapshot_dev ADD COLUMN IF NOT EXISTS document jsonb;

UPDATE editor_export_agent_context
SET document = jsonb_build_object('legacy_registry_row', true, 'payload', payload)
WHERE document IS NULL;

UPDATE editor_export_sorting_debug
SET document = jsonb_build_object('legacy_registry_row', true, 'payload', payload)
WHERE document IS NULL;

UPDATE editor_export_terrain_cell_chunk
SET document = jsonb_build_object('legacy_registry_row', true, 'payload', payload)
WHERE document IS NULL;

UPDATE editor_export_world_snapshot_dev
SET document = jsonb_build_object('legacy_registry_row', true, 'payload', payload)
WHERE document IS NULL;

ALTER TABLE editor_export_agent_context ALTER COLUMN document SET NOT NULL;
ALTER TABLE editor_export_sorting_debug ALTER COLUMN document SET NOT NULL;
ALTER TABLE editor_export_terrain_cell_chunk ALTER COLUMN document SET NOT NULL;
ALTER TABLE editor_export_world_snapshot_dev ALTER COLUMN document SET NOT NULL;

CREATE INDEX IF NOT EXISTS editor_export_agent_context_document_gin
  ON editor_export_agent_context USING gin (document jsonb_path_ops);
CREATE INDEX IF NOT EXISTS editor_export_sorting_debug_document_gin
  ON editor_export_sorting_debug USING gin (document jsonb_path_ops);
CREATE INDEX IF NOT EXISTS editor_export_terrain_cell_chunk_document_gin
  ON editor_export_terrain_cell_chunk USING gin (document jsonb_path_ops);
CREATE INDEX IF NOT EXISTS editor_export_world_snapshot_dev_document_gin
  ON editor_export_world_snapshot_dev USING gin (document jsonb_path_ops);

COMMENT ON COLUMN editor_export_agent_context.document IS
  'TECH-55b: full export body (JSON object). Legacy rows use legacy_registry_row + payload.';
COMMENT ON COLUMN editor_export_sorting_debug.document IS
  'TECH-55b: {"format":"markdown","body":"..."} or legacy wrapper.';

COMMIT;
