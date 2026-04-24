### Stage 3.1 — `PlacementValidator` core API

**Status:** Done — 5 tasks closed (**TECH-688**..**TECH-692**, all archived)

**Objectives:** Deterministic **legality** answers: footprint placeholder (1×1 MVP), zoning channel match, unlock stub, affordability hook via **`EconomyManager`** / treasury services.

**Exit:**

- Public method returns **`PlacementResult`** (allowed + **`PlacementFailReason`** + optional detail string).
- Unit tests table-driven for core cases.

**Tasks:**

| Task | Name | Issue | Status | Intent |
|---|---|---|---|---|
| T3.1.1 | Author PlacementValidator type | **TECH-688** | Done (archived) | New class file; serialized refs to **`GridManager`**, **`GridAssetCatalog`**, **`EconomyManager`** per guardrails. |
| T3.1.2 | Reason codes + result struct | **TECH-689** | Done (archived) | Structured enum covers footprint, zoning, locked, unaffordable, occupied; XML docs on public API. |
| T3.1.3 | Zoning channel match MVP | **TECH-690** | Done (archived) | Zone S manual placement path consults validator before commit; keep **`GridManager`** extraction — no new `GridManager` methods unless unavoidable (justify in §Findings). |
| T3.1.4 | Affordability gate | **TECH-691** | Done (archived) | Query **`baseCost`** cents from catalog economy snapshot; delegate to existing spend/try APIs. |
| T3.1.5 | Unlock gate stub | **TECH-692** | Done (archived) | Read **`unlocks_after`** string; integrate with existing tech stub or return **Allowed** if not implemented — document. |

#### §Stage File Plan

<!-- stage-file-plan output — do not hand-edit; apply via stage-file-apply -->

