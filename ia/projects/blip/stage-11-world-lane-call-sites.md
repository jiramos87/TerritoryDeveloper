### Stage 11 — Patches + integration + golden fixtures + promotion / World lane call sites


**Status:** Done — all 4 tasks archived (TECH-219 + TECH-220 archived 2026-04-15, TECH-221 + TECH-222 archived 2026-04-16)

**Objectives:** `BlipEngine.Play` wired at road per-tile tick + stroke complete + building place/denied + cell select. Five remaining `BlipId` values active: `ToolRoadTick`, `ToolRoadComplete`, `ToolBuildingPlace`, `ToolBuildingDenied`, `WorldCellSelected`.

**Exit:**

- `RoadManager.cs` — `BlipEngine.Play(BlipId.ToolRoadTick)` at per-tile road commit inside `HandleRoadDrawing` (line 141) or `PlaceRoadTileFromResolved` (line 2706). `BlipCooldownRegistry` at 30 ms gates rapid ticks — no additional guard. `BlipEngine.Play(BlipId.ToolRoadComplete)` at road-stroke-complete/apply site (grep `CommitStroke`/`ApplyRoadPlan`/`ConfirmStroke` or `PathTerraformPlan.Apply` call site in `HandleRoadDrawing`). `BlipEngine` self-caches — safe to call per tile (invariant #3).
- `BuildingPlacementService.cs` — `BlipEngine.Play(BlipId.ToolBuildingPlace)` at end of success path in `PlaceBuilding` (line 234). `BlipEngine.Play(BlipId.ToolBuildingDenied)` at failure-notification site where `TryValidateBuildingPlacement` returns non-null reason (in `HandleBuildingPlacement`, `GridManager.cs` line 874 or equivalent caller).
- `GridManager.cs` — `BlipEngine.Play(BlipId.WorldCellSelected)` immediately after each `selectedPoint = mouseGridPosition` assignment (lines 391, 399). One-liner side-effect — not new GridManager logic (invariant #6 carve-out). 80 ms cooldown enforced by `BlipCooldownRegistry`.
- `npm run unity:compile-check` green.
- Phase 1 — Road lane: per-tile tick + stroke complete in `RoadManager.cs`.
- Phase 2 — Building + grid: place/denied in `BuildingPlacementService.cs` + cell-select in `GridManager.cs`.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T11.1 | Road per-tile tick | **TECH-219** | Done (archived) | `RoadManager.cs` — locate per-tile commit site: grep callers of `PlaceRoadTileFromResolved` (line 2706) inside `HandleRoadDrawing` (line 141). Add `BlipEngine.Play(BlipId.ToolRoadTick)` at the point each confirmed road tile is committed to the grid. Cooldown 30 ms enforced by `BlipCooldownRegistry` via patch SO — no additional rate-limit guard. |
| T11.2 | Road stroke complete | **TECH-220** | Done (archived) | `RoadManager.cs` — locate stroke-complete hook: grep `CommitStroke`, `ApplyRoadPlan`, `ConfirmStroke`, or `PathTerraformPlan.Apply` call in `HandleRoadDrawing` (line 141 area) or `GridManager.HandleBulldozerMode` vicinity. Add `BlipEngine.Play(BlipId.ToolRoadComplete)` at end of success path (after all tiles placed, before `InvalidateRoadCache()`). `npm run unity:compile-check` green after road edits. |
| T11.3 | Building place/denied call sites | **TECH-221** | Done (archived) | `BuildingPlacementService.cs` — add `using Territory.Audio;` import, `BlipEngine.Play(BlipId.ToolBuildingPlace)` in `PlaceBuilding` success branch (after `PostBuildingConstructed`, line ~251), `BlipEngine.Play(BlipId.ToolBuildingDenied)` in `else` branch (after `PostBuildingPlacementError`, line ~258). Kickoff audit 2026-04-16 relocated denied call from GridManager caller — `HandleBuildingPlacement` line 874 is a 4-line delegate with no fail-reason branch. Insufficient-funds early-return stays silent. Scope: 1 file, 3 line-additions. |
| T11.4 | GridManager cell-select | **TECH-222** | Done | `GridManager.cs` — add `using Territory.Audio;` import + `BlipEngine.Play(BlipId.WorldCellSelected)` after line 391 (`selectedPoint = mouseGridPosition`, left-click-down) + line 399 (`selectedPoint = pendingRightClickGridPosition`, right-click-up non-pan). Kickoff 2026-04-16 confirmed file lacks `Territory.Audio` import — sibling TECH-221 lesson propagated. Invariant #6 carve-out: one-liner side-effect, not new GridManager logic. Invariant #3: `BlipEngine` self-caches — no per-frame lookup added. 80 ms cooldown in patch SO. `npm run unity:compile-check` green. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_retroactive-skip — Stage archived prior to 2026-04-24 lifecycle refactor that introduced the canonical `§Stage Audit` subsection (see `docs/MASTER-PLAN-STRUCTURE.md` §3.4 + Changelog entry 2026-04-24). Task-level §Audit prose captured in per-Task specs during Stage-scoped closeout before spec deletion; no retroactive re-run needed._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._
