-- 0148_catalog_panel_anchors.sql
-- Layer 1 author-gate: view-slot anchor required-by (TECH-28360) +
-- bind-id author registry (TECH-28358).
--
-- catalog_panel_anchors: panels declaring views[] must have a row here
-- per view (slot_name), recording which panels require the anchor
-- (required_by_panels[]). Prevents F2-class slot drift.
--
-- ia_ui_bind_registry: author-time registry of bind ids. Distinct from
-- bind_registry_log (runtime snapshot). Populated via declare_on_publish
-- flag on catalog_panel_publish; queried by validateBindIdContract gate.

BEGIN;

CREATE TABLE IF NOT EXISTS catalog_panel_anchors (
  panel_slug         text     NOT NULL,
  slot_name          text     NOT NULL,
  required_by_panels text[]   NOT NULL DEFAULT '{}',
  PRIMARY KEY (panel_slug, slot_name)
);

COMMENT ON TABLE catalog_panel_anchors IS
  'Layer 1 author-gate (TECH-28360) — each views[] entry in a published panel '
  'must have a row here declaring the anchor slot and which panels require it. '
  'Prevents F2-class slot drift (wrong scene anchor).';

CREATE TABLE IF NOT EXISTS ia_ui_bind_registry (
  bind_id          text        PRIMARY KEY,
  owner_panel_slug text        NOT NULL,
  declared_at      timestamptz NOT NULL DEFAULT now()
);

COMMENT ON TABLE ia_ui_bind_registry IS
  'Layer 1 author-gate (TECH-28358) — author-time bind id registry. '
  'Populated via declare_on_publish=true flag on catalog_panel_publish. '
  'Distinct from bind_registry_log (runtime snapshot via Editor bridge).';

DO $$
BEGIN
  RAISE NOTICE '0148 OK: catalog_panel_anchors + ia_ui_bind_registry created (Layer 1 author gates)';
END;
$$;

COMMIT;

-- Rollback (dev only):
--   DROP TABLE IF EXISTS ia_ui_bind_registry;
--   DROP TABLE IF EXISTS catalog_panel_anchors;
