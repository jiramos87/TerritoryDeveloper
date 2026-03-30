# FEAT-44 — Elevated bridges over water (reduced scope)

> **Issue:** [FEAT-44](../../BACKLOG.md)  
> **Status:** Spec revised (reduced scope) — implementation must be re-aligned to this document  
> **Created:** 2026-03-30  
> **Last updated:** 2026-03-30 (water-only scope; no dry bridges; no bridge intersections)

## 1. Summary

FEAT-44 covers **elevated road bridges that cross registered water only** (open water plus **shore** land cells that belong to the same water-related geography). The road deck uses a **uniform height** (`bridge_height`) along the span, **without terraforming terrain or water** to create the crossing—the underlying heightmap and water map stay as they are except where the **existing** road placement pipeline already allows.

**Out of scope for this issue:** bridges over **dry** gaps (canyons, cliff-to-cliff without water), **intersections** between bridge spans (no crossing or joining two bridge runs as bridges), and any geometry that is not a **single straight, axis-aligned** span over water/shore.

**Normal vs elevated (unified model):** “Bridge” here includes the **existing low bridge** behavior: when the deck height matches the **shore / bank band** (land height aligned with the water body’s geography at the crossing), the implementation reuses today’s **normal bridge** semantics. **Elevated** water bridges are the case where `bridge_height` is **strictly above** the logical **water surface S** and **strictly above** the **bed / terrain height under open water** along the span, so the deck is unambiguously above the wet cells. Validation must reject spans where the deck would intersect or sit below those references.

Canonical geography rules remain in `.cursor/specs/isometric-geography-system.md`; this file is the **temporary project spec** for FEAT-44 and should be migrated or folded into the canonical spec when the issue closes.

**Prior implementation note:** An attempt that added “high bridge” logic **and** dry-cliff spans, without fixing **pre-pipeline** eligibility for shore cells, caused hard-to-debug failures. This revision **removes dry-bridge complexity** and makes **shore-inclusive tracing** a first-class requirement for water bridges (see §5.3).

## 2. Goals and Non-Goals

### 2.1 Goals

1. **Water-only trigger:** A bridge span is only recognized when the straight crossing passes through **open water and/or shore** cells (shore treated as part of **water-related** geography for pathing and deck placement), with valid **land endpoints** on both sides per existing connectivity rules.
2. **Straight span only:** At most **one** contiguous water/shore run per stroke segment; the span must be **axis-aligned** (horizontal or vertical on the grid). **No bridge–bridge intersections** (see §2.2).
3. **Uniform deck, no terraform for the span:** `bridge_height` is constant for all deck cells in the span. The feature **does not** raise or lower banks, water surface, or bed to “make” a bridge—only places road/deck at height consistent with the plan.
4. **Height rule:** Along every cell under the deck, `bridge_height` must be **greater than** the **bed / solid terrain height** beneath open water, and **greater than** the logical **water surface S** (or equivalent project definition) for that body so the crossing is **elevated** relative to the water column. When `bridge_height` equals the **shore land height** at the crossing (today’s low-bridge case), treat as the **existing normal bridge** sub-case—still water-only, still straight, no extra terraform requirement beyond current pipeline.
5. **Endpoints:** Land cells at both ends of the span must have `cell height == bridge_height` (same as legacy bridge end rule). Approach rules (perpendicular approach, no turns on water/shore) remain as in existing bridge validation unless this spec explicitly relaxes them later.
6. **Implicit tool:** No separate “bridge mode”; eligibility follows from the stroke and these rules.
7. **Manual and AUTO** use the **same** preparation pipeline (`TryPrepareRoadPlacementPlan` family); **pathfinding walkability** must agree with water-bridge eligibility for shore/open-water steps (§5.3).
8. **Pathfinding and costs:** Same as normal roads once placed; **`InvalidateRoadCache()`** after changes.
9. **Persistence:** No new bridge-specific save fields in this iteration; bridges are ordinary road data.
10. **Minimap:** Same as ordinary roads.
11. **Manual preview/commit:** Do not corrupt zoning, buildings, or water on cells outside what placement legitimately affects (existing backlog alignment).
12. **Documentation:** Update canonical geography notes when the issue closes.

### 2.2 Non-Goals (Out of Scope)

