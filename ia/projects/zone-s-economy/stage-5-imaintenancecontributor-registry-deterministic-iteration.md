### Stage 5 — Economy services: bond ledger + maintenance registry + Zone S placement / `IMaintenanceContributor` registry + deterministic iteration

**Status:** Final

**Objectives:** Refactor `EconomyManager.ProcessMonthlyMaintenance` from hardcoded formula to contributor-registry iteration. Existing roads + power plants preserved via adapter pattern (behavior-preserving). Registry iteration sorted by contributor id string for save-replay parity (N2 resolution). Contributors emit into owning sub-type envelope — underfunded envelope shows visual decay placeholder (Q3).

**Exit:**

- `IMaintenanceContributor` interface: `int GetMonthlyMaintenance()`, `string GetContributorId()`, `int GetSubTypeId()` (-1 = general maintenance pool).
- `EconomyManager.RegisterMaintenanceContributor(IMaintenanceContributor)` + `Unregister`.
- `ProcessMonthlyMaintenance` iterates registry sorted by `GetContributorId()` (deterministic).
- `RoadMaintenanceContributor` adapter preserves existing road maintenance formula bit-for-bit.
- `PowerPlantMaintenanceContributor` adapter confirmed present (utility line in monthly tick) and implemented.
- Glossary row: `IMaintenanceContributor`.
- Phase 1 — `IMaintenanceContributor` interface + `EconomyManager` registry plumbing.
- Phase 2 — Existing-contributor adapters + hardcoded-formula retirement + tests + glossary.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | `IMaintenanceContributor` interface + registry | **TECH-540** | Done (archived) | New `IMaintenanceContributor.cs` with `GetMonthlyMaintenance() → int`, `GetContributorId() → string`, `GetSubTypeId() → int`. Add `List<IMaintenanceContributor> maintenanceContributors` to `EconomyManager`. `RegisterMaintenanceContributor(c)` + `UnregisterMaintenanceContributor(c)` public methods. Registry reset on scene load / save restore. |
| T5.2 | Refactor `ProcessMonthlyMaintenance` to iterate registry | **TECH-541** | Done (archived) | In `EconomyManager.ProcessMonthlyMaintenance`: sort registry snapshot by `GetContributorId()` (stable, deterministic). For each contributor: if `GetSubTypeId() >= 0`, draw from `budgetAllocation.TryDraw(subTypeId, amount)` — failure flags decay on contributor (no penalty MVP, hook only). Else spend from general treasury via `treasuryFloorClamp.TrySpend`. Old hardcoded `roadCount × rate + powerPlantCount × rate` formula removed. |
| T5.3 | `RoadMaintenanceContributor` adapter | **TECH-542** | Done (archived) | New adapter class `RoadMaintenanceContributor` implementing `IMaintenanceContributor`. `GetMonthlyMaintenance()` returns the prior `roadCount × ratePerRoad` result (read from existing `RoadManager` or equivalent). `GetContributorId()` returns `"road-aggregate"`. `GetSubTypeId()` returns -1 (general pool). Register from `EconomyManager.Start` after dependencies wired. Result: month-to-month maintenance delta identical to pre-refactor. |
| T5.4 | Power-plant contributor audit + adapter | **TECH-543** | Done (archived) | Audit confirmed `ComputeMonthlyUtilityMaintenanceCost` present. `PowerPlantMaintenanceContributor` adapter added, contributor id `power-aggregate`, general pool. Decision Log updated. |
| T5.5 | EditMode tests + glossary | **TECH-544** | Done (archived) | `MaintenanceRegistryTests` under `Assets/Tests/EditMode/Economy/`. 7 test cases covering sorted iteration, register/unregister, clear, duplicates, projection sum, road parity, power plant parity. `IMaintenanceContributor` glossary row added. |

### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: "TECH-540"
  title: "`IMaintenanceContributor` interface + registry"
  priority: "high"
  notes: |
    New interface + contributor list on `EconomyManager` with register/unregister.
    Reset registry on load after Stage 4 bond maintenance context (`TECH-533`).
    Touches `EconomyManager`, new `IMaintenanceContributor.cs`.
  depends_on:
    - "TECH-533"
  related: []
  stub_body:
    summary: |
      Introduce `IMaintenanceContributor` and wire a deterministic registration surface on `EconomyManager`
      so monthly maintenance can move off a hardcoded formula in later tasks.
    goals: |
      - Add `IMaintenanceContributor` with `GetMonthlyMaintenance`, `GetContributorId`, `GetSubTypeId` (-1 = general pool).
      - Add `RegisterMaintenanceContributor` / `UnregisterMaintenanceContributor` on `EconomyManager` plus internal list storage.
      - Clear or rebuild contributor list on scene load and save restore (parity with prior maintenance baseline).
    systems_map: |
      - `Assets/Scripts/Managers/GameManagers/EconomyManager.cs`
      - New `Assets/Scripts/Managers/GameManagers/IMaintenanceContributor.cs`
      - `GameSaveManager` / load ordering if registry must snapshot with save (coordinate with Stage 4 save tasks).
    impl_plan_sketch: |
      Phase 1 — Add interface + fields + public register API. Phase 2 — hook reset on load paths used by bond/economy restore.

