-- Stage arch_surfaces refinement (Stage 1.2 / TECH-2199).
--
-- Stage 1.1 migration 0034_architecture_index.sql created the
-- `stage_arch_surfaces` link table per DEC-A12 (`plan-arch-link-stage-level`)
-- and DEC-A16 (`db-relations-four-tables`) with PK (stage_id, surface_slug).
-- That shape conflates stages across master plans (stage_id `1.1` is reused
-- across many plans), so this migration adds the missing `slug` column,
-- reshapes the PK to (slug, stage_id, surface_slug), and wires a composite
-- FK to ia_stages(slug, stage_id) — matching DEC-A12 "stage-level" intent
-- (a Stage is identified by `(slug, stage_id)`, not raw stage_id).
--
-- Also adds the supporting index `stage_arch_surfaces_surface_slug_idx`
-- for reverse lookup (resolve all stages by surface slug).
--
-- Migration slot 0036 — slot 0035 was claimed by `0035_token_detail.sql`
-- (sibling work stream); sequential numbering enforced by `tools/postgres-ia`.
--
-- Idempotent: gated DO blocks check pg_catalog state before mutating, so
-- re-runs produce zero schema diff. Safe in production because the link
-- table is empty (Stage 1.1 seed left it blank for Stage 1.2 backfill).

BEGIN;

-- 1. Add `slug` column on stage_arch_surfaces (no NULL data → fill empty
--    string default to allow ADD COLUMN, then drop default).
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
     WHERE table_schema = 'public'
       AND table_name = 'stage_arch_surfaces'
       AND column_name = 'slug'
  ) THEN
    ALTER TABLE stage_arch_surfaces
      ADD COLUMN slug text NOT NULL DEFAULT '';
    ALTER TABLE stage_arch_surfaces
      ALTER COLUMN slug DROP DEFAULT;
  END IF;
END $$;

-- 2. Reshape PK from (stage_id, surface_slug) to (slug, stage_id, surface_slug).
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM pg_constraint
     WHERE conname = 'stage_arch_surfaces_pkey'
       AND conrelid = 'public.stage_arch_surfaces'::regclass
  ) THEN
    -- Detect old shape (2 cols) — drop only if it does NOT include `slug`.
    IF NOT EXISTS (
      SELECT 1
        FROM pg_constraint c
        JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = ANY(c.conkey)
       WHERE c.conname = 'stage_arch_surfaces_pkey'
         AND c.conrelid = 'public.stage_arch_surfaces'::regclass
         AND a.attname = 'slug'
    ) THEN
      ALTER TABLE stage_arch_surfaces DROP CONSTRAINT stage_arch_surfaces_pkey;
    END IF;
  END IF;

  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint
     WHERE conname = 'stage_arch_surfaces_pkey'
       AND conrelid = 'public.stage_arch_surfaces'::regclass
  ) THEN
    ALTER TABLE stage_arch_surfaces
      ADD CONSTRAINT stage_arch_surfaces_pkey
        PRIMARY KEY (slug, stage_id, surface_slug);
  END IF;
END $$;

-- 3. Composite FK on (slug, stage_id) → ia_stages(slug, stage_id) ON DELETE CASCADE.
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint
     WHERE conname = 'stage_arch_surfaces_stage_fk'
       AND conrelid = 'public.stage_arch_surfaces'::regclass
  ) THEN
    ALTER TABLE stage_arch_surfaces
      ADD CONSTRAINT stage_arch_surfaces_stage_fk
        FOREIGN KEY (slug, stage_id)
        REFERENCES ia_stages (slug, stage_id) ON DELETE CASCADE;
  END IF;
END $$;

-- 4. Reverse-lookup index by surface_slug (forward lookup is covered by PK).
CREATE INDEX IF NOT EXISTS stage_arch_surfaces_surface_slug_idx
  ON stage_arch_surfaces (surface_slug);

COMMIT;
