---
slug: landmarks-mvp
status: seed-stub
supersedes_master_plan: landmarks (closed 2026-05-16 — pre-arch drift, unstarted)
parent_exploration: docs/mvp-scope.md §3.13 (D17 lock)
related_master_plans:
  - region-scene-prototype (closed — provides RegionScene shell)
  - city-region-zoom-transition (open — CoreScene shell + scale transition)
  - full-game-mvp (umbrella)
  - utilities-pool-mvp (seed — landmarks hook on utility contributor registry)
related_specs:
  - docs/mvp-scope.md §3.13 Landmarks (D17 lock: 4 total = 2 City + 2 Region)
  - docs/mvp-scope.md §3.31 Toolbar (D32: Landmark tool, 4 cards)
  - docs/mvp-scope.md §3.20 Info panel (D20: landmark template row)
arch_decisions_inherited:
  - DEC-A22 (prototype-first-methodology)
  - DEC-A23 (tdd-red-green-methodology)
  - DEC-A29 (iso-scene-core-shared-foundation — landmarks at City + Region)
  - DEC-A30 (corescene-persistent-shell — landmark catalog UI in CoreScene)
  - DEC-A24 (game-ui-catalog-bake — landmark sprites via asset-pipeline catalog)
  - DEC-A18 (db-lifecycle-extensions — landmark unlock state persistence)
arch_surfaces_touched:
  - data-flows/persistence (landmark unlock state in save schema)
  - layers/system-layers (Territory.Landmarks new layer)
---

# Landmarks — Exploration Seed Stub (MVP)

**Status:** Seed stub. `/design-explore` to expand into full design doc.

**Replaces:** Closed master plan `landmarks` (Bucket 4-b MVP, 12 stages, never started, drifted on 36 arch decisions since 2026-04-24).

---

## Problem Statement

Landmarks are scale-unlock rewards — unique buildings that gate behind population/budget/service/time thresholds and grant visible perks (happiness boost, tax bonus, capacity raise). D17 locks **4 landmarks total — 2 at City scale + 2 at Region scale**. National-scale big-projects gating DROPPED per D3 — no country-tier mega-landmarks.

Landmarks differ from regular zone buildings: unique sprite (not from sprite-gen catalog), unlock state persists in save, single-instance per save (player can't place 5 of the same landmark), placement validators check both terrain + unlock condition.

No landmark system exists in code. Original closed plan would have built this once but pre-dated D17 lock (4 total), D32 toolbar lock (Landmark as 11th toolbar tool, 4 cards), DEC-A24 (catalog bake pipeline), DEC-A29/A30 (CoreScene + shared iso-scene-core).

---

## Open Questions (resolve at /design-explore Phase 1)

### Identity + unlock conditions

1. **4 landmark identities — 2 City + 2 Region.** What are they? (Specific names + sprite concepts.) MVP-scope §3.13 defers to "Bucket 2 / Bucket 3 author time" — that's now.
2. **Unlock condition per landmark.** Population threshold (city size)? Budget threshold (total revenue collected)? Service threshold (all S-zones at 80% coverage)? Time threshold (game-month count)? Composite?
3. **Reward per landmark.** Happiness boost (flat +N or %)? Tax bonus (% on R/C/I yield)? Capacity raise (max R density tier +1)? Per-landmark unique reward or one shared reward shape?

### Architecture

4. **Where does landmark state live?** Single `LandmarkRegistry` service or per-scale (City + Region)? How does it integrate with shared iso-scene-core registry (DEC-A29)?
5. **Save schema delta.** Unlock state = `{landmark_slug: unlocked_at_timestamp, placed_at_cell: (x,y) | null}` per save. Per-scale or single flat array? Bucket 3 save-schema-evolution dependency.
6. **Catalog integration.** Landmark sprites go through asset-pipeline catalog (DEC-A24/A25)? Or are they hand-authored unique sprites outside the catalog? Subtype-picker shows 4 cards per toolbar tool (D32) — card identity = catalog row?

### UI surface

7. **Landmark catalog panel** (D17 mention: "4 cards, locked/unlocked state"). Where does it live? Standalone panel? Tab inside stats-panel? Click on toolbar Landmark tool opens subtype-picker with 4 cards — picker = catalog?
8. **Locked-state UI.** Locked card greyed-out with unlock condition tooltip on hover? Or fully hidden until unlock fires? Player should know what's coming or be surprised?
9. **Unlock notification.** Toast tier (D34: city milestones use sticky tier) on unlock fire? Modal with sprite reveal? Just inline catalog state change?

### Placement

10. **Placement validators.** Terrain constraints per landmark (e.g. Solar Mega-Farm needs flat terrain, Coastal Lighthouse needs water-adjacent)? Or are all 4 placeable on any zoneable cell?
11. **Demolish behavior.** Can player demolish a placed landmark? Does demolish lock it again or stay unlocked for re-placement? Refund construction cost?

### City vs Region split

12. **City landmarks unlock + place + display at CityScene.** Region landmarks unlock + place + display at RegionScene. Does a City landmark contribute to Region-level signals (e.g. happiness propagation)? Or strictly scoped?
13. **Region landmark placement model.** Region grid = 64×64 (region-cells). 1 region-cell = 32×32 city-cells. Where does a Region landmark "live"? On a region-cell (replaces the city that would otherwise occupy that anchor)? On the inter-city road network? Floating sprite at region scale?

### Utilities hook (seam)

14. **Utility contributor multiplier.** Original closed `utilities` plan exposed `RegisterWithMultiplier` for landmarks (e.g., Solar Farm landmark gives +N power per neighbouring solar plant). Keep this seam in MVP, or defer to post-MVP? Utilities seed open question 13 cross-refs this.

---

## Scope NOT in this seed (defer or reject)

- **National-scale landmarks** (D3 lock OUT).
- **Achievement/badge tie-in** (MVP-OUT — no achievements).
- **Per-landmark policy knobs** (no per-landmark configuration — flat reward).
- **Landmark unlock via in-game purchase** (unlocks are condition-gated, not money-gated).
- **More than 4 landmarks** (D17 hard lock).

---

## Pre-conditions for `/design-explore`

- `region-scene-prototype` shipped (yes).
- Bucket 3 save-schema-evolution plan landed OR landmark unlock state slots into existing v3 envelope as a sub-object (defer decision to Phase 1).
- `game-ui-catalog-bake` plan closed (yes 2026-05-16) — catalog DB schema available for landmark sprite rows.

## Next step

`/design-explore docs/explorations/landmarks-mvp.md`
