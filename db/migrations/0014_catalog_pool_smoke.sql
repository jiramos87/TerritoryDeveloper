-- Optional spawn-pool smoke row (TECH-617 / T1.1.6): proves catalog_pool_member write path.

BEGIN;

INSERT INTO catalog_spawn_pool (slug, owner_category, owner_subtype)
VALUES ('smoke_zone_s_tool', 'zone_s', 'registry_smoke')
ON CONFLICT (slug) DO NOTHING;

INSERT INTO catalog_pool_member (pool_id, asset_id, weight)
SELECT p.id, 0, 100
FROM catalog_spawn_pool p
WHERE p.slug = 'smoke_zone_s_tool'
ON CONFLICT (pool_id, asset_id) DO NOTHING;

COMMIT;
