# FEAT-44 — High bridges (elevated road spans)

> **Issue:** [FEAT-44](../../BACKLOG.md)  
> **Status:** Implementation complete — pending user QA  
> **Created:** 2026-03-30  
> **Last updated:** 2026-03-30

## 1. Summary

This project extends the existing road placement pipeline so that valid path segments are treated as **elevated bridges**: the deck uses a **uniform height** derived from the **start** endpoint, spans **water**, **shores**, or **cliff-adjacent** gaps while respecting minimum **vertical clearance** over underlying terrain, and only **commits** when the **end** cell matches that bridge height. **Manual** and **AUTO** road growth must both support bridge segments. The work also fixes incorrect side effects during **manual road preview and commit** (clearing zoning, buildings, or water on cells that should not be modified).

Canonical geography rules remain in `.cursor/specs/isometric-geography-system.md`; this document is the **temporary project spec** for FEAT-44 and should be migrated or folded into the canonical spec when the issue closes.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Any road trace that satisfies bridge validity rules must use **bridge semantics** for the affected span (elevated deck, existing bridge prefab behavior where applicable).
2. **Bridge mode is implicit**: there is no separate "bridge tool"; eligibility is determined by rules on the active stroke.
3. **Geographic triggers** for a bridge span (see §5):
   - **Water:** the path crosses a water body such that **entry and exit** land cells (and required free conditions) are satisfied as defined in existing bridge rules; **shores** are in scope as part of water-related geography.
   - **Cliff:** if the **initial** endpoint of the bridge segment lies on a side where a **cliff** applies, the player (or AUTO) may **continue** the same road stroke with bridge preview/placement over the gap; no separate "dry canyon" type is specified beyond these triggers and height validation.
4. **Uniform deck height:** bridge prefabs along the span are instantiated at a single **bridge height** equal to the **start** endpoint's `cell height`, independent of per-cell terrain height **under** the span.
5. **End validation:** the stroke is valid only if the **end** cell of the bridge segment has `cell height == bridge height` (same as start). Otherwise the bridge is **invalid** (no commit, see §5.1).
6. **Vertical clearance:** minimum **one** height unit between deck and terrain (or equivalent vertical model) below, per existing heightmap rules.
7. **Grid geometry:** respect existing bridge constraints: **straight** segments on the grid, **no maximum length** (beyond what the path system already allows).
8. **Endpoints:** segments connect to the city road network. Endpoints are **stable land** (no special min/max height beyond the equality rule). Approach may be **flat** or **orthogonal slope** (N/S/E/W); **diagonal** slope at endpoints is not allowed.
9. **Pipeline:** reuse the **same road pipeline** as today (`TryPrepareRoadPlacementPlan` and related flow) with an **additional bridge analysis phase**.
10. **Pathfinding and costs:** identical to **normal road** once placed.
11. **Persistence:** bridges persist through save/load **without new bridge-specific flags**; they are represented like other road data until a future iteration adds explicit metadata.
12. **Minimap:** bridge segments appear **the same** as current roads on the minimap.
13. **Manual road side effects:** ensure trace/preview/commit does not corrupt **prefabs or data** on cells **touched or incorrectly treated as adjacent** by the stroke (fix in scope with this feature; aligns with backlog issues around manual road drawing side effects).
14. **Documentation:** update canonical geography notes (and rules as needed) so bridge behavior stays consistent with the isometric system.

### 2.2 Non-Goals (Out of Scope)

1. **Terraforming** to raise or lower one bank to match bridge height (planned for a later version).
2. Other bridge types (movable bridges, multi-deck, non-grid geometry, etc.).
3. New **save-file fields** or flags dedicated to bridges in this iteration.
4. **Preview color** changes for invalid bridge strokes; invalid state uses **hidden preview** and messaging instead (§5.1).
5. Formal **test map** authoring in this document (handled separately).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | I want manual road drawing to show a **bridge preview** over water/shore/cliff-eligible spans as I move the pointer, as part of the same stroke, without a separate build step. | Preview shows bridge segment when rules pass; updating the stroke updates preview consistently with the road pipeline. |
| 2 | Player | I want **invalid** bridge strokes to be obvious: an explicit message in **manual** mode, and **no** misleading preview (full stroke hidden). | On invalid bridge, preview for the current stroke is hidden and a clear manual-only message explains bridge invalidity. |
| 3 | Player | I want **AUTO** simulation to build bridges when rules allow so road growth does not stall over water/cliff gaps. | AUTO commits valid bridge spans; invalid configurations are skipped or rejected without breaking the network builder. |
| 4 | Player | I want bridges to **persist** after save/load and behave like roads. | Save/load round-trip; no bridge-specific flags required for this criterion. |
| 5 | Player | I want bridges on the **minimap** like ordinary roads. | Minimap rendering matches current road treatment. |
| 6 | Developer | I want this to **extend** the existing road system without regressing geography, terrain prefabs, or sorting. | No new geography invariant violations; regression checks on cliff/water/road interaction as exercised by test map and manual QA. |
| 7 | Developer | I want rules aligned with the **isometric geography spec** and updated canonically when the issue closes. | `isometric-geography-system.md` (and related rules) updated; this project spec retired per AGENTS.md policy. |
| 8 | Developer | I want code structured for **future extension** (clear phases, readable comments, agent-friendly). | Bridge logic lives in cohesive helpers/services; key decisions documented in §6. |

