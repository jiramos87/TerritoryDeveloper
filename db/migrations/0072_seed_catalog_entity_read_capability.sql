-- 0072_seed_catalog_entity_read_capability.sql
-- Adds `catalog.entity.read` capability referenced by dashboard widget routes
-- (TECH-4183 / asset-pipeline-stage-15.1) and granted to all three default
-- roles (admin / author / viewer). Routes that reference this capability:
--
--   web/app/api/catalog/dashboard/unresolved-refs/route.ts
--   web/app/api/catalog/dashboard/snapshot-freshness/route.ts
--   web/app/api/catalog/dashboard/lint-failures/route.ts
--   web/app/api/catalog/dashboard/queue-depth/route.ts
--
-- DEC-A33 capability matrix; gate = `validate:capability-coverage`.

BEGIN;

INSERT INTO capability (capability_id) VALUES
  ('catalog.entity.read')
ON CONFLICT (capability_id) DO NOTHING;

INSERT INTO role_capability (role, capability_id) VALUES
  ('admin',  'catalog.entity.read'),
  ('author', 'catalog.entity.read'),
  ('viewer', 'catalog.entity.read')
ON CONFLICT (role, capability_id) DO NOTHING;

COMMIT;
