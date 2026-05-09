-- 0120_seed_city_name_pool_es.sql
-- Wave A2 (TECH-27068) — NEW catalog kind `string-pool`.
-- 100 fictional Spanish city names seeded as pool-row(kind=string-pool, lang=es).
--
-- Idempotent: ON CONFLICT DO NOTHING throughout.

BEGIN;

-- ─── 1. catalog_entity row (kind=string-pool) ─────────────────────────────────

INSERT INTO catalog_entity (kind, slug, display_name)
VALUES ('string-pool', 'city-name-pool-es', 'City Name Pool (ES)')
ON CONFLICT (kind, slug) DO NOTHING;

-- ─── 2. string_pool_row rows — 100 fictional Spanish names ───────────────────

INSERT INTO string_pool_row (entity_id, lang, value)
SELECT ce.id, 'es', m.name
FROM (VALUES
  ('Altaverde'),
  ('Barrancosal'),
  ('Camporreal'),
  ('Duendefuente'),
  ('Espigamonte'),
  ('Fuenteclara'),
  ('Grandarroyo'),
  ('Huertavieja'),
  ('Islaseca'),
  ('Jardinalta'),
  ('Lagopiedra'),
  ('Marisalina'),
  ('Nogalazul'),
  ('Olmopardo'),
  ('Pedregosa'),
  ('Quintaloma'),
  ('Rinconcito'),
  ('Salviaverde'),
  ('Torredorada'),
  ('Umbriablanca'),
  ('Vallebello'),
  ('Xaralosa'),
  ('Yerbabuena'),
  ('Zarcillos'),
  ('Arenaluna'),
  ('Brisaflor'),
  ('Cascajillos'),
  ('Dehesaroja'),
  ('Encinalmiel'),
  ('Fresnogrande'),
  ('Granadilla'),
  ('Herbazal'),
  ('Irisamor'),
  ('Juncalverde'),
  ('Lanternamar'),
  ('Malvavisco'),
  ('Naranjales'),
  ('Olivarzul'),
  ('Pinarcanto'),
  ('Querencioso'),
  ('Retamarosa'),
  ('Sabinareal'),
  ('Thymaverde'),
  ('Uvaroja'),
  ('Verbenaval'),
  ('Wistariasol'),
  ('Xericoto'),
  ('Yunquecillo'),
  ('Zarcoflor'),
  ('Aguamanantial'),
  ('Bellotatierna'),
  ('Cerezuelos'),
  ('Daliaroja'),
  ('Estepafresca'),
  ('Florimonte'),
  ('Gironaval'),
  ('Higuerosal'),
  ('Indigoblanco'),
  ('Jazminero'),
  ('Kamarinaalta'),
  ('Lupinarejo'),
  ('Mimosaverde'),
  ('Neblinaval'),
  ('Orquidealuz'),
  ('Palmaroyal'),
  ('Quercusmar'),
  ('Romeroazul'),
  ('Saucedofresco'),
  ('Tagetesverde'),
  ('Uvamadre'),
  ('Vetiververde'),
  ('Xaraizalinde'),
  ('Yerbasanta'),
  ('Zarzamorena'),
  ('Alcocebre'),
  ('Bramadero'),
  ('Cantarilla'),
  ('Dessiecillo'),
  ('Enzinablanca'),
  ('Fontanela'),
  ('Garruchilla'),
  ('Higuerazul'),
  ('Ibericota'),
  ('Jabalinejo'),
  ('Kumquatosa'),
  ('Laurealinda'),
  ('Melocotales'),
  ('Noguerado'),
  ('Orovalles'),
  ('Peralverde'),
  ('Quelitosol'),
  ('Robledazul'),
  ('Senderoverde'),
  ('Torronteras'),
  ('Ulmofresco'),
  ('Vinagremar'),
  ('Winterbrisa'),
  ('Xerocanto'),
  ('Ylang-ylangar'),
  ('Zarabandilla')
) AS m(name)
JOIN catalog_entity ce ON ce.kind = 'string-pool' AND ce.slug = 'city-name-pool-es'
ON CONFLICT (entity_id, lang, value) DO NOTHING;

-- ─── 3. entity_version + publish ─────────────────────────────────────────────

INSERT INTO entity_version (entity_id, version_number, status, params_json, lint_overrides_json, migration_hint_json)
SELECT
  ce.id, 1, 'published', '{}'::jsonb, '{}'::jsonb,
  '{"migration":"0120_seed_city_name_pool_es","event":"initial_seed"}'::jsonb
FROM catalog_entity ce
WHERE ce.kind = 'string-pool' AND ce.slug = 'city-name-pool-es'
  AND NOT EXISTS (SELECT 1 FROM entity_version ev WHERE ev.entity_id = ce.id);

UPDATE catalog_entity ce
SET current_published_version_id = ev.id
FROM entity_version ev
WHERE ev.entity_id = ce.id
  AND ev.version_number = 1
  AND ce.kind = 'string-pool' AND ce.slug = 'city-name-pool-es'
  AND ce.current_published_version_id IS NULL;

-- ─── 4. Sanity assertions ─────────────────────────────────────────────────────

DO $$
DECLARE
  n_names int;
BEGIN
  SELECT COUNT(*) INTO n_names
  FROM string_pool_row spr
  JOIN catalog_entity ce ON ce.id = spr.entity_id
  WHERE ce.kind = 'string-pool' AND ce.slug = 'city-name-pool-es'
    AND spr.lang = 'es';

  IF n_names < 100 THEN
    RAISE EXCEPTION '0120: expected >=100 ES city names, got %', n_names;
  END IF;

  RAISE NOTICE '0120 OK: city-name-pool-es seeded with % names', n_names;
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DELETE FROM string_pool_row WHERE entity_id = (SELECT id FROM catalog_entity WHERE kind='string-pool' AND slug='city-name-pool-es');
--   DELETE FROM entity_version WHERE entity_id = (SELECT id FROM catalog_entity WHERE kind='string-pool' AND slug='city-name-pool-es');
--   DELETE FROM catalog_entity WHERE kind='string-pool' AND slug='city-name-pool-es';