## 4. Current State

### 4.1 Relevant Files and Systems

| File / Class | Role in this context |
|--------------|------------------------|
| `RoadManager` | Manual road drawing, preview vs commit, high-bridge UX (`TryPrepareRoadPlacementPlanLongestValidPrefix`). |
| `RoadBridgeAnalyzer` | High-bridge rules on cardinal-expanded paths; outcomes None / Valid / Invalid. |
| `PathTerraformPlan` | `highBridgeDeckHeightByGrid` deck metadata; Revert Phase-3 wave parity with Apply (BUG-37). |
| `RoadPrefabResolver` | Deck height from plan; infer deck on water for single-cell refresh via walk toward prev. |
| `AutoRoadBuilder` | `HowFarWeCanBuild` trimmed with `StraightSegmentPassesRoadPipeline` vs shared pipeline. |
| Road placement pipeline | `TryPrepareFromFilteredPathList` runs analyzer after Phase-1 height validation. |
| `GridManager` | Road mode input; must not absorb new responsibilities—extract helpers if needed (project invariant). |
| Pathfinding / road cache | Same traversal as roads; `InvalidateRoadCache()` after road changes. |
| Save/load (roads) | Persistence without new bridge flags for this iteration. |
| Minimap | Road rendering; bridges unchanged visually. |
| `.cursor/specs/isometric-geography-system.md` | Rivers, cliffs, roads, bridge-related notes—extend while FEAT-44 is open. |

*(Exact file list to be refined during implementation.)*

### 4.2 Current Behavior

- Roads use the existing placement and preview pipeline.
- **Known issue:** manual road tracing (preview and/or commit) can **clear or damage** zoning prefabs, buildings, or water on cells **along or near** the stroke when the mouse moves with the button held. This must be **corrected** as part of FEAT-44 so only legitimately affected cells change.
- Bridge-related rules may exist partially in code; this spec unifies **triggers**, **height**, **clearance**, **end validation**, and **UX** for manual and AUTO.

## 5. Proposed Design

### 5.1 Target Behavior

1. **Eligibility:** During an active road stroke, the system evaluates whether a candidate span qualifies as a bridge using:
   - **Water path:** crossing a water body with valid land entry/exit (and free-cell conditions per existing rules), including **shore** cells as part of that geography; **or**
   - **Cliff continuation:** the **start** of the bridge segment is cliff-appropriate such that continuing the same stroke may treat the following straight grid span as elevated bridge.
2. **Bridge height:** `bridge_height = cell height` of the **start** cell of the bridge segment (the land endpoint where the span begins in grid terms, consistent with existing "connected to network" assumptions).
3. **Deck:** All bridge cells along the span use **uniform** deck height `bridge_height` for prefab placement, regardless of underlying terrain cell heights.
4. **Clearance:** Under the deck, terrain (and water surface where applicable) must satisfy **at least one** height unit of vertical separation from the deck (per project height model).
5. **End cell:** The **last** cell of the bridge segment must have `cell height == bridge_height`. If not, the bridge is **invalid**.
6. **Invalid stroke (manual):** If the current stroke is bridge-invalid, **hide the entire path preview** for that stroke (do not show a defective route). Show an **explicit** user-facing message that the bridge is invalid. Do **not** rely on preview color alone.
7. **AUTO:** Apply the same validity rules; avoid blocking overall road growth—invalid bridge proposals should fail locally without breaking the simulation step.
8. **Commit:** Only valid strokes commit; bridge spans commit through the normal road commit path after the extra validation phase.

### 5.2 Architecture Changes

- Add or extend a **bridge analysis** step in the road preparation pipeline (helper or service), called from the existing plan/prepare flow—not a duplicate of `ComputePathPlan` without `TryPrepareRoadPlacementPlan`.
- Refactor **manual preview/commit** so temporary or dirty updates do not run broad clear logic on **non-road** cells (zoning, buildings, water). Narrow the affected set to what placement actually requires; align preview and commit.
- Keep **GridManager** thin: new logic in dedicated types where possible.

