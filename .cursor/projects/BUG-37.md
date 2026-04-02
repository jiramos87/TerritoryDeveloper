# BUG-37 — Manual street drawing clears RCI zones and buildings on cells Moore-adjacent to the road stroke

> **Issue:** [BUG-37](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

## 1. Summary

When the player draws a **manual street** (ordinary road tool), **RCI** **zones**, **buildings**, and related visuals are reportedly removed on cells **Moore-adjacent** to the **road stroke**, not only on cells that should carry the new **street**. This regresses behavior relative to completed **BUG-25**. The intended outcome is to limit removal of development to the **street placement footprint** and any **terraform plan** footprint the design explicitly allows—keeping **Moore neighbors** that are not part of that footprint intact (no undocumented “expropriation band”).

## 2. Goals and Non-Goals

### 2.1 Goals

1. **Footprint clarity:** After a successful manual **street** placement, **RCI** **zones**, **buildings**, and non-street development on cells **outside** the agreed **street placement footprint** and **terraform plan** footprint remain unless a written rule in the geography or roads spec says otherwise.
2. **Pipeline fidelity:** Implementation must follow the **road validation pipeline** and project invariants (same family as manual **street** / **interstate** / AUTO roads per geography spec); the agent chooses code changes without altering intended game rules unless this spec is updated.
3. **Verifiability:** Acceptance can be checked in play: trace a **street** through developed land and confirm which **cells** still host **zones** / **buildings**.

### 2.2 Non-Goals (Out of Scope)

1. **BUG-49** (street preview cadence / full-stroke preview UX)—note as related; do not expand scope unless required to match player expectations for footprint.
2. **Interstate** rules (**cut-through** forbidden, distinct validation)—this issue targets **ordinary street** (manual non-interstate); **interstate** behavior stays as today unless a shared fix is behavior-neutral.
3. Intentionally widening how much land a **street** may claim beyond what is needed to stop **unintended** neighbor loss.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | I draw a **street** along the edge of developed blocks so only the path that becomes road is affected; lots and **buildings** beside the **stroke** stay. | After placement, no loss of **RCI** **zones** or **buildings** on **Moore-adjacent** cells that are outside the defined footprints. |
| 2 | QA / Designer | I can state unambiguously which **cells** may change when a **street** is committed. | Spec and acceptance use glossary terms (**road stroke**, **terraform plan**, **Moore neighborhood**, **street**) so tests match docs. |

## 4. Current State

### 4.1 Observed vs expected (domain)

| | Description |
|---|-------------|
| **Observed** | Manual **street** trace clears **zone** visuals, **buildings**, and **zoning** on cells **next to** the **road stroke** (Moore-adjacent in reports). |
| **Expected** (backlog) | Only **cells** that actually receive the **street** (and any footprint required by validation / **terraform plan**) should change; **neighbors** keep **RCI** and **buildings** unless design documents a wider clear. |

### 4.2 Systems map (from backlog)

| Area | Backlog pointers |
|------|------------------|
| Street input / commit | `RoadManager.cs`, `GridManager.cs` |
| **Terraform plan** / terrain refresh | `TerrainManager.cs`, `TerraformingService.cs` |
| **RCI** / **zone** lists | `ZoneManager.cs` |

### 4.3 Implementation investigation notes (for the implementing agent)

_Not product requirements; technical leads only._

- **Terraform plan** application refreshes terrain beyond the **road stroke** line (plan **adjacent** cells and neighbor refresh waves, including **cut-through** depth). Terrain refresh may replace child visuals; guards may not cover every **building** hierarchy.
- **Diagonal step expansion** turns sketched diagonals into cardinal steps; some cardinal **cells** may not lie under the cursor path but are still part of the planner’s **stroke** for rules and prefabs.
- Confirm whether `DemolishCellAt` or bulk **zone** clears are invoked from street mode (verify against `GridManager` / `RoadManager`).

## 5. Proposed Design

### 5.1 Target behavior (product)

1. **Protected neighbor rule:** Any **cell** not in the **street placement footprint** and not in an explicitly allowed **terraform plan** footprint must not lose **RCI** **zones**, **buildings**, or other non-street development because of that **street** placement.
2. **Terraform vs development:** If terrain must change on a **neighbor** **cell** for **height constraints** or **cut-through corridor** art, prefer approaches that preserve **buildings** and **RCI** **zones** on that **cell** unless the canonical spec is updated to allow removal.
3. **Invariants:** **HeightMap** and **Cell.height** stay synchronized on every write; **road cache invalidation** after road topology changes; no new responsibilities on `GridManager` beyond what invariants already allow—extract helpers if needed.

### 5.2 Architecture / code (agent-owned)

The implementing agent picks concrete edits (which managers, helpers, and plan phases to adjust) provided the result matches section 5.1 and does not weaken **interstate**, **water bridge** / **wet run**, or **cut-through** behavior except where this spec explicitly narrows **unintended** neighbor loss.

### 5.3 Verification approach (agent-owned)

Reproduce with a developed strip; classify cleared **cells** against planner footprint vs neighbor refresh; regression-test **deck span** / **wet run** and **cut-through** **street** cases after the fix.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Backlog “expected” is authoritative: no extra expropriation band beyond documented footprint. | Matches BACKLOG acceptance. | Wider intentional clear—rejected without design change. |
| 2026-04-02 | Narrowing neighbor loss must not break **height constraints**, **cliff** / **slope** continuity, or **shore** rules. | Geography spec wins on terrain. | Disabling all neighbor terrain refresh—likely invalid. |

## 7. Implementation Plan

### Phase 1 — Confirm mechanism

- [ ] Reproduce: **street** along **RCI** strip with **buildings**; record which **cells** clear.
- [ ] Map cleared **cells** to **road stroke**, **terraform plan** region, and terrain neighbor refresh (implementation detail).
- [ ] Check for demolish / **zone** clear calls tied to street tool (implementation detail).

### Phase 2 — Fix and regress

- [ ] Minimal behavior-preserving fix for **ordinary street**; respect **road validation pipeline** and invariants.
- [ ] Manual: **street** on flat developed land; **street** with **cut-through**; **street** with **water bridge** / **wet run** if applicable.
- [ ] If player-visible footprint rules change, update geography spec section on manual **streets** (or roads spec cross-link) per terminology policy.

## 8. Acceptance Criteria

- [ ] Manual **street** through or along developed **RCI** land does **not** clear **zones** or **buildings** on **Moore-adjacent** **cells** that are outside the **street** and **terraform plan** footprints defined for this fix (aligned with BACKLOG).
- [ ] No regression for manual **street** over **water bridge** / **wet run** or **cut-through** scenarios that the geography / roads specs already allow.
- [ ] Terrain **height constraints** and **HeightMap** / **Cell.height** sync remain satisfied (project invariants).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | — | — | — |

## 10. Lessons Learned

- (Complete on closure; migrate durable rules to canonical specs / glossary if concepts change.)

## Open Questions (resolve before / during implementation)

Use **canonical terms** from [`glossary.md`](../specs/glossary.md) (e.g. **road stroke**, **street**, **terraform plan**, **Moore neighborhood**, **cardinal neighbor**, **RCI**, **zone**, **building**, **cut-through corridor**, **height constraint**, **diagonal step expansion**, **save data** / **load pipeline**). These questions define **game logic**, not code.

1. **Road stroke vs input trace:** For a diagonal player drag, **diagonal step expansion** yields extra **cardinal** **cells** on the **road stroke**. Should every such **cell** be treated as part of the **street** for what may be cleared (development removal allowed), or should clearing follow only **cells** the player’s gesture directly covers? (Affects whether “beside the stroke” includes those inserted **cardinal** steps.)

2. **Terraform plan footprint vs street cells:** If the **terraform plan** must modify terrain on a **Moore neighbor** (or **cardinal neighbor**) of a **street** **cell** to satisfy **height constraints** or **cut-through** rules, may that modification remove **RCI** **zones** or **buildings** on that neighbor, or must development on that **cell** be preserved while terrain rules still hold?

3. **Street vs existing building:** If the **road stroke** crosses a **cell** that already has a **building** (or fully developed **zone**), should the **street** replace that development, block placement, or follow another rule?

4. **Cut-through corridor and neighbors:** When a **cut-through corridor** applies to an **ordinary street**, may **Moore-adjacent** **cells** outside the visible **street** line lose **RCI** or **buildings** for corridor / **cliff** continuity, or must they remain developed?

5. **Persistence verification:** After placing a **street** through dense development, must **save data** round-trip (**load pipeline**) be verified for this issue, or is in-session play verification enough?

## References

- Backlog: **BUG-37**, **BUG-25** (completed), **BUG-49** (related preview).
- Canonical specs: `isometric-geography-system.md` (manual **streets**, **terraform plan**, **road stroke** vocabulary in section 14 and road chapters), `roads-system.md` (**road validation pipeline**).
- Glossary: **Street (ordinary road)**, **Terraform plan**, **Road stroke**, **Diagonal step expansion**, **Cut-through**, **Moore neighborhood**, **RCI**, **Zone**, **Building**.
