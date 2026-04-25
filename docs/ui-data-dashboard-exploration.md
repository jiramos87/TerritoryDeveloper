# Data dashboard — exploration and prerequisite subsystems

> **Open delivery:** [FEAT-51](../BACKLOG.md) (project spec body lives in DB-backed `ia_tasks` row post Step 9.5)  
> **Visual tokens (shipped):** [`ia/specs/ui-design-system.md`](../ia/specs/ui-design-system.md) **§1**, **§5.2** (**`UiTheme`**, **`DefaultUiTheme.asset`**)  
> **Program trace:** **UI-as-code program** (**glossary**)

This document captures **future** work: mechanisms, methods, and subsystems needed **before** in-game **data dashboards** (charts, dense stat grids, time-range sync) are feasible. It is **not** a polish-only delivery. Shipped **HUD** / **MainMenu** visual polish reuses **`UiTheme`** and **`ui-design-system.md`**; dashboard **cards** and **charts** should consume the same **palette**, **typography** rhythm, **spacing**, and **surface** tokens so the product stays visually coherent.

---

## 1. Product intent (why dashboards)

**City-builder** and **simulation** players expect both **aggregate** views (budget, demographics, demand over time) and, eventually, **spatial** views (info overlays on the **map**). References that motivate this direction:

- **Industrial / Grafana-style** dashboards: card grid, consistent semantic colors (green / amber / red), sparklines and multi-series lines, gauges, compact KPI rows.
- **Business analytics** UIs: hero numbers, ring/donut summaries, mixed chart types chosen per data shape (line, area, waterfall).
- **Mods and adjacent games** (e.g. Cities: Skylines info views, workshop “stats panel” mods): heavy use of **tabs**, **legends**, and **map-linked** modes.

**Conclusion:** Shipping a credible dashboard is less about a single “charts screen” and more about a **data pipeline** (history → derived metrics → render → layout) plus optional **map** visualization. UI polish alone cannot substitute for that pipeline.

---

## 2. Prerequisite subsystems (spec-level sketches)

### 2.1 Time-series data collection and storage

**What it is:** Record simulation metrics each tick (or on a configurable interval) into rolling buffers.

**Why it is needed:** Line charts, area charts, and sparklines need **history**, not only the current scalar from **StatisticsManager** / **EconomyManager** / **DemandManager**.

**Design sketch:**

