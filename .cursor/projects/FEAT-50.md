# FEAT-50 — UI visual polish: aesthetic refinement (HUD, panels, toolbar, MainMenu)

> **Issue:** [FEAT-50](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-04
> **Last updated:** 2026-04-06

<!--
  Structure guide: ../projects/PROJECT-SPEC-STRUCTURE.md
  Use glossary terms: ../../.cursor/specs/glossary.md (spec wins if glossary differs).
-->

## 1. Summary

Deliver a coordinated **player**-visible **UI** aesthetic upgrade across **`MainMenu`**, **city** **HUD**, **ControlPanel** / **toolbar**, and shared **panels**—grounded in **ui-design-system** **Foundations** and the committed **as-built** inventory JSON. This issue **uses** the shipped **UI-as-code** baseline (**glossary** **UI-as-code program**): **`UiTheme`** (**`ui-design-system.md`** **§5.2**), prefab **v0** under `Assets/UI/Prefabs/`, and **`UIManager`** **partial** surfaces—it optimizes look-and-feel (palette, type rhythm, spacing, iconography, contrast, optional motion) **without** reopening the archived program umbrella (optional **MCP** **`ui_theme_tokens`**, extra **Editor** automation) unless this issue explicitly extends scope.

**Product direction, ranked polish objectives, benchmarks, and opportunities** are recorded in **this spec** (§5–§7). **Future data-dashboard mechanics** (time-series, charts, layout system, info views) live in [`docs/ui-data-dashboard-exploration-FEAT-50.md`](../../docs/ui-data-dashboard-exploration-FEAT-50.md)—out of scope for **FEAT-50** delivery but dependent on the same **token** choices for visual coherence.

## 2. Goals and Non-Goals

### 2.1 Goals

1. **Coherent visual language** across primary surfaces (**MainMenu**, **MainScene** **Canvas** roots) so **HUD** stats, **toolbar** chrome, and **popup**/**panel** chrome feel like one product.
2. **Readable hierarchy**: primary vs muted **text**, **surface** contrast, and touch/click targets that remain comfortable at the **Canvas** scaler targets already used in scenes.
3. **Spec traceability**: where polish changes **color** or **typography** norms, update **ui-design-system** **as-built** tables and refresh **`docs/reports/ui-inventory-as-built-baseline.json`** (re-run **Export UI Inventory** when that workflow is available).
4. **Accessibility-minded defaults**: sufficient contrast for core **HUD** readouts and **toolbar** labels on representative backgrounds (record exact ratios in **Decision Log** or **ui-design-system** **Target** notes).
5. **Ship the ranked polish objectives** in §7.1 (checklist) in priority order unless **Decision Log** defers an item.

### 2.2 Non-Goals (Out of Scope)

1. **Simulation**, **grid**, **road**, **water**, or **Save data** behavior changes.
2. Re-scoping the completed **UI-as-code program** (**glossary**): no requirement to add **MCP** **`ui_theme_tokens`**, new **Editor** scaffolds, or further **`UIManager`** splits unless a follow-up **TECH-**/**FEAT-** row says so. **FEAT-50** may still **extend** **`UiTheme.cs`** fields and **`DefaultUiTheme.asset`** values—that is polish data, not new program capstone work.
3. Full **TextMeshPro** migration of the **city** **HUD** (per **`ui-design-system.md`** **§1.2**—legacy **`UnityEngine.UI.Text`** remains unless a future issue scopes **TMP**).
4. New **gameplay** **modal** flows or input routing changes—coordinate with **BUG-19** instead of solving scroll vs **camera** here. (A **welcome**/**briefing** shell may reuse existing **modal** patterns; new flows stay product-approved.)
5. **Data dashboards**, time-series history, chart engines, and **map** **info-view** overlays—see [`docs/ui-data-dashboard-exploration-FEAT-50.md`](../../docs/ui-data-dashboard-exploration-FEAT-50.md).

### 2.3 Related exploration

| Document | Role |
|----------|------|
| [`docs/ui-data-dashboard-exploration-FEAT-50.md`](../../docs/ui-data-dashboard-exploration-FEAT-50.md) | Prerequisite subsystems for future in-game dashboards (charts, layout, history, binding, info views). |

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|----------------------|
| 1 | Player | I want the **HUD** and menus to look intentional and easy to scan so that long sessions feel less fatiguing. | Core stat **panels** and **MainMenu** show updated styling per **§8**; no loss of legibility on reference resolution. |
| 2 | Developer / agent | I want **ui-design-system** and the baseline JSON to reflect shipped **UI** tokens so automated inventory and specs stay trustworthy. | **§8** includes doc/JSON updates when **Graphic.color** / font sizes shift materially. |

## 4. Current State

### 4.1 Domain behavior

**As-built** **color** and **typography** frequencies are summarized in **ui-design-system** **§1** and sourced from **`docs/reports/ui-inventory-as-built-baseline.json`**. **`UiTheme`** and prefab **v0** are **shipped** (**§5.2**); **FEAT-50** layers visual refinement on those assets or on scene-authored widgets, depending on schedule (**Open Questions**).

### 4.2 Systems map

| Area | Pointers |
|------|----------|
| Reference spec | `.cursor/specs/ui-design-system.md` (**§1** **Foundations**, **§2** components, **§3** patterns, **§4.3** **Canvas**, **§5.2** theme paths) |
| Program charter | **glossary** **UI-as-code program**; **glossary** **UI design system (reference spec)** |
| Structural UI baseline | **`ui-design-system.md`** **§5.2** (`UiTheme.cs`, **`DefaultUiTheme.asset`**, prefab **v0**, **`UIManager.*.cs`**) |
| Unity / Editor | `.cursor/specs/unity-development-context.md` **§10** (**Export UI Inventory**, **Validate UI Theme** and related **Editor** menus). **Canvas Scaler** matrix: **`ui-design-system.md`** **§4.3**. |
| Critique trace | `docs/ui-as-built-ui-critique.md` |
| Future dashboards (out of scope) | `docs/ui-data-dashboard-exploration-FEAT-50.md` |

### 4.3 Implementation investigation notes (optional)

Prefer **Prefab**-level styling and shared materials/sprites over per-scene one-offs. Prefer binding polish constants to **`UiTheme`** where practical to avoid duplicate sources of truth.

### 4.4 Invariants and dependency preflight

- **System invariants:** no new **singletons**; no **`FindObjectOfType`** in **`Update`** or per-frame **UI** paths (**BUG-14**). **FEAT-50** does not touch **HeightMap**, **roads**, **water**, or **Save data**.
- **Backlog:** **Depends on:** none. **Soft:** **BUG-19** (scroll over **popup** lists vs **camera** zoom)—verify when changing scrollable **panels** or **modal** chrome (**`ui-design-system.md`** **§3.5**).

## 5. Proposed Design

### 5.1 Target behavior (product)

**Player**-visible **UI** adopts an agreed mood: calm, readable, slightly immersive (semi-transparent **panels** over the **map** where safe), restrained accents, consistent **surface** tiers, and optional short motion that does not block input. Exact **RGBA** / px values ship as **Target** rows in **ui-design-system**, then **as-built**.

### 5.2 Visual benchmarks (informative)

These references informed §7.1; they are **not** commitments to copy art style.

| Reference | Patterns to borrow |
|-----------|-------------------|
| Dark industrial / Grafana-style dashboards | Card grid, semantic green/amber/red, KPI **title → visual → number**, sparkline-friendly density |
| Timberborn-style welcome modal | Full-bleed background art, translucent dark card, single primary **CTA**, generous padding |
| Analytics-style KPI cards | **Hero number** for top stats (population, treasury), small secondary label, optional trend hint |
| SimCity-style budget table | Dense rows, semantic red/green numbers, summary block separated by rule |
| Sidebar / master-detail games | **Toolbar** grouping, optional future panel stack—**toolbar** grouping applies in §7.1 |

### 5.3 Opportunities (as-built vs target)

Grounded in **`ui-as-built-ui-critique.md`** and **ui-design-system** **as-built**:

- **Colors:** Move from emergent **Inspector** tints to **`UiTheme`** fields (**surface** tiers, **border**, **text** primary/secondary, **accent** primary, positive, negative).
- **Typography:** Even with legacy **`Text`**, enforce a **four-step scale** (display / heading / body / caption) via **`UiTheme`** sizes—stops mixed 10/12/14/36 drift.
- **Spacing:** Introduce a base **spacing unit** (e.g. 4 px) for padding, row height, and **toolbar** gaps.
- **Components:** Prefer prefab **v0** patterns (**stat row**, **modal** shell) where they exist; add thin dividers, optional **progress** bars, and semantic number formatting as small shared utilities or prefabs.
- **Modals:** Align dimmer, fade timing, and optional **Esc** with the **modal** pattern in **ui-design-system** **§3.2** / critique **P7**—coordinate **BUG-19** for scroll vs **camera**.

### 5.4 Architecture / implementation (agent-owned unless fixed by design)

Implementation order follows §7. **Respect invariants:** no new **singletons**; no **`FindObjectOfType`** in per-frame **UI** paths (**BUG-14**). Prefer **CanvasGroup** tweens over heavy **Animator** on **HUD** stats.

### 5.5 Method / algorithm notes (optional)

None required at kickoff.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-04 | Issue id **FEAT-50** | Next free **FEAT-** id after **FEAT-48** and reserved **FEAT-49** spec file. | **ART-** prefix (rejected: scope spans **UX** + spec, not only art assets). |
| 2026-04-05 | Consolidate exploration into this spec; split dashboard mechanics to `docs/ui-data-dashboard-exploration-FEAT-50.md` | Single normative place for **FEAT-50** polish; dashboard prerequisites stay discoverable without overloading polish scope. | Keep monolithic exploration doc (rejected: harder to maintain and to link from **BACKLOG**). |
| 2026-04-06 | **Project spec kickoff** — aligned vocabulary with **glossary** **UI-as-code program** / **UI design system (reference spec)**; concrete **§7** file paths; **§7b** mapped to **§8**; **§4.4** invariants/**BUG-19** preflight | **territory-ia** `backlog_issue` + `invariants_summary` + `router_for_task` (UI) + `glossary_*`; clarifies polish **uses** shipped **§5.2** assets without reopening archived umbrella scope. | — |

## 7. Implementation Plan

Phases map to **§7.1** checklist items. Primary **write** surfaces: **`Assets/Scenes/MainScene.unity`** (**`UI/City/Canvas`**, **`ControlPanel`**, **`DataPanelButtons`**), **`Assets/Scenes/MainMenu.unity`** / **`MainMenuCanvas`**, **`Assets/UI/Theme/DefaultUiTheme.asset`**, **`Assets/Scripts/Managers/GameManagers/UiTheme.cs`**, prefab **v0** under **`Assets/UI/Prefabs/`**, and controllers only when wiring demands it (**`UIManager.*.cs`**, **`UnitControllers/`**, **`MainMenuController`**).

### Phase 1 — Direction lock

- [ ] Resolve **Open Questions** enough to proceed: mood, **UiTheme**-first vs scene-first, default **modal** motion stance; record in **Decision Log**.
- [ ] Mark **§7.1** items explicitly **deferred** (if any) in **Decision Log** so scope stays bounded.

### Phase 2 — Token foundation (**§7.1** items 1, 3, 4, 6)

- [ ] Add or adjust **`UiTheme`** **Color** / **`fontSize*`** fields in **`UiTheme.cs`** and serialize values on **`DefaultUiTheme.asset`**; wire **`MainMenuController.menuTheme`** when that path is used (**`ui-design-system.md`** **§5.2**).
- [ ] Apply **surface** / **text** / **accent** tokens to **MainScene** **HUD** and **toolbar** roots and **MainMenu** roots (prefer **Prefab** instances over one-off **Inspector** drift).
- [ ] Run **Territory Developer → Reports → Validate UI Theme asset** after theme edits.
- [ ] Spot-check **Canvas Scaler** at **800×600** and **1920×1080** per **`ui-design-system.md`** **§4.3**.

### Phase 3 — **HUD** + **MainMenu** readability (**§7.1** items 2, 11, 12)

- [ ] Key-value **stat** rows (**StatisticsManager** / **EconomyManager** readouts): label/value hierarchy, semantic **Text.color** or helper.
- [ ] **Tax** / **budget** **popup** rows and totals: dividers, alignment, **accent-positive** / **accent-negative** amounts.

### Phase 4 — **Toolbar** + secondary **panels** (**§7.1** items 8, 9)

- [ ] **ControlPanel** grouping and active-tool chrome (**`ui-design-system.md`** **§3.3**).
- [ ] **Demand** / coverage **bars** where **managers** already expose values—**presentation only** (**Non-Goals**).

### Phase 5 — **Modals** + motion + **tooltips** (**§7.1** items 5, 7, 10)

- [ ] Optional welcome/briefing shell (**§7.1** item 5)—only if product approves and existing **PopupType** / stack allows without new **gameplay** flows (**Non-Goals** §2.2 item 4).
- [ ] **CanvasGroup** open/close on **popup** roots; keep **§3.2** **Esc** / stack behavior intact.
- [ ] **Tooltip** prefab or structured replacement for **`UIManager`** float—no per-frame **`FindObjectOfType`**.
- [ ] **Regression:** pointer over scrollable **popups** + **camera** zoom (**BUG-19** class, **`ui-design-system.md`** **§3.5**).

### Phase 6 — Documentation + baseline

- [ ] Update **`ui-design-system.md`** **§1** **as-built** (and **Target** where used) for shipped **RGBA** / **typography** norms.
- [ ] Regenerate **`docs/reports/ui-inventory-as-built-baseline.json`** when **Export UI Inventory** is run and **Graphic.color** / font sizes changed materially.

### 7.1 Polish objectives (ranked checklist)

Order reflects estimated **value / impact** (player-visible benefit vs effort in **`UiTheme`** + **uGUI**). Check off when shipped; defer via **Decision Log** if scope conflicts with **Non-Goals**.

- [ ] **1. Unified surface hierarchy + dark palette (HIGH)** — Add **`UiTheme`** tokens (example targets): `surface-base` `#111318`, `surface-card` `#1c1f26`, `surface-elevated` `#282c35`, `border-subtle` `#2e3340`, `accent-primary` `#4a9eff`, `accent-positive` `#34c759`, `accent-negative` `#ff453a`, `text-primary` `#e8eaf0`, `text-secondary` `#8b8fa4`. Apply across **MainMenu**, **HUD** cards, **toolbar**, **popup** roots.
- [ ] **2. Key-value stat display + emphasis (HIGH)** — Muted **caption** labels; larger right-aligned values; optional small delta (e.g. `+12`) for growth stats. **Budget**/**tax** rows: label left, amount right; stronger **totals** row with divider (ties to item 12).
- [ ] **3. Semi-transparent panel overlays (HIGH)** — **HUD** panel backgrounds ~88–92% alpha; **toolbar** slightly more opaque (~94%) for icon contrast; **modal** dimmer e.g. `#000000aa` (~67% alpha). Validate readability on **map** behind **Canvas**.
- [ ] **4. Type scale — four steps (HIGH)** — `display` 28–32 px (hero numbers), `heading` 18–20 px, `body` 14 px, `caption` 11–12 px; store as **`UiTheme`** fields (`fontSizeDisplay`, etc.). Stay on legacy **`Text`** per **Non-Goals** unless a separate issue migrates **TMP**.
- [ ] **5. Welcome / briefing modal shell (MEDIUM-HIGH)** — Centered card, **RawImage** or art background, generous padding, single **CTA**, dimmer dismiss; reuse existing **popup**/**modal** wiring. **Product** must own copy and whether this ships in **FEAT-50** or a follow-up.
- [ ] **6. Spacing rhythm (MEDIUM-HIGH)** — Base unit 4 px; panel padding 16 px; stat row height ~36 px; **toolbar** buttons ~40×40 with 8 px gaps and 16 px between groups; divider margins 12 px vertical.
- [ ] **7. Modal transitions (MEDIUM)** — **CanvasGroup** fade ~180 ms open / ~120 ms close; optional 20 px slide; cap ≤250 ms; avoid tween during heavy **simulation** spikes (defer frame if needed).
- [ ] **8. Progress / gauge bars (MEDIUM)** — Horizontal bars for **zone demand**, budget health vs runway, service coverage in detail panels—**visual only**, data from existing managers; no new **simulation** rules.
- [ ] **9. Toolbar grouping + active tool state (MEDIUM)** — Vertical dividers or gaps between tool clusters; **accent** bottom border + **elevated** surface for active tool; hover state ~100 ms; disabled tools ~40% alpha when contextually unavailable.
- [ ] **10. Tooltip upgrade (LOW-MEDIUM)** — Prefab tooltip: **`surface-elevated`**, **caption** text, max-width ~240 px, 400 ms show delay, 100 ms fade; flip to avoid screen edges; content = short stat gloss + shortcut where applicable.
- [ ] **11. Semantic number formatting (LOW-MEDIUM)** — Positive **accent-positive** (optional `+`), negative **accent-negative** with `-`, zero/neutral **text-primary**; centralize e.g. `UiFormatHelper` to avoid duplicated color logic.
- [ ] **12. Panel dividers (LOW)** — 1 px **`border-subtle`** rules between **HUD** sections and above **budget**/**tax** totals.

### 7.2 Conclusions

- **Highest leverage** is **tokens** (item 1) + **type scale** (item 4) + **transparency** (item 3): they unify every surface before fine-tuning individual widgets.
- **Stat readability** (items 2, 11, 12) is the next win for time-in-**city** play.
- **Toolbar** (item 9) reduces misclicks and sells the “designed product” feel.
- **Motion** and **tooltips** (items 7, 10) are polish multipliers with medium implementation cost—schedule after core tokens.
- **Welcome modal** (item 5) is valuable for first-run but touches **modal** policy—explicit **product** sign-off and **BUG-19** check.
- Future **chart** UIs should consume the same tokens; see [`docs/ui-data-dashboard-exploration-FEAT-50.md`](../../docs/ui-data-dashboard-exploration-FEAT-50.md).

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| **§8** — **Player**-visible aesthetic (**§5** + shipped **§7.1**) | Manual / Play Mode | Unity: **MainMenu** → **MainScene** | Exercise **HUD** (**`DataPanelButtons`** clusters), **ControlPanel**, representative **PopupType** (e.g. **LoadGame**, **Tax**, **BuildingSelector**) at **800×600** and **1920×1080** per **`ui-design-system.md`** **§4.3** |
| **§8** — No **simulation** / **grid** / **Save** regression | Manual / Play Mode | Unity smoke | **Grid** tool selection, one **Save**/**Load** cycle if touched scenes serialize **UI** refs; **AUTO** tick unchanged |
| **§8** — **ui-design-system** + baseline JSON match shipped tokens | Manual + Editor | Edit **`.cursor/specs/ui-design-system.md`** **§1**; **Export UI Inventory** → refresh **`docs/reports/ui-inventory-as-built-baseline.json`** when norms shift | Run **Territory Developer → Reports → Validate UI Theme asset** after **`DefaultUiTheme.asset`** / **`UiTheme.cs`** changes |
| **§8** — **BUG-19** / **camera** vs scroll | Manual / Play Mode | Unity | With **popup** open, pointer over **ScrollRect**: **camera** zoom / pan does not fire through (**`ui-design-system.md`** **§3.5** checklist) |
| **Spec:** links and **Spec:** paths valid | Node | `npm run validate:dead-project-specs` (repo root) | After **BACKLOG** / **Spec** / durable doc link edits |
| **IA** index drift | Node | `npm run validate:all` (repo root) | Only if **glossary** or **reference spec** bodies that feed IA indexes change (**AGENTS.md** checklist) |

## 8. Acceptance Criteria

- [ ] **Player**-visible **UI** on **`MainMenu`** and **`MainScene`** reflects **§5** and the shipped subset of **§7.1** (see **§7b** first row).
- [ ] No regressions to **simulation**, **grid** interaction, or **Save**/**Load** attributable to this work.
- [ ] **ui-design-system** and (when run) **`docs/reports/ui-inventory-as-built-baseline.json`** match shipped **color**/**typography** for changed widgets.
- [ ] **BUG-19** / **camera** vs scroll behavior unchanged or explicitly coordinated (no accidental **modal** scroll capture).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

*This issue is **UI**/**UX** presentation and **schedule**—not **simulation** rules. Use **glossary** and **`ui-design-system.md`** terms below.*

1. Should polish ship **after** binding to **`UiTheme`** / prefab **v0**, or start on the current hierarchy and re-apply once tokens stabilize?
2. What minimum **Canvas** / safe-area assumptions must the polished **HUD** respect? (**Normative baseline:** **`ui-design-system.md`** **§4.3** **Canvas Scaler**—**city** **800×600** reference vs **MainMenu** path.)
3. For **modal**-heavy surfaces, should motion (**CanvasGroup**, transitions) be **on**, **minimal**, or **off** by default for performance and clarity?
4. If localized strings lengthen, does the **HUD** layout still hold at the new **typography** scale?
5. Should **minimap** and secondary **panels** use the same **chrome** as **ControlPanel** (**`ui-design-system.md`** **§3.3**), or a subdued variant?
6. Should the welcome/**briefing** modal ship in **FEAT-50** or in a follow-up **FEAT-** once the shared **modal** pattern (**`ui-design-system.md`** **§3.2**; critique **P7**) is stable?

## Review workflow (informative)

1. **Mood:** 3–5 adjectives + 1–2 anti-goals (e.g. not neon, not heavy skeuomorphism).
2. **Optional mock:** screenshot markup or experimental **Canvas** branch.
3. **Token sheet:** final **RGBA** and px sizes for **Decision Log** / **ui-design-system** **Target**.
4. **Rollout:** **MainMenu**-first vs **HUD**-first—team choice; default order is §7.0.
5. **Sign-off:** product owner approves **Target** or **Decision Log** before wide **Prefab** merge.
