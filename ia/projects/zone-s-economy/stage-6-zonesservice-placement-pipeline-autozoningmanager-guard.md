### Stage 6 — Economy services: bond ledger + maintenance registry + Zone S placement / `ZoneSService` placement pipeline + `AutoZoningManager` guard

<!-- sizing-gate-waiver: H5+H6 WARN (GridManager + ZoneSService overlap across tasks); user /stage-file 2026-04-20 -->

**Status:** Final

**Objectives:** Land the single entry point for Zone S placement. `ZoneSService.PlaceStateServiceZone` wraps the envelope draw + `ZoneManager.PlaceZone` route + growth-pipeline contributor registration. Guard `AutoZoningManager` from placing S (manual-only in MVP). End of Step 2: exploration Examples 1 + 2 (place police OK, place fire blocked by envelope) reproducible as integration tests with no UI involvement.

**Exit:**

- `ZoneSService.cs` under `Assets/Scripts/Managers/GameManagers/`. MonoBehaviour. `[SerializeField]` refs to `BudgetAllocationService`, `TreasuryFloorClampService`, `ZoneSubTypeRegistry`, `ZoneManager`, `GridManager`.
- `PlaceStateServiceZone(GridCoord cell, int subTypeId) → bool` implements Example 1 + 2 flow.
- Cell access goes through `GridManager.GetCell` (invariant #5).
- Growth pipeline hook: on S building spawn, `EconomyManager.RegisterMaintenanceContributor(StateServiceMaintenanceContributor)`.
- `AutoZoningManager` guard: early-return no-op when `IsStateServiceZone(targetType)` is true + comment: `// Zone S is manual-only in MVP — see docs/zone-s-economy-exploration.md §Q2`.
- Glossary row: `ZoneSService`.
- Integration tests: Example 1 (place succeeds, envelope + treasury decrement, building registers as contributor) + Example 2 (envelope exhausted blocks placement, no treasury mutation) green.
- Phase 1 — `ZoneSService` skeleton + manager refs wired.
- Phase 2 — `PlaceStateServiceZone` flow + growth-pipeline contributor registration.
- Phase 3 — `AutoZoningManager` guard + integration tests + glossary.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | `ZoneSService` skeleton class | **TECH-545** | Done (archived) | New `ZoneSService.cs` MonoBehaviour under `Assets/Scripts/Managers/GameManagers/`. `[SerializeField] private BudgetAllocationService budgetAllocation; [SerializeField] private TreasuryFloorClampService treasuryFloorClamp; [SerializeField] private ZoneSubTypeRegistry registry; [SerializeField] private ZoneManager zoneManager; [SerializeField] private GridManager grid;` — all with `FindObjectOfType` fallback in `Awake` (guardrail #1). Public API signatures declared (body empty / returns default). |
| T6.1.1 | Scene wiring + prefab attach | **TECH-546** | Done (archived) | Attach `ZoneSService` component to the `EconomyManager` GameObject (or dedicated GameManagers GO) in main scene prefab. Populate Inspector refs for all 5 serialized fields. Verify `FindObjectOfType` fallbacks still resolve when Inspector refs are cleared (loadable prefab safety). |
| T6.2 | `PlaceStateServiceZone` flow | **TECH-547** | Done (archived) | Implement `PlaceStateServiceZone(GridCoord cell, int subTypeId) → bool`: lookup `entry = registry.GetById(subTypeId)`; fail-fast on null. Fetch cell via `grid.GetCell(cell.x, cell.y)` (invariant #5). Call `budgetAllocation.TryDraw(subTypeId, entry.baseCost)` — on false, emit notification "insufficient {subType} envelope this month" and return false (Q4 block-before-deduct, no treasury mutation). On true, call `zoneManager.PlaceZone(cell, ZoneType.StateServiceLightZoning, subTypeId: subTypeId)` and return true. Sub-type id written to `Zone.subTypeId` sidecar inside `ZoneManager.PlaceZone` (extend signature). |
| T6.3 | `StateServiceMaintenanceContributor` component | **TECH-548** | Done (archived) | New `StateServiceMaintenanceContributor` MonoBehaviour attached to S building prefabs. Implements `IMaintenanceContributor`: `GetMonthlyMaintenance()` reads `registry.GetById(subTypeId).monthlyUpkeep`; `GetContributorId()` returns `$"s-{subTypeId}-{instanceId}"`; `GetSubTypeId()` returns `subTypeId`. `Start()` calls `economyManager.RegisterMaintenanceContributor(this)`; `OnDestroy()` calls `Unregister`. |
| T6.4 | Growth-pipeline hook for S buildings | **TECH-549** | Done (archived) | Inside existing building-spawn pipeline (grep `PlaceZone` → growth callback or `BuildingSpawner` equivalent), attach `StateServiceMaintenanceContributor` component on spawned S buildings. Read `Zone.subTypeId` from the owning cell + pass to the component. Guard: only applies when `IsStateServiceZone(cell.zoneType)`. No change for R/C/I pipelines. |
| T6.5 | `AutoZoningManager` S no-op guard | **TECH-550** | Done (archived) | Grep `AutoZoningManager.cs` for the zone-selection switch. Add early-return: `if (EconomyManager.IsStateServiceZone(candidate)) { /* Zone S is manual-only in MVP — see docs/zone-s-economy-exploration.md §Q2 */ continue; }`. Covers Review Note N4 (path reference in comment). No behavioral change for RCI. |
| T6.6 | Integration tests — Examples 1 + 2 | **TECH-551** | Done (archived) | `ZoneSServicePlacementTests` under `Assets/Tests/EditMode/Economy/` (or PlayMode if grid scene needed). Example 1: envelope=1200, treasury=10000, `PlaceStateServiceZone(cell, POLICE(baseCost=500))` → returns true, envelope=700, treasury=9500, zone at cell is `StateServiceLightZoning` with `subTypeId=POLICE`. Example 2: envelope=200, baseCost=600 → returns false, envelope UNCHANGED (200), treasury UNCHANGED, zone at cell NOT placed. Add `ZoneSService` glossary row + MCP reindex. Run `npm run validate:all`. |

### §Plan Fix — resolved (2026-04-20)

> plan-review found 1 drift: Stage 6 **Exit** first bullet listed `EconomyManager` on `ZoneSService`; T6.1 + TECH-545 specify five refs only. Contributor path uses `EconomyManager` via `StateServiceMaintenanceContributor` (TECH-548/549), not on `ZoneSService`. **Exit** bullet updated in-repo to match. Original tuple had invalid anchor `L498` — repaired to full-line `target_anchor` before apply. No pending `plan-fix-apply` for this item.

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: "TECH-545"
  title: "`ZoneSService` skeleton class"
  priority: "high"
  notes: |
    New `ZoneSService` MonoBehaviour with serialized refs to budget, treasury clamp, registry, zone manager, grid.
    `FindObjectOfType` fallbacks in `Awake`. Public method stubs only — behavior lands in TECH-547.
  depends_on: []
  related: ["TECH-546", "TECH-547", "TECH-548", "TECH-549", "TECH-550", "TECH-551"]
  stub_body:
    summary: |
      Introduce `ZoneSService` shell with guardrail wiring so later tasks can implement `PlaceStateServiceZone` without scene churn.
    goals: |
      - Add `ZoneSService.cs` under `Assets/Scripts/Managers/GameManagers/`.
      - Serialize `BudgetAllocationService`, `TreasuryFloorClampService`, `ZoneSubTypeRegistry`, `ZoneManager`, `GridManager` with `Awake` fallbacks.
      - Declare `PlaceStateServiceZone` (or equivalent) signatures; bodies empty / default returns until TECH-547.
    systems_map: |
      - New `ZoneSService.cs`
      - `BudgetAllocationService`, `TreasuryFloorClampService`, `ZoneSubTypeRegistry`, `ZoneManager`, `GridManager`
    impl_plan_sketch: |
      Phase 1 — New class + serialized fields + `Awake` resolution. Phase 2 — stub public API for placement (no logic).

- reserved_id: "TECH-546"
  title: "Scene wiring + prefab attach"
  priority: "high"
  notes: |
    Attach `ZoneSService` to Economy / GameManagers GO in main scene prefab; fill Inspector refs; verify fallbacks when refs cleared.
  depends_on: []
  related: ["TECH-545", "TECH-547", "TECH-548", "TECH-549", "TECH-550", "TECH-551"]
  stub_body:
    summary: |
      Wire `ZoneSService` into shipped scene so runtime and tests resolve component without manual scene edits.
    goals: |
      - Add component to `EconomyManager` GameObject or dedicated GameManagers root in main scene prefab.
      - Populate all five serialized references in Inspector.
      - Document or verify `FindObjectOfType` path when Inspector fields empty (load safety).
    systems_map: |
      - Main scene / prefab under `Assets/` (exact path per repo convention)
      - `ZoneSService` component
    impl_plan_sketch: |
      Phase 1 — Prefab/scene edit. Phase 2 — smoke: enter play mode or EditMode load if applicable.

- reserved_id: "TECH-547"
  title: "`PlaceStateServiceZone` flow"
  priority: "high"
  notes: |
    Implement placement: registry lookup, `GridManager.GetCell`, `budgetAllocation.TryDraw`, `ZoneManager.PlaceZone` with `subTypeId` param.
    Example 1–2 semantics: block before deduct when draw fails; extend `PlaceZone` signature for sidecar write.
  depends_on: []
  related: ["TECH-545", "TECH-546", "TECH-548", "TECH-549", "TECH-550", "TECH-551"]
  stub_body:
    summary: |
      Implement manual Zone S placement through envelope draw then zone placement; honor invariant #5 (cell access via `GridManager`).
    goals: |
      - `PlaceStateServiceZone(GridCoord cell, int subTypeId)` returns false on bad registry id or failed `TryDraw` with notification.
      - On success call `ZoneManager.PlaceZone` with correct `ZoneType` and `subTypeId`; `Zone.subTypeId` set inside `PlaceZone`.
      - No treasury mutation when envelope draw fails (Q4).
    systems_map: |
      - `ZoneSService.cs`
      - `ZoneManager`, `BudgetAllocationService`, `ZoneSubTypeRegistry`, `GridManager`
      - `Zone.cs` sidecar if signature threading required
    impl_plan_sketch: |
      Phase 1 — Implement draw + place happy path. Phase 2 — failure paths + notifications + `PlaceZone` signature extension.

- reserved_id: "TECH-548"
  title: "`StateServiceMaintenanceContributor` component"
  priority: "medium"
  notes: |
    MonoBehaviour on S buildings implementing `IMaintenanceContributor`; register in `Start`, unregister in `OnDestroy`; id scheme `s-{subTypeId}-{instanceId}`.
  depends_on: []
  related: ["TECH-545", "TECH-546", "TECH-547", "TECH-549", "TECH-550", "TECH-551"]
  stub_body:
    summary: |
      Per-building maintenance contributor for Zone S using registry monthly upkeep and stable contributor id.
    goals: |
      - New `StateServiceMaintenanceContributor` under GameManagers (or appropriate folder).
      - Implement interface methods; `GetSubTypeId()` returns configured sub-type; upkeep from `ZoneSubTypeRegistry`.
      - Register with `EconomyManager` on `Start`; unregister on destroy.
    systems_map: |
      - New contributor class
      - `IMaintenanceContributor`, `EconomyManager`, `ZoneSubTypeRegistry`
    impl_plan_sketch: |
      Phase 1 — Component + interface impl. Phase 2 — wire subTypeId source (serialized or injected).

- reserved_id: "TECH-549"
  title: "Growth-pipeline hook for S buildings"
  priority: "medium"
  notes: |
    In building spawn path after `PlaceZone`, attach contributor when `IsStateServiceZone`; read `Zone.subTypeId` from cell; R/C/I unchanged.
  depends_on: []
  related: ["TECH-545", "TECH-546", "TECH-547", "TECH-548", "TECH-550", "TECH-551"]
  stub_body:
    summary: |
      Ensure spawned S buildings register maintenance contributor automatically from growth/build pipeline.
    goals: |
      - Locate spawn callback chain (grep `PlaceZone` / spawner).
      - Add `StateServiceMaintenanceContributor` only for state-service zone types.
      - Pass through `subTypeId` from zone sidecar.
    systems_map: |
      - Building spawn / growth pipeline scripts (paths from codebase search)
      - `StateServiceMaintenanceContributor`, `EconomyManager.IsStateServiceZone`
    impl_plan_sketch: |
      Phase 1 — Trace spawn hook. Phase 2 — attach component + configure fields.

- reserved_id: "TECH-550"
  title: "`AutoZoningManager` S no-op guard"
  priority: "medium"
  notes: |
    Early-return / continue when candidate type is state-service zone; comment cites exploration §Q2; RCI behavior unchanged.
  depends_on: []
  related: ["TECH-545", "TECH-546", "TECH-547", "TECH-548", "TECH-549", "TECH-551"]
  stub_body:
    summary: |
      Block AUTO zoning from placing Zone S; manual-only MVP per locked design.
    goals: |
      - Find zone-selection loop in `AutoZoningManager`.
      - Skip state-service zone types with documented comment referencing `docs/zone-s-economy-exploration.md` §Q2.
      - No change to R/C/I AUTO paths.
    systems_map: |
      - `AutoZoningManager.cs`
      - `EconomyManager.IsStateServiceZone`
    impl_plan_sketch: |
      Phase 1 — Grep + insert guard. Phase 2 — regression sanity on AUTO RCI.

- reserved_id: "TECH-551"
  title: "Integration tests — Examples 1 + 2"
  priority: "medium"
  notes: |
    `ZoneSServicePlacementTests` EditMode (or PlayMode if scene required). Assert envelope + treasury + placement per Stage Exit; glossary `ZoneSService`; validate:all.
  depends_on: []
  related: ["TECH-545", "TECH-546", "TECH-547", "TECH-548", "TECH-549", "TECH-550"]
  stub_body:
    summary: |
      Lock Examples 1 and 2 from exploration with automated tests and canonical glossary entry for `ZoneSService`.
    goals: |
      - Example 1: successful place decrements envelope and treasury; zone + subTypeId correct.
      - Example 2: insufficient envelope leaves balances and grid unchanged.
      - Add glossary row + index regen per repo policy; `npm run validate:all` green.
    systems_map: |
      - `Assets/Tests/EditMode/Economy/ZoneSServicePlacementTests.cs` (new)
      - `ia/specs/glossary.md`, index regen scripts
      - `ZoneSService` + test doubles / scene setup
    impl_plan_sketch: |
      Phase 1 — Test harness + Example 1. Phase 2 — Example 2 + glossary + validators.
```

---
