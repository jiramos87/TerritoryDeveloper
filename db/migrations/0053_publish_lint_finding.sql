-- 0053_publish_lint_finding.sql
--
-- asset-pipeline Stage 15.1 / TECH-4183.
-- DEC-A23 (lenient soft FK) + DEC-A42 (dashboard catalog read).
--
-- Persisted lint finding rows emitted on entity publish by
-- `web/lib/lint/finding-recorder.ts`. Consumed by the Stage 15.1
-- Dashboard lint-failures widget (last 10 fail rows).
--
-- Idempotent: IF NOT EXISTS on table/index; re-applying is a no-op.
-- No data seeds.

BEGIN;

CREATE TABLE IF NOT EXISTS publish_lint_finding (
  id                bigserial     PRIMARY KEY,
  entity_id         bigint        NOT NULL,
  entity_version_id bigint        NOT NULL,
  rule_id           text          NOT NULL,
  severity          text          NOT NULL,
  status            text          NOT NULL CHECK (status IN ('pass', 'fail', 'warn')),
  message           text          NOT NULL,
  created_at        timestamptz   NOT NULL DEFAULT now()
);

COMMENT ON TABLE publish_lint_finding IS
  'Persisted lint findings emitted per publish (TECH-4183, Stage 15.1). Soft FK to catalog_entity + entity_version (DEC-A23).';
COMMENT ON COLUMN publish_lint_finding.entity_id IS
  'Source catalog_entity.id — soft FK, no SQL constraint (DEC-A23).';
COMMENT ON COLUMN publish_lint_finding.entity_version_id IS
  'Source entity_version.id — soft FK, no SQL constraint (DEC-A23).';
COMMENT ON COLUMN publish_lint_finding.severity IS
  'Rule severity at evaluation time: block | warn | info.';
COMMENT ON COLUMN publish_lint_finding.status IS
  'Outcome after justification: fail (block severity), warn (warn severity), pass (info).';

-- Dashboard query: last 10 fail rows ordered DESC.
CREATE INDEX IF NOT EXISTS publish_lint_finding_status_created_idx
  ON publish_lint_finding (status, created_at DESC);

COMMIT;

-- Rollback (manual, not auto-run):
--   BEGIN;
--   DROP INDEX IF EXISTS publish_lint_finding_status_created_idx;
--   DROP TABLE IF EXISTS publish_lint_finding;
--   COMMIT;
