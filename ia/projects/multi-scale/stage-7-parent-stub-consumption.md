### Stage 7 — City MVP close / Parent-stub consumption

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** ≥1 city UI panel + ≥1 sim system actively consume Step 1 stubs (ParentRegionId / ParentCountryId / GetNeighborStub). Establishes consumer pattern for Step 3 to flesh out.

**Exit:**

- `ParentContextPanel.cs` (new) in city HUD: reads `GridManager.ParentRegionId` + `ParentCountryId`; displays region + country placeholder names.
- `NeighborCityStubPanel.cs` (new) in city HUD sidebar: reads `GridManager.GetNeighborStub(side)` for all border sides; renders ≥1 stub card (display name + border direction); inert.
- `DemandManager.GetExternalDemandModifier()` (new method): reads `GetNeighborStub()` list; returns `1.0f + 0.05f * stubCount` as placeholder; called in demand calculation; `GridManager` cached in `Awake` (invariant #3). Establishes consumption pattern for Step 3.
- Testmode smoke: after New Game, `ParentContextPanel` shows non-null values; `GetNeighborStub()` returns ≥1 stub; `GetExternalDemandModifier()` returns > 1.0f.
- Phase 1 — Parent context + neighbor stub UI panels.
- Phase 2 — Sim consumer + integration smoke.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | Parent context panel | _pending_ | _pending_ | `ParentContextPanel.cs` (new) MonoBehaviour in city HUD: reads `GridManager.ParentRegionId` + `ParentCountryId`; displays region + country placeholder name; binds on scene load. Follows `ia/specs/ui-design-system.md` §HUD patterns. |
| T7.2 | Neighbor stub panel | _pending_ | _pending_ | `NeighborCityStubPanel.cs` (new): iterates border sides via `GridManager.GetNeighborStub(side)`; renders ≥1 HUD stub card (display name, border direction enum); inert — no behavior, no data mutation. |
| T7.3 | DemandManager parent modifier | _pending_ | _pending_ | `DemandManager.GetExternalDemandModifier()` (new): reads neighbor stub list; returns `1.0f + 0.05f * stubCount`; wired into demand calculation. Cache `GridManager` in `Awake` (invariant #3). Pattern seeded for Step 3 expansion. |
| T7.4 | Parent-stub integration smoke | _pending_ | _pending_ | Testmode smoke scenario: New Game → assert `ParentContextPanel` non-null display; assert `GetNeighborStub()` count ≥ 1; assert `GetExternalDemandModifier()` > 1.0f. Confirms Step 1 stubs consumed end-to-end. |