- A `SimulationHistory` (or similar) service subscribed to the simulation tick.
- Per metric: `CircularBuffer<float>` (or `(tickIndex, value)` pairs) with configurable capacity (e.g. 200 samples).
- Candidate metrics: population, treasury, income, expenses, R/C/I **demand** and supply, **happiness**, employment, zone fill, congestion proxies—aligned with glossary/spec terms when implemented.
- Axis labels: map tick indices to in-game dates via **TimeManager** (or equivalent).
- **Persistence:** optional in a first version (reset on load); align with **persistence-system** if history must survive **Save**/**Load**.

**Considerations:** Start with a **small** metric set to prove the pipeline; avoid writing every cell-level stat to history by default (memory and CPU). Prefer **sampling** or **event-summarized** buckets for noisy signals.

---

### 2.2 Observable / data-binding layer

**What it is:** A reactive layer so dashboard widgets update on **data change** instead of polling every frame.

**Why it is needed:** Dense dashboards (10–30 live fields) make `Update()` polling expensive and couple UI to manager internals.

**Design sketch:**

- `ObservableValue<T>` (or **ScriptableObject** channels) with subscribe/unsubscribe in `OnEnable` / `OnDisable`.
- Alternatively: Unity **UI Toolkit** **Data Binding** (Unity 6.x) if the project ever adopts **UI Toolkit** for data-heavy screens—keep the **domain** API UI-agnostic.

**Considerations:** Do not introduce new **singletons**; follow existing **Inspector** + **FindObjectOfType** patterns only where the codebase already does. Respect **BUG-14** (no **FindObjectOfType** in per-frame UI paths).

---

### 2.3 Chart rendering engine

**What it is:** Runtime-capable rendering for line, bar, pie/donut, gauge, and optionally radar / waterfall inside the chosen UI stack.

**Why it is needed:** Production-quality charts are not practical as one-off **uGUI** hacks at scale.

**Options (summary):**

| Option | Pros | Cons |
|--------|------|------|
| **XCharts** (MIT, uGUI) | Many chart types; Inspector preview; active maintenance | Third-party dependency; theming work to match **UiTheme** |
| **Custom `Graphic` / mesh** | Full control | High build and maintenance cost |
| **UI Toolkit** + vectors | Fits data-heavy UI; official binding roadmap | Migration cost vs current **uGUI** investment |
| **RenderTexture** hosting | Could embed external renderers | Latency, DPI, pipeline complexity |

**Recommended approach:** Spike **XCharts** (or equivalent) behind an **`IChartView`** / provider abstraction so the engine can be swapped. Theme charts using the same **surface** / **accent** tokens as **`UiTheme`** / **`ui-design-system.md`** to avoid a “second UI.”

**Considerations:** Validate performance with target **Canvas** scaler resolutions and worst-case sample counts before committing.

---

### 2.4 Stat aggregation and derived metrics

**What it is:** A layer that turns raw series into **deltas**, **moving averages**, **ratios**, and **percent-of-capacity** series.

**Why it is needed:** Dashboards show “rate of change,” “vs last month,” and health indices—not only raw totals.

**Design sketch:**

- `MetricsComputer` (or static helpers + tests) reading `SimulationHistory` and emitting derived buffers in the same shape as raw series.

**Considerations:** Keep derivations **deterministic** and **testable**; document formulas in a reference spec section when behavior is player-visible.

---

### 2.5 Dashboard layout and panel management

**What it is:** A way to arrange **cards** in a grid (scrollable, tabbed presets), instantiate cards from configuration, and optionally drill into detail.

**Why it is needed:** Differs from today’s **PopupType**-driven single panels; needs layout presets (Economy, Population, Infrastructure).

**Design sketch:**

- `DashboardLayout` **ScriptableObject** (or JSON): metric id → chart type → grid cell.
- `DashboardCard` prefab: header, chart host **RectTransform**, optional KPI row.
- Top **tabs** for presets; **ScrollRect** when content exceeds viewport.

**Considerations:** Reuse **modal** stack / input policy from **`ui-design-system.md`** **§3.2** / **§3.5** so scroll does not fight **camera** zoom.

---

### 2.6 Map overlay / info-view system

**What it is:** Tint or overlay **cells** by data dimension (zones, services, value, pollution analogs)—similar in *role* to Cities: Skylines **info views**.

**Why it is needed:** Charts show **aggregates**; the **map** shows **where** problems are. Together they match player expectations for deep management UIs.

**Design sketch:**

- `InfoViewManager`: active mode drives per-**cell** color or material property; legend in **HUD** or sidebar.
- `Gradient` (or token-driven ramp) per view mode.

**Considerations:** Large scope—track as its own **FEAT-** / **TECH-** issue when ready; depends on **`GridManager`** **GetCell** access patterns and rendering cost.

---

## 3. Dependency graph (build order)

```
2.1 Time-series collection
    └──> 2.4 Stat aggregation / derived metrics
            └──> 2.3 Chart rendering engine
                    └──> 2.5 Dashboard layout & panel management

2.2 Observable / data-binding (parallel; benefits all UI)

2.6 Map overlay / info-view (parallel; spatial complement)

UiTheme / ui-design-system tokens (palette, type scale, spacing, surfaces) ──> consistent dashboard chrome
```

**Conclusion:** Critical path for **chart dashboards**: **history → aggregation → charts → layout**. **Binding** and **info views** can proceed in parallel once ownership and backlog items exist.

---

## 4. Open threads (dashboard program)

1. **Spike issue:** Evaluate **XCharts** (or alternative) under repo **LICENSE** / maintenance constraints before a full dashboard **FEAT-**.
2. **UI stack:** Long term, compare staying on **uGUI** vs a phased **UI Toolkit** path (see Unity’s Timberborn case study: migration reduced merge pain for complex runtime UI—not a mandate for this repo).
3. **Info views:** Split **§2.6** into a dedicated backlog row when product priority is set; link to **isometric-geography-system** / **grid** performance notes.
4. **Chart-friendly tokens:** Add **Target** / **as-built** chart-oriented fields (e.g. faint grid line color) in **`ui-design-system.md`** / **`UiTheme`** when **FEAT-51** scopes them so dashboards do not invent a second palette.

---

## 5. References (external)

- [How Timberborn’s complex runtime UI was built](https://unity.com/case-study/timberborn) — UI Toolkit migration, runtime complexity, localization/debugging.
- Unity Manual: [Comparison of UI systems](https://docs.unity3d.com/Manual/UI-system-compare.html), [Data binding](https://docs.unity3d.com/6000.0/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/data-binding.html) (UI Toolkit).
- [XCharts](https://github.com/XCharts-Team/XCharts) — uGUI chart library (evaluate; not an endorsement until spike passes).

---

*Last updated: 2026-04-11 — exploration charter for **FEAT-51**; rename from legacy filename; polish delivery trace in [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md).*