*(Optional diagram: stroke → path → `TryPrepareRoadPlacementPlan` → bridge phase → preview / commit.)*

### 5.3 Method / Algorithm Specification

1. **Input:** Proposed polyline or cell sequence from existing road path computation.
2. **Segmentation:** Identify maximal straight grid runs that may be bridge candidates (per existing straight-bridge rule).
3. **For each candidate span:**
   - Verify trigger: water/shore entry-exit **or** cliff rule at start continuation.
   - Compute `bridge_height` from start land endpoint.
   - Verify **clearance** along all cells under the deck.
   - Verify **end** cell `cell height == bridge_height`.
4. **Output:** Annotate plan with bridge deck height and span bounds for prefab instantiation, or mark stroke invalid for manual messaging / AUTO skip.
5. **After commit:** Ensure road cache invalidation and any existing post-road hooks run as today.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-03-30 | Implicit bridges only; no separate tool | Reduces UX complexity; matches "valid trace implies bridge." | Dedicated bridge mode |
| 2026-03-30 | `bridge_height` from start; end must match | Clear single source of truth; avoids partial spans at wrong elevation. | Independent end height; terraform in v1 |
| 2026-03-30 | Invalid manual stroke → hide **full** preview | Prevents misleading partial routes. | Hide only bridge cells |
| 2026-03-30 | No new save flags in v1 | Smallest persistence change; bridges = roads in data. | Serialized bridge metadata |
| 2026-03-30 | Minimap unchanged | Consistent with "logical only" visual reuse. | Distinct minimap style |
| 2026-03-30 | Manual road clearing bug fixed **with** FEAT-44 | Same code paths; avoids shipping bridges on broken preview/commit. | Separate issue only |

## 7. Implementation Plan

### Phase 1 — Bridge validity and planning

- [x] Implement bridge analysis phase on top of existing road preparation pipeline.
- [x] Encode triggers (water/shore entry-exit, cliff continuation), clearance, and end-height equality.
- [ ] Unit or integration tests for representative grid layouts (paired with test map from separate workstream).

### Phase 2 — Manual preview and commit integrity

- [x] Fix preview/commit neighbor refresh parity (`PathTerraformPlan.Revert` wave count matches `Apply` for cut-through).
- [x] Align invalid-bridge UX: full preview hidden, explicit manual message (throttled during drag).

### Phase 3 — AUTO and persistence

- [x] Ensure AUTO road builder uses the same bridge validation (`StraightSegmentPassesRoadPipeline` after length heuristic).
- [x] Verify save/load without new flags; minimap unchanged (no code changes).

### Phase 4 — Spec and cleanup

- [x] Update `.cursor/specs/isometric-geography-system.md` (and rules if needed).
- [ ] Mark BACKLOG item verified after user QA; migrate lessons learned; remove this project spec per AGENTS.md.

## 8. Acceptance Criteria

- [ ] Manual road stroke shows bridge preview when valid; full stroke hidden with message when bridge rules fail.
- [ ] Committed roads include valid bridge spans over eligible water/shore/cliff situations with uniform deck height and ≥1 height unit clearance.
- [ ] End cell of a bridge segment has the same `cell height` as `bridge_height` from the start; otherwise no commit (manual) / no accept (AUTO).
- [ ] AUTO continues to grow the road network including bridges where valid.
- [ ] Save/load preserves bridges; minimap shows them like normal roads.
- [ ] Manual tracing no longer incorrectly wipes zoning, buildings, or water on non-target cells (preview and commit).
- [ ] Canonical geography documentation updated; road cache invalidation respected after road changes.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | Adjacent terrain wrong after cut-through road preview drag | `PathTerraformPlan.Revert` refreshed only one neighbor wave; `Apply` used two when `isCutThrough` | `Revert` now calls `GetPhase3NeighborWaveCount()` matching `Apply`. |
| 2 | Slope roads showed “bridge invalid” over dry terrain | Cliff-led branch returned `Invalid` when bank heights differed; shallow gaps matched cliff + 1-step clearance | Unequal banks → `return false` (not bridge); dry spans require `bridge_height − min(interior) ≥ 2`. |

## 10. Lessons Learned

- Run high-bridge analysis on the **cardinal-expanded** path so deck cells align with `ComputePathPlan` / resolver indices.
- When `RoadBridgeAnalyzer` returns **Valid**, require the **full** filtered stroke in `TryPrepareRoadPlacementPlanLongestValidPrefix` so bridge failures are never masked by a shorter prefix.
- **Dry bridge** intent must not overlap **slope roads:** mismatched bank heights or a shallow interior (max drop &lt; 2 under deck) → `Outcome.None`, not `Invalid`.
- On user QA pass: migrate any extra lessons to canonical docs and delete this file per `AGENTS.md`.
