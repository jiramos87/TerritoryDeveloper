# Water junction merge — implementation plan (BUG-45)

> **Issue:** [BUG-45](../BACKLOG.md)  
> **Canonical geography:** [`.cursor/specs/isometric-geography-system.md`](../.cursor/specs/isometric-geography-system.md) (§12.7 after update)  
> **Status:** partially implemented — Pass B, cascade rule, multi-surface shore corners, and conditional lake-shore refresh are in code; see `BACKLOG.md` **BUG-45** and spec §12.7 / §5.6.2 for shipped behavior.

## 1. Goal

Fix broken visuals where two **registered** water cells meet on a **cardinal** edge with **different logical surface heights** (`S_high > S_low`): voids, floating water, wrong shores, and inconsistent cliffs. Implement a **junction merge** that:

- Extends the **lower-surface** body into a **parametric strip** along the full contact (width follows the upper body’s cross-section where terrain allows).
- Uses **existing** diagonal and cardinal water-shore prefabs, **rotated** from the **contact direction** (not fixed to one map orientation).
- Keeps **`cliffWaterSouth` / `cliffWaterEast`** (and stack height for **`ΔS > 1`**) on the **upper** side, including faces of **upper** diagonal shore cells when required.
- Runs as **Pass B** in `WaterManager.UpdateWaterVisuals`: **after** existing bed normalization (**Pass A**), **before** `PlaceWater`.

## 2. Locked design decisions

| Topic | Decision |
|--------|----------|
| Scope | Any cardinal adjacency **water–water** with `S_high > S_low` (lakes, rivers, mixed). |
| Future triggers | Same pass when **terraform / load / editor** change water or heights nearby (no player terraform yet). |
| Strip width | **Parametric**; narrowest case matches minimum river cross-section in design (e.g. bed + two banks). |
| Absorbed cells | **HeightMap** + **Cell.height** + **`waterBodyId`** align with **lower** logical surface; treated as **open water** (including former water-shore cells) until a finer rule is needed. |
| Two lowers, same `S`, different ids | **Any** of those bodies may receive new cells; **do not** merge the two lower bodies into one id. |
| Pass order | **Pass A** → **Pass B** → `PlaceWater` → `RefreshWaterCascadeCliffs` → shore / slope refresh for affected bbox. |
| Spec | §12.7 updated; Pass A unchanged; Pass B **supersedes** the old “never dry→water” restriction for this junction case. |

## 3. Pipeline placement

**File:** `WaterManager.UpdateWaterVisuals`

1. **`waterMap.ApplyMultiBodySurfaceBoundaryNormalization(hm)`** — Pass A (existing).  
2. **`waterMap.ApplyWaterSurfaceJunctionMerge(hm, gridManager, …)`** — Pass B (new). Must be **idempotent**.  
3. Existing loop: `PlaceWater` for all water cells.  
4. **`terrainManager.RefreshWaterCascadeCliffs(this)`** — extend if upper **diagonal** shores need cliff children.  
5. Call existing **shore refresh** for a **dirty region** covering Pass B cells + Moore halo (reuse `RefreshLakeShoreAfterLakePlacement` patterns or a thin wrapper).

**Do not** run Pass B after `PlaceWater` without a second full refresh (user chose **pre–PlaceWater**).

## 4. Implementation phases

### Phase 1 — `WaterMap` API and body bookkeeping

- Add **`ApplyWaterSurfaceJunctionMerge`** (or a dedicated helper class called from `WaterMap`) taking **`HeightMap`**, **`GridManager`** (or callbacks to read/write **`Cell.height`**), and optional **dirty bounds** output.
- Reuse **`RemoveCellFromBody` / add-to-body** patterns so **`WaterBody`** cell lists and **`waterBodyIds`** stay consistent.
- When assigning **`waterBodyId`** for new lower-surface cells: pick a body with **`SurfaceHeight == S_low`** among cardinal lower neighbors; if multiple ids share **`S_low`**, pick **deterministically** (e.g. smallest id, or first neighbor) — **not** a geometric merge of bodies.

### Phase 2 — Contact detection

- Scan **cardinal edges** between **`IsWater(x,y)`** and **`IsWater(nx,ny)`** with **`GetSurfaceHeightAt`** → **`S_here > S_neighbor`**.  
- Build **edge segments** (4-connected chains) or process per-edge with a **local window** for cross-section width.  
- Store **outward normal** from high to low (which cardinal direction is “downstep”) for **prefab orientation**.