1. **Dry bridges:** No elevated spans over **terrain-only** gaps (cliff gaps, canyons, valleys without water/shore under the deck).
2. **Bridge intersections:** No support for two bridge spans **crossing** each other, T-junctions **on** bridge decks, or merging two bridge runs as bridges. If a stroke would create such a configuration, reject or fall back to non-bridge rules as defined in implementation (default: **invalid**).
3. **Terraforming** banks or water to match a desired deck height.
4. **Non-straight** bridge geometry (diagonal bridge axis, bends on water/shore beyond what existing rules already forbid).
5. New **save-file fields** dedicated to bridges in this iteration.
6. Distinct **minimap** styling for bridges.
7. Formal **test map** authoring in this document (handled separately).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | I draw a **straight** road that crosses **water and shore**; when heights allow, I see a **bridge preview** and can commit an **elevated** deck above S and bed. | Preview and commit go through shared pipeline; invalid strokes show clear feedback. |
| 2 | Player | Low crossings at **shore height** still work like **today’s bridges** (subset of the same rules). | Existing normal-bridge behavior preserved for water-only spans. |
| 3 | Player | Invalid elevated strokes do not mislead me (e.g. hidden full preview + message in manual where applicable). | Consistent with project UX patterns. |
| 4 | Player | **AUTO** can place valid **water** bridges where rules allow. | Invalid proposals skip locally without breaking the builder. |
| 5 | Player | Save/load and minimap behave like **normal roads**. | No new persistence fields required. |
| 6 | Developer | Shores are **traceable** on bridge-intent paths; `CanPlaceRoad` / pathfinder do not veto shore before the pipeline. | §5.3 satisfied. |
| 7 | Developer | No dry-bridge or bridge-intersection features to maintain in this ticket. | Code and tests scoped to §2. |
| 8 | Developer | Rules stay aligned with **isometric geography spec**; this project spec retired per AGENTS.md after QA. | Canonical doc updated. |

## 4. Current State

### 4.1 Relevant Files and Systems

| File / Class | Role in this context |
|--------------|------------------------|
| `RoadManager` | Manual draw, preview/commit, `TryPrepareRoadPlacementPlan` / longest-prefix flow, bridge path helpers (`IsWaterOrWaterSlope`, straightening, validation). |
| `TerraformingService` / `PathTerraformPlan` | Path plan and Phase-1 checks; bridge spans must **skip** terrain modification on water/shore under deck (existing skip paths); no **new** terraform to sculpt water crossings. |
| `RoadPrefabResolver` | Deck height / prefab choice for water and shore cells. |
| `AutoRoadBuilder` | Uses shared road preparation when present. |
| `TerrainManager` | `CanPlaceRoad`, `IsWaterSlopeCell`, heightmap / shore eligibility. |
| `GridPathfinder` | `CanPlaceRoad` in walkability—must align with water-bridge + shore tracing (§5.3). |
| `GridManager` | Input dispatch; keep thin per project invariants. |
| `.cursor/specs/isometric-geography-system.md` | Water surface S, shore band, rivers/lakes—canonical reference for height comparisons. |

### 4.2 Current Behavior (baseline)

- Legacy **water bridges** use straight segments over water and **water-slope** (`IsWaterSlopeCell`) cells in `RoadManager` bridge helpers.
- **`CanPlaceRoad`** still rejects **all** shore land for generic road placement, which **blocks** strokes before bridge logic unless gates are contextual (§5.3).
- Cut-through / preview neighbor refresh issues may still affect manual mode; parity between `Apply` and `Revert` must be preserved when touching this flow.

## 5. Proposed Design

### 5.1 Target Behavior (water-only)

1. **Trigger:** Candidate span is a **single straight** run whose interior cells are all **open water** and/or **shore** (same notion as `IsWaterOrWaterSlope` / project equivalent). End cells are **land** at `bridge_height`.
2. **`bridge_height`:** Taken from the **start land endpoint** of the span (consistent with existing pipeline assumptions).
3. **Elevated vs normal:** If `bridge_height` is **above** S and **above** bed under water for all spanned wet cells → **elevated** water bridge. If the stroke matches **today’s low bridge** (deck at shore/bank height in the water-adjacent band) → **normal bridge** sub-case; same water-only and straight rules apply.
4. **No terraform:** Do not flatten or raise terrain to build the crossing; deck placement and existing skips for water/shore in `PathTerraformPlan.Apply` carry the behavior.
5. **No bridge intersections:** Do not accept a placement that is a **second** bridge span intersecting an existing bridge span on the grid (definition: overlapping deck cells that are both classified as bridge-under-this-feature, or crossing axis-aligned bridge corridors—implement as a clear, documented test). Straight **continuation** of ordinary roads onto a new water bridge is in scope; **X/T between two bridges** is not.
6. **Invalid manual strokes:** Use project-standard UX (e.g. hidden misleading preview + message) where already specified.
7. **AUTO:** Same validity; fail local segment without aborting the whole step.

### 5.2 Architecture

- Keep a **single** road preparation entry path (`TryPrepareRoadPlacementPlan` and related). Add or adjust a **water-bridge-only** validation phase that annotates deck height for the span.
- **Do not** add dry-gap or cliff-led bridge branches in this issue.
- **GridManager:** no new responsibilities; helpers/services hold bridge rules.

### 5.3 Pre-pipeline gates: shores must reach the pipeline

**Problem:** `TerrainManager.CanPlaceRoad` returns **false** for `IsWaterSlopeCell` land to keep ordinary streets off the coast buffer, but **water bridges require** tracing over those cells. Manual `HandleRoadDrawing`, `GridPathfinder`, and AUTO must not reject the cursor/path **before** `TryPrepareRoadPlacementPlan` when the stroke is a valid **water** bridge.

**Requirements:**

1. Use **context** or a dedicated check: allow shore (and water) steps when they are part of a **water-bridge-candidate** path, while keeping **ordinary** street placement off shore unless product later changes global rules.
2. **Pathfinder** must produce paths that include shore steps when they are the only viable approach to a water crossing, consistent with the plan phase.
3. **Safety:** Global “allow all roads on shore” is **not** acceptable without breaking the coast buffer; scoping is mandatory.