- reserved_id: "TECH-541"
  title: "Refactor `ProcessMonthlyMaintenance` to iterate registry"
  priority: "high"
  notes: |
    Replace road/power line item with sorted registry iteration. Sub-type path uses `budgetAllocation.TryDraw`; general pool uses treasury clamp.
    Depends on registry API from T5.1 (`TECH-533` chain prerequisite).
  depends_on:
    - "TECH-533"
  related: []
  stub_body:
    summary: |
      Drive `ProcessMonthlyMaintenance` from a snapshot of contributors sorted by `GetContributorId()` for save/replay-stable ordering.
    goals: |
      - Snapshot and sort contributors by id string before spending.
      - Route `GetSubTypeId() >= 0` draws through `budgetAllocation.TryDraw` with decay hook on failure; else `treasuryFloorClamp.TrySpend`.
      - Remove legacy `roadCount × rate + powerPlantCount × rate` block once adapters land in T5.3–T5.4.
    systems_map: |
      - `EconomyManager.ProcessMonthlyMaintenance`
      - `BudgetAllocationService`, `TreasuryFloorClampService`
    impl_plan_sketch: |
      Phase 1 — Iterate sorted list. Phase 2 — delete hardcoded formula after contributor adapters match legacy totals.

- reserved_id: "TECH-542"
  title: "`RoadMaintenanceContributor` adapter"
  priority: "high"
  notes: |
    Preserve pre-refactor road maintenance math via adapter implementing `IMaintenanceContributor`.
    Register from `EconomyManager` after dependencies wired (`RoadManager` or equivalent).
  depends_on:
    - "TECH-533"
  related: []
  stub_body:
    summary: |
      Encapsulate existing road maintenance calculation in `RoadMaintenanceContributor` so refactor stays behavior-identical month-to-month.
    goals: |
      - Implement `GetMonthlyMaintenance` using prior `roadCount × ratePerRoad` inputs.
      - Use `GetContributorId() == "road-aggregate"` and `GetSubTypeId() == -1`.
      - Register in `EconomyManager.Awake` after refs resolve.
    systems_map: |
      - `RoadManager` (or existing road count source)
      - `EconomyManager`
      - New `RoadMaintenanceContributor.cs`
    impl_plan_sketch: |
      Phase 1 — Adapter class + unit comparison against legacy formula. Phase 2 — register + EditMode guard in T5.5.

- reserved_id: "TECH-543"
  title: "Power-plant contributor audit + adapter"
  priority: "medium"
  notes: |
    Confirm whether power plants participate in monthly maintenance today. If yes, add `PowerPlantMaintenanceContributor`; if deferred, document Decision Log + skip implementation.
  depends_on:
    - "TECH-533"
  related: []
  stub_body:
    summary: |
      Audit `ProcessMonthlyMaintenance` for power-plant lines and either add a matching adapter or explicitly defer Bucket 4 with logged decision.
    goals: |
      - Grep/read current monthly tick for power plant charges.
      - If present: adapter analogous to roads with stable contributor id. If absent: Decision Log entry + no code churn.
    systems_map: |
      - `EconomyManager.ProcessMonthlyMaintenance`
      - Power / utility managers as referenced in codebase audit
    impl_plan_sketch: |
      Phase 1 — Audit + decision record. Phase 2 — implement adapter only if audit finds existing line item.

- reserved_id: "TECH-544"
  title: "EditMode tests + glossary"
  priority: "medium"
  notes: |
    `MaintenanceRegistryTests` covering sort order, envelope vs treasury paths, road parity regression, plus glossary row for `IMaintenanceContributor`.
  depends_on:
    - "TECH-533"
  related: []
  stub_body:
    summary: |
      Lock contributor registry behavior with tests and add canonical glossary entry for `IMaintenanceContributor`.
    goals: |
      - Deterministic iteration order test with three contributors registered out of order.
      - Sub-type vs general-pool spend paths exercised per Stage exit bullets.
      - Road cost before/after adapter parity assertion.
      - Glossary row + index regen where required by repo policy.
    systems_map: |
      - `Assets/Tests/EditMode/Economy/MaintenanceRegistryTests.cs` (new)
      - `ia/specs/glossary.md`, MCP glossary indexes if touched
    impl_plan_sketch: |
      Phase 1 — Author tests with fakes/mocks for contributors. Phase 2 — glossary + `npm run validate:all` coordination.
```