### Phase 3 — Lower-side strip geometry

- For each contact location, compute **allowed width** perpendicular to the contact from the **upper** body’s footprint (river bed + banks, or lake boundary).  
- If **bed alignment** fails (§12.7 / generator constraints), **narrow** the strip rather than skipping the whole junction.  
- Set **lower** cells to bed height consistent with **existing lower neighbors** (reuse min-bed logic where applicable).  
- **Idempotency:** second run makes no changes.

### Phase 4 — Upper-side shore pattern

- Ensure **neighbor heights / water mask** after Pass B cause **`DetermineWaterShorePrefabs`** to select **diagonal** water shores where intended, **or** add a **small junction mask** (e.g. `WaterMap` or `TerrainManager` flags) consulted from **`PlaceWaterShore`** / placement path so art is stable without global side effects.  
- **Rotate** prefab choice from **contact normal** (map N/E/S/W step from high cell to low cell → correct NE/NW/SE/SW cardinal–diagonal pair).

### Phase 5 — `RefreshWaterCascadeCliffs` extension

- After merge, **upper** cells still have **`S_high`**. Ensure **south/east** water cliff stacks spawn on:  
  - open **upper** water toward **lower** water (existing), and  
  - **upper** cells classified as **water-shore / diagonal** at the junction, when the **visible** face still needs a **vertical** drop to the lower surface.  
- For **`ΔS > 1`**, keep **`segmentCount`** tied to surface delta; **underwater cull** and sort cap behavior must match §5.6.2 / BUG-40.

### Phase 6 — Terrain and cell refresh

- Collect **affected cells** from Pass B; expand by **1–2 Chebyshev rings** for land shores.  
- Call **`RefreshLakeShoreAfterLakePlacement`**-style updates (or factor a **`RefreshShoresInBounds`**) so **rim cliffs** and **ordinary slopes** stay coherent with §2.4.1 / §5.6.1.

### Phase 7 — Generators and init

- **`ProceduralRiverGenerator`** (and any lake fallback that creates multi-surface contacts) should **not** rely on hand-placed mouths; after water assignment, **`UpdateWaterVisuals`** must produce correct junctions.  
- Verify **`GeographyManager`** init order: no extra pass required if **`UpdateWaterVisuals`** runs after all water ids are written.

### Phase 8 — Save / load

- **New games:** junction state is whatever Pass A+B produce before save.  
- **Legacy saves:** if a save predates Pass B, loading then calling **`UpdateWaterVisuals`** should **repair** junctions idempotently (verify once in Unity).

### Phase 9 — Verification

- Screenshots / scenes: river (min width) → lower lake; lake–lake step; `ΔS == 2`; long straight contact; corner where contact direction changes.  
- Assert **no** accidental **`MergeAdjacentBodiesWithSameSurface`** between two **lower** bodies that only share absorbed strip geometry.  
- Performance: full-grid scan is acceptable at init; if needed later, restrict to **dirty bbox** when terraform exists.

## 5. Files likely to change

| Area | Files |
|------|--------|
| Orchestration | `WaterManager.cs` |
| Merge + ids | `WaterMap.cs` (new pass; possibly new partial class or `WaterSurfaceJunctionMerge.cs` under `Managers/GameManagers/` or `UnitManagers/`) |
| Heights / cells | `TerrainManager.cs`, `GridManager.cs` (minimal — cell height sync) |
| Shores / cliffs | `TerrainManager.cs` (`DetermineWaterShorePrefabs`, `PlaceWaterShore`, `RefreshWaterCascadeCliffs`) |
| Rivers | `ProceduralRiverGenerator.cs` (only if generator must seed contacts compatible with strip width) |
| Diagnostics | `WaterGeographyDiagnosticsLog.cs` (optional: log junction edits) |

## 6. Multi-confluence (deferred)

When an **upper** cell touches **several** lower-surface neighbors with different **`S`**, prefer **lowest `S`** for strip direction / priority; document tie-break. **T- and X-junctions** can follow the same pass with **iteration until stable** or **priority rules** — mark as **phase 10** if v1 is only **single lower neighbor** per edge cell.

## 7. Completion criteria

- §12.7 and this plan match shipped behavior.  
- BUG-45 moved to **Completed** in `BACKLOG.md` only after **user verification** in Unity (per project workflow).