### 5.4 Algorithm sketch (water-only)

1. Input: filtered cell path from existing road drawing / AUTO.
2. Detect **at most one** contiguous run of `open water ∪ shore` per §5.1; enforce **axis-aligned** straight line through that run (reuse or simplify existing straightening/validation; **do not** extend to dry cells).
3. Compute `bridge_height` from land endpoint; verify land ends match height.
4. Verify **elevation:** for each wet cell under the deck, `bridge_height > S` and `bridge_height > H_bed` (or project-defined bed height under that cell). Use **affiliated water body** / junction rules from `WaterManager` where multi-body contact applies.
5. Verify **no bridge intersection** per §5.1.
6. Emit plan metadata for uniform deck height on span cells; commit through existing road apply path.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-03-30 | Implicit bridges; no separate tool | Same as legacy water bridges. | Dedicated bridge mode |
| 2026-03-30 | **Water-only spans** | Removes dry-cliff complexity and misclassification vs slope roads. | Dry “high bridges” |
| 2026-03-30 | **No bridge–bridge intersections** | Cuts resolver and UX scope. | Full junction support |
| 2026-03-30 | **No terraform** for crossing | Deck-only; geography stays authoritative. | Bank sculpting |
| 2026-03-30 | `bridge_height` from start; land ends match | Same end rule as before. | Independent end height |
| 2026-03-30 | Shore = traceable for water bridges; gate must be contextual | Unblocks cliff-adjacent and narrow water entries. | Global `CanPlaceRoad` allow |
| 2026-03-30 | Low bridge at shore height = sub-case of same feature | One mental model; elevated = strictly above S and bed. | Separate feature id |

## 7. Implementation Plan

### Phase 1 — Water-bridge validity and planning

- [ ] Implement or trim bridge validation to **water/shore only**; remove or disable **dry-gap** branches tied to this issue.
- [ ] Enforce **straight span** and **no bridge intersection** rules.
- [ ] Enforce `bridge_height` vs **S** and **bed** under span; preserve **normal** low-bridge case at shore height.
- [ ] **Pre-pipeline alignment:** `CanPlaceRoad` / pathfinder / manual early exits (§5.3).
- [ ] Tests or manual QA matrix for: narrow lake, river, shore approach from higher land, multi-surface junction (per spec).

### Phase 2 — Manual preview and commit integrity

- [ ] Preview/commit do not damage off-path cells; `Apply`/`Revert` neighbor waves stay matched for cut-through if still used.

### Phase 3 — AUTO and persistence

- [ ] AUTO uses same water-bridge validation.
- [ ] Save/load without new flags; minimap unchanged.

### Phase 4 — Spec and cleanup

- [ ] Update `isometric-geography-system.md` for reduced-scope bridges.
- [ ] Mark BACKLOG verified after user QA; migrate lessons; delete this file per AGENTS.md.

## 8. Acceptance Criteria

- [ ] **Water-only:** Committed elevated spans cross **only** open water + shore interior; **no** dry-gap bridge commits under this feature flag/logic.
- [ ] **Straight:** Span is a single axis-aligned run; **no** supported bridge–bridge intersection.
- [ ] **Heights:** For elevated cases, deck height **>** S and **>** bed under water along the span; low bridge at shore height still works as today.
- [ ] **No terraform** requirement to create the crossing beyond existing road placement behavior.
- [ ] Shores are **included** in valid traces when rules pass; pre-pipeline does not block them (§5.3).
- [ ] Manual + AUTO + pathfinding **consistent** for eligible layouts.
- [ ] Save/load; minimap; road cache invalidation.
- [ ] Manual tracing does not incorrectly wipe zoning, buildings, or water on unrelated cells.

## 9. Issues Found During Development (historical)

| # | Description | Root cause | Resolution / status |
|---|-------------|------------|---------------------|
| 1 | Adjacent terrain wrong after cut-through road preview drag | `PathTerraformPlan.Revert` vs `Apply` neighbor wave mismatch | Fixed with matching wave count; still verify when changing preview. |
| 2 | Slope roads “bridge invalid” over dry terrain | **Obsolete for reduced scope** — dry bridges removed from FEAT-44. | N/A under §2.2. |
| 3 | Manual/pathfinder blocked on shore routes | `CanPlaceRoad` rejects `IsWaterSlopeCell` before pipeline | Address via §5.3. |

## 10. Lessons Learned

- Align bridge validation indices with **cardinal-expanded** paths used by `ComputePathPlan` / resolver.
- **Longest-prefix** search must not silently shorten a stroke that was validated as a **single** water bridge span.
- **`CanPlaceRoad` is not bridge-aware;** water-bridge work **must** update manual gates, A* pathfinding, and AUTO together for shore-inclusive paths.
- **Do not mix dry-gap logic with slope roads**—out of scope; avoids the worst false “invalid bridge” cases.
- After QA: migrate lessons to canonical docs and remove this file per AGENTS.md.
