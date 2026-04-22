# Grid asset visual registry — Stage 2.3 plan digest (aggregate)

**Orchestrator:** `ia/projects/grid-asset-visual-registry-master-plan.md`  
**Stage:** 2.3 — Zone S consumer migration  
**Filed tasks (archived):** [TECH-684](../ia/backlog-archive/TECH-684.yaml) · [TECH-685](../ia/backlog-archive/TECH-685.yaml) · [TECH-686](../ia/backlog-archive/TECH-686.yaml) · [TECH-687](../ia/backlog-archive/TECH-687.yaml)

## Stage intent (from master plan)

`ZoneSubTypeRegistry` becomes the consumer of `GridAssetCatalog` for **costs**, **display names**, and **sprite** paths; seven legacy `0..6` `subTypeId` values map to catalog `asset_id` PKs; callers and EditMode tests align with the published snapshot and `db` seed.

## Task index (executable digest location)

| Task | Archive | Short outcome |
|------|---------|----------------|
| T2.3.1 | [TECH-684](../ia/backlog-archive/TECH-684.yaml) | `[SerializeField]` + `FindObjectOfType<GridAssetCatalog>()` in `Awake`; `internal Catalog` getter |
| T2.3.2 | [TECH-685](../ia/backlog-archive/TECH-685.yaml) | `int[7]` `SubTypeIdToAssetId` + `TryGetAssetIdForSubType` (literals from seed) |
| T2.3.3 | [TECH-686](../ia/backlog-archive/TECH-686.yaml) | `SubTypePickerModal` + `ZoneSService` use catalog cents / façade |
| T2.3.4 | [TECH-687](../ia/backlog-archive/TECH-687.yaml) | Snapshot fragment JSON + `ZoneSubTypeRegistryCatalogBackedTests` for seven ids |

## Implementation order (dependency)

1. **TECH-684** then **TECH-685** (same file `ZoneSubTypeRegistry.cs` — merge conflicts avoided by sequential PR or single branch).
2. **TECH-686** (adds `TryGetPickerLabelForSubType` or equivalent in registry if not split out; wires UI/services).
3. **TECH-687** (asserts end state after 684–686).

**Hard depends_on (backlog):** all four list **TECH-672** (archived) — boot `GridAssetCatalog` path.

## Verification gate (Stage end, not this filing step)

- `npm run unity:compile-check` after C#.
- Run EditMode test assemblies touched by TECH-687.
- Full `verify-loop` / `ship-stage` per orchestrator (outside this `stage-file` chain).

## Provenance

Generated during `/stage-file-main-session` for Stage 2.3. Per-task mechanical steps and gates live under each task spec `## §Plan Digest` section.
