---
purpose: "Explore moving UI panel child component tree from prefab/bake JSON into DB-owned schema (ia_panel_nodes table) so MCP queries return complete panel definition."
audience: agent
loaded_by: on-demand
created_at: 2026-05-11
related_docs:
  - ia/specs/architecture/interchange.md
  - web/lib/design-system.md
related_mcp_slices:
  - ui_panel_get
  - ui_panel_list
  - ui_component_get
  - ui_component_list
  - ui_def_drift_scan
---

# UI panel tree — DB storage exploration

## §Discussion abstract (2026-05-11)

Surfaced during stats-panel definition lookup. Initial premise (tree-not-in-DB) **wrong** — corrected after grep sweep:

- DB **already owns** child tree via `panel_child` table (mig `0031_panel_detail_and_child.sql`, 2026): `(panel_entity_id, panel_version_id, slot_name, order_idx, child_kind, child_entity_id, child_version_id, params_json)` with unique `(panel, slot, order)`.
- DB **already owns** panel shell via `panel_detail` (1:1 with `catalog_entity` kind=panel): archetype binding, sprite + token slots, layout_template, modal flag.
- Stats-panel seeded specifically via `0137_seed_stats_panel.sql`; 21 children visible in `Assets/UI/Snapshots/panels.json` bake output (header, close, tab-strip, range-tabs, 3 charts, 3 stacked-bar-rows, 11 service-rows).
- Bake pipeline (`tools/scripts/snapshot-export-game-ui.mjs` + `Assets/Scripts/Editor/Bridge/UiBakeHandler.cs`) is already DB → JSON → prefab — flow exists.
- Drift gates exist: `validate-ui-def-drift.mjs`, `validate-panel-blueprint-harness.mjs`, `validate-ui-id-consistency.mjs`, MCP `ui_def_drift_scan`.

**Real gap = MCP slice surface, not schema.** `ui_panel_get` payload returns shell only (rect, layout, padding, params, modal) — does NOT include `children[]`. Agents querying via MCP see half-spec even though DB holds full tree. `panel_consumers[]` on `ui_component_get` empty because resolver doesn't traverse `panel_child.child_entity_id`.

**Recommendation (tentative).** Extend `ui_panel_get` slice: JOIN `panel_child` ON `panel_entity_id`, return `children[]` array (ordered by `slot_name, order_idx`) with resolved `child_entity` (slug + display_name + kind). Optional recursive flag for nested panel children. Fix `ui_component_get.panel_consumers[]` to query `panel_child WHERE child_entity_id = ?`.

**Cross-link to atomization.** UI Domain (`Domains/UI/`) currently underported (ThemeService 530 LOC = Tier E split target, UiBakeHandler family Tier D). Slice extension is orthogonal — pure MCP-server work in `tools/mcp-ia-server/src/tools/ui-panel-get.ts`; does NOT block on hub-thinning.

## §Open questions

- **Q1 — slice payload shape.** Flat `children[]` (single SELECT, params_json raw) OR nested resolved (recursive JOIN on `child_entity_id` → component spine)? Trade: flat = cheap + drift-honest; nested = ergonomic for agents but hides version mismatches.
- **Q2 — version pinning.** `ui_panel_get` returns children at `panel_version_id` snapshot (frozen tree per published version) OR live latest (HEAD of `panel_child` for the panel)? Bake pipeline currently uses live; calibration corpus may need frozen.
- **Q3 — `panel_consumers[]` fix scope.** Just query `panel_child WHERE child_entity_id = ?` (covers direct nesting) OR also walk `params_json` for slug refs (covers loose coupling, expensive)?
- **Q4 — `child_kind` vocabulary.** Current CHECK: `button, panel, label, spacer, audio, sprite, label_inline` — bake snapshot shows `tab-strip, range-tabs, chart, stacked-bar-row, service-row` (richer). Where do those route through? Generic `panel` with `params_json.kind`? Need schema audit before extending slice.
- **Q5 — drift gate coverage.** Once slice exposes children, should `ui_def_drift_scan` MCP add a tree-drift check (DB tree vs bake snapshot)? Currently file-level only.
- **Q6 — corpus rows surface.** `ui_panel_get.corpus_rows[]` already in payload (returned `[]` for stats-panel) — does children extension change calibration corpus indexing?

## §Constraints

- MCP-first directive (force-loaded `invariants.md`): UI surface queries must resolve via DB slice, not file read.
- `ui_def_drift_scan` existence implies DB ↔ runtime parity contract — currently uncoverable for tree (no slice exposure).
- Schema is locked + populated — change scope is MCP-server only (`tools/mcp-ia-server/src/tools/ui-panel-get.ts`, `ui-component-get.ts`) + slice tests (`tools/mcp-ia-server/tests/tools/ui-slices.test.ts`).
- `child_kind` CHECK constraint is narrow (7 values) vs bake output (richer set) — investigate before extending.

## §Hand-off

Run `/design-explore docs/explorations/ui-panel-tree-db-storage.md` to expand, resolve Q1–Q6, then `/project-new` (likely TECH-* single-issue scope, NOT a full master plan — schema unchanged, just MCP slice work). Runs parallel to `large-file-atomization-hub-thinning-sweep`.