```yaml
- reserved_id: ""
  title: "Author PlacementValidator type"
  priority: high
  notes: |
    New MonoBehaviour or plain C# service under GameManagers/Services; serialized GridManager,
    GridAssetCatalog, EconomyManager refs per unity-invariants Inspector pattern. No direct grid.cellArray.
    Stage 3.1 Phase 1 — types foundation for CanPlace MVP.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Add PlacementValidator type with serialized manager refs and a stub CanPlace API surface so later
      tasks can attach reason codes, zoning, economy, and unlock gates without reshaping the type.
    goals: |
      - PlacementValidator lives under Assets/Scripts/Managers/GameManagers/ (or Services sibling).
      - SerializeField refs: GridManager, GridAssetCatalog, EconomyManager; Awake resolves via FindObjectOfType fallback where pattern already exists in codebase.
      - Public CanPlace signature reserved (bool + detail) even if body returns placeholder true until T3.1.2 lands.
    systems_map: |
      - New: Assets/Scripts/Managers/GameManagers/PlacementValidator.cs
      - Existing: GridManager, GridAssetCatalog, ZoneSubTypeRegistry / catalog wiring, EconomyManager
      - Ref: docs/grid-asset-visual-registry-exploration.md §8.3 PlacementValidator
    impl_plan_sketch: |
      ### Phase 1 — Type scaffold
      - [ ] Create class file; add SerializeField trio + XML summary on class.
      - [ ] Stub CanPlace(assetId, cell, rotation) returning true with TODO hook for fail reasons.
- reserved_id: ""
  title: "Reason codes + result struct"
  priority: high
  notes: |
    PlacementFailReason enum + structured result (bool, reason, optional message) with XML docs on public API.
    Table-driven EditMode tests per Stage Exit. Consumes PlacementValidator from TECH-688.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Replace placeholder CanPlace return with PlacementResult carrying bool, PlacementFailReason, and optional
      detail string; document enum values for footprint, zoning, locked, unaffordable, occupied.
    goals: |
      - Enum covers footprint, zoning, locked, unaffordable, occupied (+ None/Ok as appropriate).
      - XML documentation on public CanPlace / result types.
      - EditMode tests: table-driven cases for at least one pass and one fail path per reason category (stubbed deps).
    systems_map: |
      - PlacementValidator.cs (expand)
      - Tests under Assets/Tests/ or existing EditMode test assembly pattern
    impl_plan_sketch: |
      ### Phase 1 — Result + tests
      - [ ] Define PlacementFailReason + PlacementResult structs/classes.
      - [ ] Wire CanPlace to return structured failures (stubs OK for deps not yet implemented).
      - [ ] Add EditMode test fixture with [TestCase] matrix.
- reserved_id: ""
  title: "Zoning channel match MVP"
  priority: high
  notes: |
    Zone S manual placement consults validator before commit; use GridManager public API only; avoid new
    GridManager methods unless justified in §Findings. Integrates with TECH-689 result shape.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Hook Zone S (or ZoneManager manual path) so placement attempts call PlacementValidator.CanPlace before
      committing building spawn; surface fail reason for downstream UX (Stage 3.2).
    goals: |
      - Manual placement path blocks illegal zoning channel mismatches using validator output.
      - No direct grid.cellArray access from validator; GridManager extraction only.
      - Document any unavoidable GridManager API addition in §Findings.
    systems_map: |
      - PlacementValidator.cs
      - ZoneManager.cs, CursorManager.cs (integration hooks per master plan surfaces)
      - GridManager public surface
    impl_plan_sketch: |
      ### Phase 1 — Integration
      - [ ] Locate Zone S commit point; insert CanPlace guard; abort commit on false.
      - [ ] Map PlacementFailReason.Zoning (or equivalent) for channel mismatch.
      - [ ] Manual smoke: place allowed vs disallowed asset in Editor.
- reserved_id: ""
  title: "Affordability gate"
  priority: medium
  notes: |
    Read baseCost cents from GridAssetCatalog economy snapshot; delegate to existing EconomyManager try/spend
    APIs. Unaffordable → PlacementFailReason.Unaffordable.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Extend validator so CanPlace rejects when treasury cannot afford catalog baseCost for assetId using
      existing economy APIs (no new economy subsystem).
    goals: |
      - Query catalog snapshot for baseCost; align with economy spec cents.
      - Call existing spend/affordability check pattern used elsewhere for buildings.
      - Unit or EditMode test: affordable vs unaffordable paths.
    systems_map: |
      - PlacementValidator.cs
      - EconomyManager, GridAssetCatalog economy fields
      - ia/specs/economy-system.md (treasury / spend patterns)
    impl_plan_sketch: |
      ### Phase 1 — Economy gate
      - [ ] Resolve baseCost for assetId from catalog.
      - [ ] Integrate EconomyManager affordability probe before allow.
      - [ ] Tests for can/cannot afford.
- reserved_id: ""
  title: "Unlock gate stub"
  priority: medium
  notes: |
    Read unlocks_after from catalog row; integrate tech unlock stub if present else return Allowed and document
    behavior in spec §Open Questions / Decision Log.
  depends_on: []
  related: []
  stub_body:
    summary: |
      Validator reads unlocks_after string; if no tech unlock system wired, document and return allowed; if stub
      exists, map locked assets to PlacementFailReason.Locked.
    goals: |
      - Catalog field unlocks_after consulted in CanPlace path.
      - Documented fallback when tech tree not implemented.
      - Test or explicit manual checklist for locked vs unlocked asset.
    systems_map: |
      - PlacementValidator.cs
      - GridAssetCatalog asset rows / DTO
      - Existing tech unlock stub (if any)
    impl_plan_sketch: |
      ### Phase 1 — Unlock stub
      - [ ] Parse unlocks_after in validator; branch to Locked or Allowed per integration state.
      - [ ] Document integration gap in Decision Log if returning Allowed by default.
      - [ ] Minimal test or §8 manual step.
```

#### §Plan Fix — PASS (no drift)

> plan-review exit 0 — all Task specs aligned. No tuples emitted. Downstream pipeline continue.

#### §Stage Audit

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._
