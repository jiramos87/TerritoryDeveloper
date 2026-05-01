---
purpose: "Scene contract — canonical Unity scene paths the asset-pipeline bridge composite resolves against."
audience: agent
loaded_by: on-demand
---

# Asset Pipeline — Scene Contract

> Canonical Unity scene path surfaces the IDE agent bridge composite `wire_asset_from_catalog` resolves against. Pre-mutation rejection contract: any offered path that fails template match emits the structured error envelope shape (DEC-A48) **before** any GameObject mutation or asset write.

Cross-refs: [`asset-pipeline-architecture.md §DEC-A4`](asset-pipeline-architecture.md) (catalog spine + per-kind detail tables) and [`asset-pipeline-architecture.md §DEC-A48`](asset-pipeline-architecture.md) (structured error envelope). Stage 19.3 / Master plan: `asset-pipeline`.

## 1. Scope

The bridge composite `wire_asset_from_catalog` (TECH-1591) accepts an `entity_id` + a target scene location and instantiates the catalog-resolved prefab under one canonical mount. To bound the surface area + reject malformed mounts deterministically, the bridge resolves the offered location against a fixed set of templates. This doc enumerates that set.

Out of scope: asset import paths under `Assets/Prefabs/Catalog/{slug}.prefab` (asset-side, not scene-side); ScriptableObject mounts; non-UI / non-cell GameObjects.

## 2. Canonical path surfaces

Four surfaces. Each entry: name, template, owner subsystem, bridge resolution behavior.

### 2.1 Toolbar buttons — `Canvas/Toolbar/ZoneButtons`

- **Template**: `Canvas/Toolbar/ZoneButtons` (literal — no substitution).
- **Owner**: `UIManager` (zone toolbar mount).
- **Bridge resolution**: `GameObject.Find("Canvas/Toolbar/ZoneButtons")` returns the live mount. Composite kind `wire_asset_from_catalog` rejects with `unknown_scene_path` when the mount is absent (e.g. UIManager bootstrap not yet primed in the active scene).
- **Wire intent**: zone-button prefabs (catalog `kind=button`) drop here as children; sibling order = catalog ordinal.

### 2.2 Panel tiers — `Canvas/Panels/{tier}`

- **Template**: `Canvas/Panels/{tier}` where `{tier} ∈ {floating, modal, overlay}`.
- **Owner**: `UIManager` (ThemedPanel tier system per `Assets/Scripts/UI/Themed/ThemedPanel.cs`).
- **Bridge resolution**: composite substitutes `{tier}` from the offered token + verifies the resolved path resolves a live `Transform` via `GameObject.Find`. Unknown tier strings (`{tier}` ∉ {`floating`,`modal`,`overlay`}) reject with `unknown_scene_path`.
- **Wire intent**: ThemedPanel-conformant panel prefabs (catalog `kind=panel`) mount under their declared tier; tier change between two wire calls = explicit re-parent + re-wire (caller responsibility).

### 2.3 World cells — `World/GridRoot/Cells/{cell_xy}`

- **Template**: `World/GridRoot/Cells/{cell_xy}` where `{cell_xy}` = compact `"<x>_<y>"` token (e.g. `5_7`).
- **Owner**: `GridManager` (per-cell GameObject parent established at geography init time).
- **Bridge resolution**: composite substitutes `{cell_xy}` from the input arg + resolves via `GameObject.Find`. Absent cell parent (cell coordinate out of grid bounds OR cell GameObject not yet instantiated) rejects with `unknown_scene_path`.
- **Wire intent**: per-cell catalog prefabs (catalog `kind=building` / `prop`) instantiate under the canonical cell parent; parent-relative transform owned by prefab.

### 2.4 Bootstrap — `Bootstrap/UIManager`

- **Template**: `Bootstrap/UIManager` (literal — no substitution).
- **Owner**: scene bootstrap (`UIManager` MonoBehaviour singleton — Unity invariant 4: scene-singleton via `FindObjectOfType`, not `DontDestroyOnLoad`).
- **Bridge resolution**: read-only mount used as a parent reference for tooltip / overlay prefabs that do not belong to a panel tier. Same `GameObject.Find` resolution + `unknown_scene_path` rejection rules apply.
- **Wire intent**: bootstrap-scope catalog entries (catalog `kind=tooltip`) parent here when no tier mount applies.

## 3. Unknown-path rejection contract

Bridge composite `wire_asset_from_catalog` rejects offered scene paths that fail canonical template match **before** any mutation. Envelope shape mirrors DEC-A48:

```
{
  "ok": false,
  "error": "unknown_scene_path",
  "path": "<offered scene path>"
}
```

Reject conditions:

- Offered path does not match any of the four templates above (literal or `{tier}` / `{cell_xy}` substitution).
- Offered path matches a template but the resolved `GameObject.Find` returns null (mount missing in active scene — bootstrap not primed, cell not yet built, panel tier root not initialized).
- Offered `{tier}` substitution falls outside `{floating, modal, overlay}` (panel tiers are closed enum).

Reject is pre-mutation: no `PrefabUtility.InstantiatePrefab` call, no snapshot capture, no asset write. Snapshot capture (TECH-1592 / `CellSubtreeSnapshot.Capture`) only fires AFTER path resolution succeeds and BEFORE mutation. Caller receives the envelope synchronously.

`dry_run=true` invocation still enforces this contract — the proposed `mutations[]` descriptor is meaningless without a resolvable mount, so dry-run also rejects with `unknown_scene_path`.

## 4. Composite kind boundaries

This contract applies to the `wire_asset_from_catalog` composite kind. Other bridge composite kinds (e.g. `bake_ui`, future `place_landscape_prop`) define their own scene contracts. Path templates in this doc are NOT shared with non-wire composites.

When a new composite kind needs a new canonical scene path surface, the path must be added here before the composite ships — pre-mutation `unknown_scene_path` rejection on the new path becomes the wire-time gate.

## 5. Future extensions (Stage 20.x and beyond)

Not part of Stage 19.3 acceptance — listed for orientation only:

- **Pre-resolve / mutate split** — currently `WireAssetFromCatalog.Run` bakes resolve + mutate in one call; Stage 20.x may split into two halves so snapshot capture sees pure pre-mutation state. Escalation enum: `snapshot_phase_split`.
- **Panel sub-mounts** — `Canvas/Panels/{tier}/{slot}` for slot-aware wire (e.g. `floating/header`, `modal/footer`). Out of MVP scope.
- **Per-row metadata** — when `CatalogEntity` exposes `world_sprite` / `has_button` columns, the composite will use them for prefab path resolution instead of the current synthesized `Assets/Prefabs/Catalog/{slug}.prefab` path.
