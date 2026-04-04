# FEAT-49 — Local sector inspector (rectangular grid selection, layered analysis, JSON export)

> **Issue:** [FEAT-49](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-04
> **Last updated:** 2026-04-04

<!--
  Structure guide: ../projects/PROJECT-SPEC-STRUCTURE.md
  Use glossary terms: ../../.cursor/specs/glossary.md (spec wins if glossary differs).
-->

## 1. Summary

Add an in-game **local sector inspector**: the player or developer defines a **rectangle** on the **cell** grid (interaction pattern analogous to **zoning** drag), sees a light **transparent** darkening over the selected **cells**, and opens a **panel** with a **layer** selector. Each **layer** summarizes a slice of **city** state inside the bounds—initially **streets** (topology, **prefab** orientation consistency hints) and **terrain** (**HeightMap**, **water map**, **shore band**-relevant signals where applicable). The same structured snapshot can be exported as **JSON** and stored via the **Editor export registry** (**Postgres** **`document jsonb`** when configured, otherwise **`tools/reports/`** fallback), as **Interchange JSON**-style diagnostics—not **Save data**.

## 2. Goals and Non-Goals

### 2.1 Goals

1. **Rectangle selection** on the grid reusing the established drag-to-rect pattern used for **zoning** (and aligned with **FEAT-35** notes on the same mechanism).
2. **Visual feedback:** selected **cells** show a subtle semi-transparent overlay while the tool is active or a snapshot is pinned.
3. **Analysis panel** with at least two **layers**: (a) **Street** / **road** diagnostics inside the bounds; (b) **Terrain** / **water** diagnostics (**HeightMap**, **open water**, **slope** class as surfaced by existing systems).
4. **Street** layer (v1 scope): report whether **street**-occupied **cells** form one or more connected components (graph in **Moore** or **Chebyshev** neighborhood—see **Open Questions**); flag suspected **prefab** / orientation mismatches relative to expected **street** direction or **land slope** rules (read-only heuristics for **debug**, not mutating **roads**).
5. **Export:** produce versioned **JSON** suitable for **Postgres** insert through the same family of tooling as **unity-development-context** §10 (**Editor export registry**, **Interchange JSON**); document the **`artifact`** / **`schema_version`** name in the **Decision Log** when implemented.
6. **Dual use:** optimize for **development** and QA first; keep UX compatible with a future **player**-facing “urban sector analysis” mode if product enables it.

### 2.2 Non-Goals (Out of Scope)

1. Changing **Save data** shapes or the **Load Game** pipeline (**persistence-system**).
2. Mutating **roads**, **zones**, **terraform**, or **water** from this tool in v1 (read-only inspector).
3. Replacing **territory-ia** MCP or implementing the **future** agent **skill** in this issue (see **§5.1** extension note only).
4. Full graph-theory guarantees for **pathfinding** parity with **RoadManager** / **GridPathfinder**—v1 may approximate connectivity for diagnostics only.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want to drag a **rectangle** on the **map** like **zoning** so that I can scope analysis to a **neighborhood** without hand-counting **cells**. | Rectangle matches min/max **grid** corners; overlay matches bounds; cancel clears selection. |
| 2 | Developer | I want to pick a **layer** and see **street** connectivity and **prefab** consistency hints so that I can spot bad **road stroke** visuals or resolver edge cases. | Panel shows summary + per-issue hints (e.g. disconnected component count, orientation outliers). |
| 3 | Developer | I want a **terrain** **layer** for **HeightMap** / **water** / **slope** signals in the same bounds so that I can correlate **terraform** bugs with **road** issues. | Panel shows aggregates and notable outliers (e.g. **water body** ids, height range). |
| 4 | Developer | I want one-click **JSON** export to **Postgres** (or filesystem fallback) so that agents and scripts can consume the same snapshot. | Export validates against an agreed schema; **Editor export registry** path documented; no secrets in **JSON**. |

## 4. Current State

### 4.1 Domain behavior

**Zoning** and planned **FEAT-35** (bulldozer drag) already describe drag-to-rectangle **grid** selection. There is no unified “sector report” that combines **street**, **terrain**, and **water** in one bounded snapshot for UI + **Interchange JSON**. **Road stroke** semantics and **road validation pipeline** live under **roads-system** and **isometric-geography-system**; **HeightMap** and **cell** height must stay in sync per **invariants**.

### 4.2 Systems map

- **Backlog:** **FEAT-35** (reuse selection pattern); **TECH-59** (optional **MCP** staging for **EditorPrefs** + **JSON**); **TECH-55b** **§ Completed** — **Editor export registry**; **TECH-67** / **ui-design-system** for panel patterns.
- **Specs:** `.cursor/specs/ui-design-system.md` (foundations, components); `.cursor/specs/managers-reference.md` — **Zones & Buildings**; `.cursor/specs/roads-system.md` + **isometric-geography-system** §9, §10, §13, §14.5 (**street**, **wet run**, **pathfinding costs**); `.cursor/specs/water-terrain-system.md` + **geo** §2–§5; `.cursor/specs/unity-development-context.md` §10 (**Editor** diagnostics, **Postgres** path).
- **Runtime:** `GridManager` (**`GetCell`**, no direct **grid** array access from new code), `RoadManager`, `TerrainManager`, `WaterManager`, `UIManager`, `CursorManager` (or equivalents for input routing).

### 4.3 Implementation investigation notes (optional)

- Prefer a dedicated **helper** or **service** class for aggregations; do not grow **`GridManager`** beyond coordination (**invariants**).
- **Connectivity:** subgraph of **cells** marked as **street** inside bounds; edge rules **TBD** in **Open Questions**.
- **Prefab** alignment: compare **SpriteRenderer** / prefab variant metadata against expected cardinal direction or **slope** bucket—implementation-owned, but reports must label confidence (heuristic vs authoritative).

## 5. Proposed Design

### 5.1 Target behavior (product)

1. **Modes:** A dedicated tool or debug-gated entry toggles **local sector inspector**; exiting clears overlay unless user “pins” a snapshot (**TBD** in **Open Questions**).
2. **Layers** are informational tabs or dropdown entries; v1 = **Streets**, **Terrain / water**.
3. **Export** produces **JSON** with: bounds, **schema_version**, timestamp, optional **`backlog_issue_id`** when used from **Editor** workflow, and per-**layer** payloads. Storage follows **unity-development-context** §10 (**DB-first** when **Postgres** available).
4. **Future (not in v1):** A **Cursor Skill** or **territory-ia** tool could request the latest **sector snapshot** **JSON** (from **Postgres** or staged file) so an agent can reason over the same **layer** summaries without driving **Unity** UI—coordinate with **TECH-48** / **TECH-59** when that becomes a backlog item.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- New UI **panel** + input controller wiring; reuse **zoning** rectangle math.
- Read-only queries via **`GridManager.GetCell`**; **`RoadManager`** / **`TerrainManager`** / **`WaterManager`** as needed.
- **JSON** schema under `docs/schemas/` (or extend an existing **interchange** schema) + fixture for **`npm run validate:fixtures`** if new **artifact** type is added.
- **Editor** menu item optional: “Export last sector snapshot” mirroring other **Reports** flows.

### 5.3 Method / algorithm notes (optional)

None fixed—implementer proposes graph build and **prefab** checks; document false-positive rates in **Decision Log** if heuristics are noisy.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-04 | Issue id **FEAT-49** | Next free **FEAT-** id in **BACKLOG** / archive scan. | **TECH-** only (rejected: product may ship analysis to players). |
| 2026-04-04 | **Interchange JSON** / **Editor export registry** alignment | Reuse **Postgres** **`document jsonb`** pipeline and glossary vocabulary; not **Save data**. | Ad-hoc logs only (rejected: breaks agent/tooling consistency). |

## 7. Implementation Plan

### Phase 1 — Selection + overlay

- [ ] Rectangle pick (reuse **zoning**-style drag); clamp to **map** bounds.
- [ ] Semi-transparent overlay renderer for selected **cells**.

### Phase 2 — Panel + **Street** layer

- [ ] Panel shell + **layer** selector.
- [ ] **Street** subgraph summary + **prefab** / orientation heuristics.

### Phase 3 — **Terrain** / **water** layer

- [ ] **HeightMap** / **cell** height stats, **water map** / **open water** signals, **slope** highlights as available from public APIs.

### Phase 4 — **JSON** export + validation

- [ ] Define **`artifact`** type and schema; wire **Editor export registry** / filesystem fallback per **unity-development-context** §10.
- [ ] Document export in **`docs/postgres-ia-dev-setup.md`** or **README** if new menu paths are added.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| New **JSON** **schema** or fixture | Node | `npm run validate:fixtures` | If schema lives under `docs/schemas/` |
| No stale **Spec:** links | Node | `npm run validate:dead-project-specs` | After **BACKLOG** / cross-links |
| **Street** / **terrain** summaries match manual spot-check | Manual | Unity **Play Mode** / **Editor** | Sample 2–3 known **maps** |

## 8. Acceptance Criteria

- [ ] User can define a rectangular **cell** region with the same interaction family as **zoning** drag.
- [ ] Selected region shows a clear, non-blocking darkening overlay.
- [ ] Panel lists at least **Streets** and **Terrain**/**water** **layers** with meaningful summaries.
- [ ] **Street** layer reports connectivity across **street** **cells** in the selection and surfaces **prefab** / direction anomalies as warnings (heuristic acceptable if documented).
- [ ] **JSON** export succeeds to **Postgres** when configured, else writes under **`tools/reports/`** per existing **Reports** behavior.
- [ ] No **Save data** format changes; no **road** mutations through this feature in v1.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | — | — | — |

## 10. Lessons Learned

- *(Fill at closure.)*

## Open Questions (resolve before / during implementation)

1. **Availability:** Should **local sector inspector** ship only in **development** builds, remain behind a **debug** toggle in **player** builds, or always be visible? (Product decision.)
2. **Connectivity definition:** For the **street** **layer**, should adjacency use **Moore** (8-neighbor) or **Chebyshev** / cardinal-only edges matching **pathfinding** neighbor rules (**geo** §10), and should **interstate** / **bridge** **cells** be in the same graph as ordinary **streets**?
3. **Bounds vs city:** Is “connected” evaluated only among **street** **cells** inside the rectangle, or should the report indicate whether components attach to the wider **city** **street** network outside the selection?
4. **Pinned snapshot:** Should the overlay and **JSON** refer to a frozen snapshot at selection time, or live-update on every **simulation tick** while the panel is open?
