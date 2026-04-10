---
purpose: "Project spec for FEAT-51 — In-game game data dashboard (charts and dense stats)."
audience: both
loaded_by: ondemand
slices_via: none
---
# FEAT-51 — In-game game data dashboard (charts and dense stats)

> **Issue:** [FEAT-51](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-05
> **Last updated:** 2026-04-05

<!--
  Exploration charter: ../../docs/ui-data-dashboard-exploration.md
  Structure guide: ../projects/PROJECT-SPEC-STRUCTURE.md
-->

## 1. Summary

Deliver an **in-game data dashboard**: time-aligned **city** metrics, derived summaries, and **chart** views that reuse the **UI design system** (**palette**, **typography**, **surface** tokens from **`UiTheme`** — see **`ui-design-system.md`** **§1**, **§5.2**, **§5.3**). This builds the **simulation** → history → presentation pipeline described in the exploration doc; it is **not** a polish-only pass.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Record a **configurable, bounded** history of selected **simulation** scalars (aligned with **CityStats**, **EconomyManager**, **DemandManager**, **StatisticsManager**, **TimeManager** semantics) for charting and KPI rows.
2. Provide a **player-accessible** dashboard surface (panel or dedicated mode) with **card** layout, at least one **line** or **bar** chart proving the pipeline, and **scroll**/**modal** behavior consistent with **`ui-design-system.md`** input policy.
3. Theme charts and chrome with **`UiTheme`** (and extend tokens if needed, e.g. grid line / series colors) so the dashboard does not look like a separate product skin.
4. Keep dashboard UI updates **event- or tick-driven**, not per-frame **`Update`** polling across dozens of fields (**BUG-14**).

### 2.2 Non-Goals (Out of Scope)

1. **Map** **info view** / per-**cell** tint overlays (exploration **§2.6**) — track as a **follow-up** **FEAT-** when prioritized; may depend on **grid** rendering cost review.
2. Full **UI Toolkit** migration or replacing all **uGUI** **HUD** in this issue.
3. Changing core **simulation tick** ordering or **AUTO** rules solely for the dashboard (read-only observation unless a separate **BACKLOG** item approves behavior changes).
4. Committing to a specific third-party chart package in this spec — **spike** first (**Decision Log**).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|----------------------|
| 1 | Player | I want to see **treasury** and **population** (or agreed substitutes) over **in-game time** so I can understand trends. | At least one chart shows ≥2 series or a clear single-metric history with **TimeManager**-aligned labels or ticks. |
| 2 | Player | I want dense **stats** (e.g. **demand (R / C / I)**, utilities) in one place without hunting scattered **HUD** rows. | Dashboard shows a **preset** grid of **cards** with live values refreshed without noticeable hitch (profiled on target resolution). |
| 3 | Developer | I want a documented pipeline (history → optional derived metrics → view) so new metrics can be added without duplicating **UIManager** string formatting. | **Implementation Plan** phases delivered; extension point documented in **Decision Log** or **`ARCHITECTURE.md`** on closeout. |

## 4. Current State

### 4.1 Domain behavior

The **HUD** exposes many **current** scalars (**money**, **population**, **happiness**, **demand (R / C / I)**, utilities) via **`UIManager`** and controllers, but there is **no** first-class **time-series** store for **simulation** metrics. **Charts** (sparklines, multi-series lines) require **history** and a rendering approach beyond ad hoc **uGUI** **Image** stretching.

### 4.2 Systems map

| Area | Pointers |
|------|----------|
| Exploration (mechanisms) | [`docs/ui-data-dashboard-exploration.md`](../../docs/ui-data-dashboard-exploration.md) |
| **UI** stack | [`.cursor/specs/ui-design-system.md`](../../.cursor/specs/ui-design-system.md) **§1** Foundations, **§3** patterns (**modal**, scroll vs **camera** — **§3.5**) |
| **Simulation** | [`.cursor/specs/simulation-system.md`](../../.cursor/specs/simulation-system.md) (**simulation tick**, **AUTO** — consume metrics after tick, do not redefine tick order here) |
| **Persistence** | [`.cursor/specs/persistence-system.md`](../../.cursor/specs/persistence-system.md) — if history must survive **Save**/**Load** |
| **Managers** | [`StatisticsManager`](../../Assets/Scripts/Managers/GameManagers/), [`EconomyManager`](../../Assets/Scripts/Managers/GameManagers/), [`DemandManager`](../../Assets/Scripts/Managers/GameManagers/), **`CityStats`**, **`TimeManager`** |
| Visual tokens | **`UiTheme`**, **`ui-design-system.md`** **§5.2** / **§5.3** |

### 4.3 Implementation investigation notes (optional)

- Spike **XCharts** (or alternative) behind **`IChartView`** / provider per exploration **§2.3**; validate **LICENSE**, **Canvas** **CanvasScaler** resolutions, and sample count limits.
- **`ObservableValue<T>`** / **ScriptableObject** channels vs tick-callback registration — choose without new **singletons**; **Inspector** + **`FindObjectOfType`** only where the codebase already does (**invariants**).

## 5. Proposed Design

### 5.1 Target behavior (product)

1. The player opens the **data dashboard** from an agreed entry point (toolbar control, **popup** type, or sub-panel — **implementation** choice).
2. The dashboard shows **at least one** chart backed by **real** **simulation** history and **at least one** **preset** of **KPI** **cards** for agreed metrics.
3. History length and metric set are **bounded** (memory/CPU); noisy signals may use **sampling** or **bucketed** aggregation (**exploration** **§2.1**).
4. **In-game date** or tick index labels align with **`TimeManager`** (or documented equivalent).

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- **SimulationHistory** (or equivalent **MonoBehaviour** service): subscribes after **simulation** steps (or fixed interval); writes circular buffers per metric id.
- **MetricsComputer** (optional phase): derived series (deltas, moving averages) from raw buffers (**exploration** **§2.4**).
- **Dashboard** prefab(s): **ScrollRect**, **tabs** or preset selector, **DashboardCard** instances; chart host **RectTransform** + provider implementation.
- **Theming**: map **`UiTheme`** **TextPrimary**, **AccentPositive** / **AccentNegative**, **SurfaceCardHud**, and any new **chart** tokens.

### 5.3 Method / algorithm notes (optional)

Deterministic derivations only for **player-visible** formulas; document in **Decision Log** or **reference spec** slice when behavior is locked.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|-------------------------|
| 2026-04-05 | Charter and dependency graph taken from [`docs/ui-data-dashboard-exploration.md`](../../docs/ui-data-dashboard-exploration.md). | Single source for prerequisite subsystems. | Ad hoc scope in **BACKLOG** only. |
| 2026-04-05 | **Map** **info view** deferred to a future issue. | Large scope; **grid** cost and art direction need their own row. | Ship minimal tint in **FEAT-51** (rejected — splits focus). |

## 7. Implementation Plan

### Phase 1 — History spine

- [ ] Define metric ids and circular buffer storage; hook sample points relative to **simulation tick** (or throttled interval) without **`FindObjectOfType`** in per-frame UI paths.
- [ ] Document default capacity and reset behavior on **New Game** (and **Load** if persistence is in scope).

### Phase 2 — Chart engine spike

- [ ] Evaluate candidate chart library (e.g. **XCharts**) behind a small abstraction; verify performance and **LICENSE**.
- [ ] Prove one chart in **Play Mode** with **UiTheme**-aligned colors.

### Phase 3 — Dashboard shell

- [ ] **Dashboard** layout: **cards**, **scroll**, entry point wiring; align **modal** / pointer policy with **`ui-design-system.md`** **§3.5**.
- [ ] Replace ad hoc polling with subscriptions or tick-aligned refresh (**BUG-14**).

### Phase 4 — Product hardening

- [ ] Expand metric set per **Open Questions** agreement; add **7b** **Play Mode** checks.
- [ ] If history must persist: align with **persistence-system** and add save version notes (**Decision Log**).

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| **BACKLOG** / **Spec:** links valid | Node | `npm run validate:dead-project-specs` | After any **`Spec:`** or link edits |
| Chart spike + dashboard **Play Mode** | Manual | Unity **Play Mode** + agreed scene | Record metric ids verified in **Issues Found** if automated **UTF** not available |
| Touch **glossary** / **reference spec** / MCP index bodies | Node | `npm run validate:all` | If durable IA text changes on closeout |

## 8. Acceptance Criteria

- [ ] Bounded **time-series** collection for an agreed **metric** set; no unbounded per-**cell** history by default.
- [ ] At least one **themed** **chart** and one **dashboard** **preset** shipped behind a player-visible entry point.
- [ ] No new **singletons**; no **`FindObjectOfType`** in dashboard **`Update`** loops.
- [ ] **Scroll** / **zoom** coexistence documented or verified against **`ui-design-system.md`** **§3.5** where the dashboard uses **ScrollRect**.
- [ ] Exploration doc remains the **mechanism** reference; **FEAT-51** **Decision Log** records chart library choice and persistence stance.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- *(Fill on closure; migrate to **`ui-design-system.md`**, **`ARCHITECTURE.md`**, or **glossary** as appropriate.)*

## Open Questions (resolve before / during implementation)

1. Which **simulation** scalars are **required** for the **first** shippable dashboard (e.g. **population**, **treasury**, **demand (R / C / I)** levels, **happiness**, employment), and which are **explicitly deferred**?
2. Must **time-series** **history** survive **Save** and **Load**, or is **session-only** history acceptable until a later **persistence** milestone?
3. What is the product-approved **sampling cadence** relative to the **simulation tick** (every tick, every N ticks, or **in-game** day boundaries via **TimeManager**)?
