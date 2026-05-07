# MVP Scope — Territory Developer (first release)

> **Role.** Definitive scope of what ships in the first release of Territory Developer. Single source of truth for "what the player can do." Derives the UI element catalog (panels, buttons, readouts) — every UI element must trace back to a feature listed here.
>
> **Status.** **LOCKED 2026-05-07** — D1–D36 polled + folded in. Source: `docs/full-game-mvp-exploration.md` (Framing F) + `docs/full-game-mvp-rollout-tracker.md` + DB master plan `full-game-mvp` (umbrella). MainScene→CityScene rename DONE 2026-05-07 (§7). Next workstreams: UI panel grilling against this locked scope (§6 inventory).
>
> **Frame.** Polished Ambitious MVP. Twenty to fifty curated dev-savvy testers. macOS + Windows native. English only. Mouse + keyboard.

---

## 1. Game premise

Single-player city builder on an isometric 2D grid. Player paints zones, lays roads, manages budget + utilities + services, watches the city grow + react. Three nested simulation scales — **City** (the grilled scale), **Region** (group of cities), **Country** (group of regions). One country playable at MVP; international hooks visible but not playable until post-MVP.

Tester goal: experience a polished slice deep enough to feel like SimCity / Cities-Skylines lineage, narrow enough to ship in one umbrella plan.

---

## 2. The two scenes + three simulation scales

MVP ships **two scenes** — `CityScene` + `RegionScene`. There is **no CountryScene**. Country logic is simulated from RegionScene the same way Region is depicted from CityScene — by what shows at the borders + economic signals propagating in / out.

| Scale | Owning scene | What the player sees | What is simulated |
| --- | --- | --- | --- |
| **City** | `CityScene` | Streets + zones + buildings + services. Day-to-day moment-to-moment play. **Region depicted at map borders** — neighbour cities visible on edges + econ signals (trade flow / migration / utility transfers) shown via border indicators. | Full city sim. Region influence = read-only signals from neighbours. |
| **Region** | `RegionScene` | Map of cities + inter-regional roads + region-wide aggregates. **Country depicted at borders** — neighbour regions visible on edges + national econ signals (national budget transfer / utility pool draw / international hooks) shown via border indicators. | Full region sim. Country influence = read-only signals from country layer. |
| **Country** | (No scene — simulated layer) | Implicit. Player never enters a Country scene. Country state surfaces in RegionScene as edge-of-map signals + a country-stats panel. | Background sim — utility pool, national budget, bonds, international hooks. Computed from region aggregates. |

**Current grilling target:** CityScene only. Panels, buttons, layouts derived from City features below + the **City→Region border surface** (how neighbour cities + region econ signals show at the edges). Primitives carry forward to RegionScene grilling later.

### 2.1 City ↔ Region signal contract (LOCKED)

All four signal groups IN MVP. Visual depiction at City borders TBD at Bucket 1 author time — contract locked here so simulation + save schema can plan slots.

| Group | Signals | Direction | UI surface (City) |
| --- | --- | --- | --- |
| **Money flows** | Inter-city trade revenue, regional tax transfer, regional deficit transfer | Both | Border revenue indicator, tax-transfer line in budget panel |
| **People flows** | Migration in/out, commuter influx | Both | Border migration indicator, demographics panel migration row |
| **Resource flows** | Regional utility pool draw (water/power/sewage), pollution upscale to region | Both | Utility readout shows pool share, pollution overlay shows leakage to region |
| **Physical flows** | Inter-regional roads (entry/exit nodes at map edge), service spillover (police/fire crossing borders) | Both | Border road stubs visible, service overlay extends to neighbour edge |

### 2.2 Region ↔ Country signal contract (LOCKED — partial)

Two of four signal groups IN MVP. International hooks + big-projects gating DROPPED — moved to OUT list.

| Group | Status | Notes |
| --- | --- | --- |
| **National budget + bonds** | IN | Country-level tax pool, bond issuance, deficit-spending propagation. Surface in RegionScene budget panel. |
| **Country utility pool** | IN | National water/power/sewage aggregate. Region draws from pool; deficit drives Country-level utility-plant commission. |
| **International hooks (border-sign UX)** | OUT | No second-country surface. No customs / trade-deal UI. Removed from MVP. |
| **Big-projects gating (national-scale landmarks)** | OUT | Landmarks remain (City + Region scale). National-scale big-projects commission removed. |

---

## 3. City scale — IN scope

### 3.1 Zone painting (R / C / I / S)

Four parallel zone types — Residential, Commercial, Industrial, **State-owned (S)**. S is the new fourth zone for police / fire / education / healthcare / parks / public housing / public offices. **R + C + I follow 3-tier density evolution — light → medium → heavy (D16 lock).** Each tier = distinct sprite set + capacity / tax-yield / pollution profile. Cell evolves automatically based on demand pressure. **I splits into 3 specialisations — Agriculture / Manufacturing / Tech (D4 lock — Tourism dropped from MVP).**

Sprite footprint: 3 density tiers × (R + C + I-Agri + I-Manuf + I-Tech) = ~15 sprite families minimum for R/C/I.

UI need: zone palette with 4 channels, density visible per cell, 3-option I-specialisation picker. **D22 / D23 lock — all tools live in the existing left-side vertical toolbar panel + a single shared subtype-picker panel. See §3.31 (Toolbar + subtype-picker contract). Build-residential + build-commercial buttons DROPPED from HUD.**

### 3.2 Roads (D11 lock — no transit, no pedestrians)

Multi-tier road hierarchy — street / avenue / arterial / interstate. Road preparation family stays unchanged (see invariants). Bridges over water / cliffs already wired. **Cars are the only mode of movement.** No public transport. No pedestrian sim.

UI need: road tier picker, road / interstate toggle. No transit placement entry-points.

### 3.3 Utilities v1 — D15 lock

Water + power + sewage as **country-level pools** fed by per-building contributors. City consumers pull from regional / country pool. Utility deficit → happiness penalty.

**Each utility ships 2 plant variants — dirty + clean tier.**

| Utility | Dirty (cheap, high pollution) | Clean (expensive, low pollution) |
| --- | --- | --- |
| Power | Coal plant | Solar / wind plant |
| Water | Reservoir | Desalination plant |
| Sewage | Basic sewage plant | Treated sewage plant |

**6 plant building families total.** Drives a clear tax / pollution tradeoff for the player.

UI need: utility readout (pool level per utility), utility-building placement (6 entries), deficit notification surface.

### 3.4 Waste

Collection + disposal as a service coverage signal. Tied to pollution.

UI need: waste service overlay, waste-building placement.

### 3.5 City services (S consumers) — D9 lock: 7 sub-types

S-zone splits into **7 sub-types**:

| Sub-type | Behaviour | Coverage radius? |
| --- | --- | --- |
| Police | Crime suppression | Yes |
| Fire | Fire-state suppression | Yes |
| Education | Workforce skill-up + happiness | Yes |
| Healthcare | Happiness + crime down | Yes |
| Parks | Leisure + desirability + pollution sink | Yes |
| **Public Housing** | State-built R alternative — low-income capacity boost when private R lags | No (capacity-based, not coverage-based) |
| **Public Offices** | Administration capacity — unlocks budget knobs, raises tax tolerance, reduces corruption proxy | No (city-scale effect, not per-cell) |

**5 overlay-driving services** (police/fire/edu/health/parks). **2 capacity-driving services** (public-housing + public-offices) surface in budget + demographics panels, NOT in overlay stack.

UI need: 7-option S-zone palette, 5 service overlays on minimap (per D6), service-building info panel.

### 3.6 Pollution (3-type)

Air / land / water. Sourced by I + roads + utilities. Sinks = forests, parks. Drives desirability + happiness.

UI need: 3 pollution overlays (one per type), pollution readout.

### 3.7 Crime

Per-cell crime score. Driven by low service coverage + high unemployment. Fed back into desirability.

UI need: crime overlay, crime readout.

### 3.8 Traffic flow abstraction

Road density heuristic — low / medium / high / jammed. Drives anim swap on road strokes (no per-vehicle pathing).

UI need: traffic overlay, traffic readout.

### 3.9 Construction evolution

Buildings construct visibly (placeholder → frame → finished) instead of pop-into-existence. Anim per zone type × density.

UI need: none direct (visual only). Indirect: info panel may show "under construction" state.

### 3.10 Districts — DROPPED (D8)

**Districts entirely removed from MVP.** No paint tool, no naming, no per-district stats, no per-district policy. Player works at city scale only.

### 3.11 Urban growth rings

Centroid-driven growth pressure. Already partly implemented (FEAT-43). Tunes density spawn weights.

UI need: growth-rings overlay (debug-grade), centroid marker.

### 3.12 Industrial specialisation

I splits into **Agriculture / Manufacturing / Tech** (D4 lock — 3 subtypes; Tourism dropped). Each carries different pollution profile + tax curve + sprite archetype + unlock condition.

UI need: 3-option I-specialisation picker on zone paint, info panel shows I subtype.

### 3.13 Landmarks — D17 lock: 4 total (2 City + 2 Region)

Scale-unlock rewards. **2 at City scale + 2 at Region scale = 4 landmarks total in MVP.** National-scale big-projects gating dropped (D3).

Each landmark = unique sprite + unlock condition (population / budget / service / time threshold) + visible reward (happiness boost, tax bonus, capacity raise). Specific landmark identities + unlock thresholds locked at Bucket 2 / Bucket 3 author time.

UI need: landmark catalog (4 cards, locked / unlocked state), landmark info panel.

### 3.14 Forests + parks — D32 lock

Forests = pollution sink + visual. Parks = service coverage (leisure) + happiness.

**Forests (D32):** dedicated toolbar tool (independent row). Subtype-picker shows 3 density cards: **sparse / medium / dense**. Picker also exposes **2 placement-mode buttons inside the panel**: **single-cell** (click one cell at a time) + **spray** (drag-paint area, density determines tree count per cell). Mode persists across paint actions until tool deactivated.

**Parks:** stay inside S-zone (Service tool, D9 — `parks` subtype). Coverage-service style placement (single-cell click). No drag-paint for parks.

UI need: Forest tool slot in toolbar; subtype-picker variant that hosts 3 density cards + 2 mode-toggle buttons (first picker variant with secondary controls — extends the standard cards-only picker layout per §3.31).

### 3.15 Budget + finance — D24 lock

Tax sliders R / C / I (and S maintenance). Per-service budget allocation. Deficit spending. Bonds. Monthly maintenance covers roads + utilities + S.

**D24 lock — Budget readout (current balance + monthly delta) is ALWAYS-ON in the HUD strip.** The full budget editor (tax sliders + per-service allocation + bonds + deficit detail) opens as a dedicated `budget-panel` when the HUD readout is clicked. Budget is NOT a stats-panel tab.

UI need: HUD always-on budget readout (clickable), `budget-panel` (tax sliders + allocation + bonds + deficit), deficit notification toast.

### 3.16 Time controls — D5 + D33 lock

**D5 lock (corrected at D7) — 2 buttons total, no long-press.**

- **Play / Pause** — single button, toggles based on state (music-player style). Icon flips between play-glyph and pause-glyph.
- **Speed cycle** — single-click advances 1 → 2 → 3 → 4 → 1. Speed shown as 1–4 arrow glyphs on the button. **Max speed = 4x. The 5x speed step is DROPPED from MVP.**

**D33 lock — game-time mapping = current code (`TimeManager.cs`):**
- **1x = 1 real-second per game-day.** Day advances when `timeElapsed >= 1f` at multiplier 1.0.
- Speed multipliers: 1x = 1.0, 2x = 2.0, 3x = TBD (not in current `timeSpeeds = {0, 0.5, 1.0, 2.0, 4.0}` array — code currently skips 3x; spec D5 implies 1/2/3/4 cycle, code has 0.5/1/2/4). **Drift: spec calls 4-speed integer cycle; code has 4 play-speeds with 0.5x + 4x but no 3x. Fix in code = drop 0.5x + add 3x to match spec.**
- Game day-tick fires `cityStats.PerformDailyUpdates`, `simulationManager.ProcessSimulationTick`.
- Game month-tick (game-day == 1) fires `cityStats.PerformMonthlyUpdates`, `economyManager.ProcessDailyEconomy`, `simulationManager.ProcessMonthlyReset`.
- Start date hardcoded `2024-08-27` (`TimeManager.cs:50`). Should be parameterized in new-game-form post-MVP.

**D33 lock — budget close cadence = monthly.** Tax revenue collected, expenses subtracted, balance updated once per game-month (already wired via `economyManager.ProcessDailyEconomy` on Day == 1). Single notification toast emitted at month-close (deficit warning + revenue summary). Daily budget movement = none (HUD readout updates only on monthly close).

UI need: 2-button time cluster, HUD date readout (current date display — covered by §3.32 city-name cell or new readout TBD). No onboarding teaching needed (interactions are conventional). **Code task: reconcile `timeSpeeds` array with spec 1/2/3/4 cycle (drop 0.5x, add 3x).**

### 3.17 Camera controls — D21 lock: 3 buttons

**3 buttons in HUD: zoom-in / zoom-out / recenter.** Mouse wheel zoom + mouse drag pan also wired in parallel (redundant but discoverable). No rotate button — isometric view is fixed-angle.

UI need: camera cluster (3 buttons), wheel + drag bindings.

### 3.18 Map / minimap — D26 lock

**Minimap is hidden by default.** HUD strip has a `map` button. Click → minimap opens as a floating panel; click again → closes. Per D6, minimap is the canvas for all 13 overlay layers (pollution / services / crime / traffic / etc.). **Overlay-toggle stack lives inside the minimap panel** (no separate overlay-toggles surface).

UI need: HUD `map` toggle button, floating minimap panel (with embedded overlay toggle group, ~13 toggles, additive stack).

### 3.19 Notifications — D19 + D27 + D34 + D36 lock

**Toast-only surface.** Event-feed-panel DROPPED from MVP per D36 (2026-05-07).

- **Toast stream — top-right corner stack** (under hud-bar, growing downward, 320 px wide cards, ~4–8 s auto-dismiss per tier; milestone tier sticks until clicked). Final geometry locked in `docs/ui-element-definitions.md` § `notifications-toast`.
- **Event-feed-panel — DROPPED.** No persistent scrollable history surface in MVP. Toasts are transient-only. No HUD bell-icon button. No unread-count badge.

**D34 lock — event sources (MVP, scoped to toast tiers):**

| Source | Trigger | Toast tier | Examples |
| --- | --- | --- | --- |
| City milestones (NEW per D36) | Population threshold cross (1k / 5k / 10k / 25k / 50k / 100k) | Milestone (sticky) | "Population reached 10 000" |
| Service-driven (D9) | Coverage drop below 40 % (debounced 30 in-game days per service) | Warning | Fire / Crime / Edu / Health gaps |
| Utility-driven (D15) | Demand > supply (deferred — wired through service-coverage thresholds in MVP) | Warning | Brownout / Water shortage / Sewage overflow |
| Budget (D24) | Insufficient funds via `TreasuryFloorClampService` | Error | "Cannot afford this build" |
| Pre-existing | (already wired) | Info / Success | Build commit · Save · Validation failures |

**Out of MVP:** Random natural events (earthquake / flood / wildfire) DROPPED (D34). Economic shocks (recession / boom / I-tech-shift) DROPPED (D34). Persistent event-feed history DROPPED (D36). All deferred to post-MVP.

UI need: top-right toast stream only; no event-feed-panel; no HUD bell button; no unread badge.

### 3.20 Info panel — D20 lock: one unified surface

**One adaptive info panel.** Player clicks anything (empty cell / R-C-I cell / road / S building / utility plant / landmark) → same panel slides in, content adapts to target kind. Content templates per target kind, rendered in the same panel chrome.

| Click target | Panel content |
| --- | --- |
| Empty cell | Coords, terrain, eligibility flags (zoneable / road-buildable / forested) |
| R / C / I cell | Zone + density + happiness + pollution + service coverage + tax yield + I subtype if I |
| Road segment | Tier + traffic load + connection status |
| S building | Sub-type + coverage radius + staffed status + maintenance cost |
| Utility plant | Plant kind + output + pollution + maintenance cost |
| Landmark | Name + unlock condition + active reward |

Click another target → same panel re-renders with new content. Dismiss = click empty space or close button.

UI need: single info panel, content templates per target kind, dismiss interaction.

### 3.21 Overlays — minimap only (D6 lock + D26)

**Overlays render on the minimap, NOT on the main gridmap.** Full-gridmap color-tint overlays are OUT of MVP. The gridmap shows the city in its diegetic look (zones / roads / buildings / construction state) at all times.

Minimap overlays — pollution (×3), desirability, happiness, zone, service coverage (×5), crime, traffic flow, growth rings. **Additive toggle** — player stacks any combination on the minimap; no exclusivity enforcement. Legend reflects active stack.

**Per D26, overlay toggles live INSIDE the minimap panel** (no separate `overlay-toggles` panel — toggles are embedded in minimap chrome, only visible when minimap is open).

UI need: overlay toggle group embedded in minimap panel (~13 toggles, all independently toggleable).

### 3.22 Onboarding — DROPPED (D7)

**No onboarding ships in MVP.** Players discover the game on their own. Tester reactions become the source signal for post-MVP UX iteration. Tutorial overlay / step indicator / skip button all removed from scope.

### 3.23 Save / load + slots — D10 lock: unlimited

**Unlimited save slots.** Player names each save freely. List view of all saves with sort by date / name. Add / delete / rename inline. Save schema v3 (envelope owned by Bucket 3). Local-only.

UI need: save / load dialog with scroll list, sort controls, name input, save-as-new + overwrite + delete actions.

### 3.24 Settings menu — D14 lock: full set

**Three sections, ~9 controls total.**

- **Audio** — 5 sliders: Master / Music / SFX / Ambient / UI.
- **Display** — resolution dropdown + fullscreen toggle.
- **Game** — autosave interval (dropdown / slider) + default speed (1–4 picker).

UI need: settings panel with 3 grouped sections, sliders, dropdowns, save-on-change.

### 3.25 Pause menu — D25 lock: hub pattern

**Esc-key triggered. Pause-menu is the hub for in-game system actions.**

Buttons (top → bottom): Resume / Save / Load / Settings / Main menu / Quit.

- Save / Load / Settings open **as nested modals from pause-menu** (sub-modal stack — back arrow returns to pause-menu).
- Main menu / Quit prompt confirm dialog before exit.
- Sim is paused while pause-menu OR any nested modal is open.

Main-menu screen + new-game form are separate (pre-game flow, not nested under pause-menu).

UI need: pause-menu modal (6 buttons), save-modal + load-modal + settings-modal as nested children, back-arrow contract on each nested modal.

### 3.26 New-game setup — D18 + D30 lock

**Fields:** map size (dropdown), starting budget (slider or dropdown), city name (text input), seed (text input). **Difficulty DROPPED** — single tuning, no Easy/Normal/Hard split.

**D30 lock — pre-game flow:** Main-menu (title screen) → click New Game → `new-game-form` modal opens → fill fields + click Start → CityScene loads. Alternative branches from main-menu: Continue (resume most-recent save), Load (open `load-modal`), Settings (open `settings-modal`), Quit.

UI need: new-game form with 4 controls + start button. Reachable only from main-menu (NOT from in-game pause-menu — pause-menu's "Main menu" button returns to title to start a new game).

### 3.27 Graphs panel — D13 + D24 lock

**4 curves: Population / Happiness / Pollution / Budget.** Time axis = game time. **3-position range selector — last week / last month / all-time.** No pan, no zoom.

**D24 lock — Graphs lives as a tab inside the shared `stats-panel` (alongside Demographics + CityStats).**

UI need: graphs tab with 4-curve render, range selector (3 buttons), curve highlight on hover or click.

### 3.27a Demographics tab — D34 lock: full depth

**D34 lock — Demographics tab ships at full depth (3 charts + ~10 readouts).**

| Section | Content |
| --- | --- |
| Population breakdown | Total / employed / unemployed / school-age (4 numbers + 4 small bars) |
| Age pyramid | Histogram: children / working-age / retired (3-bin chart) |
| Education levels | 4-bin bar chart: none / primary / secondary / higher |
| Income tiers | 3-bin histogram: low / mid / high |
| Avg commute time | Single readout (game-minutes from R-cell to nearest workplace) |
| Housing distribution | R-density 3-bin histogram: light / medium / heavy |

Updated **monthly** (matches budget close cadence per D33).

UI need: Demographics tab as third tab in `stats-panel`. ~3 chart primitives (histogram + age-pyramid + bar-chart) + ~6 readout cells. Risk: high info density — consider collapsible sections if tab feels crowded at implementation time.

### 3.28 Tooltips — D28 + D36 lock

**Tooltip primitive only. Glossary-panel DROPPED (D36).**

- **Tooltip system** — hover any interactive UI element → tooltip after ~500ms delay. Tooltip shows term name + short definition. Tooltip primitive applies across HUD readouts, toolbar tools, palette cards, info-panel rows, settings labels.

**Out of MVP:** Dedicated glossary-panel + HUD `?` button DROPPED (D36). Discoverability handled by tooltips alone in MVP. Persistent term lookup deferred to post-MVP.

UI need: tooltip primitive (component) only. No HUD `?` button. No glossary-panel.

### 3.29 In-game CityStats dashboard (D12 lock)

**In-game stats dashboard SHIPS in MVP.** Drives the HUD readout cluster + demographics tab + minimap overlays + graphs tab. Designed standalone — no requirement to mirror the web dashboard contract. **Web-dashboard parity DEFERRED to post-MVP.**

**D24 lock — CityStats dashboard lives as a tab inside the shared `stats-panel` (alongside Graphs + Demographics).** HUD readout cluster stays separate (always-on). Budget is also always-on in HUD per D24 (NOT a stats-panel tab).

UI need: stats-panel CityStats tab, HUD readout cluster, minimap-integrated metrics, data feed shared with graphs tab.

### 3.30 Audio — D35 lock

**UI SFX (4 cue groups):**

| Cue group | Trigger | Notes |
| --- | --- | --- |
| Toolbar + picker | Toolbar tool click · subtype-picker open · subtype-picker close · subtype card confirm | Stubs already in `SubtypePickerController.cs` (`sfxPanelOpen` / `sfxPanelClose` / `sfxPickerConfirm`) — reuse + extend. |
| Paint commit | Mouse-up commit per family (zone / road / power / water / sewage / service / forest / landmark) | Different cue per family for category identity. Demolish has its own crunch cue. |
| HUD buttons | Play/pause toggle · speed-cycle advance · map toggle · stats toggle · `?` toggle | Each HUD button gets a click cue. Toast-pop + bell-chime DROPPED (D35). |
| Error / validation | Placement validation failure | Single cue family — short error chirp on every "Cannot place …" notification. |

**Music + ambient:**
- **Music** — playlist of **3–5 looping tracks**. Tracks rotate on track-end (random or sequential, TBD). Settings panel (D14) gets a master music slider + a track-skip button (post-MVP — not on critical path).
- **City ambient layer** — single looping bed (traffic + birds + wind + distant crowd) on top of music. Fades up over ~3 sec on scene load.

UI need: AudioMixer with channels for UI / Music / Ambient (per D14 5-slider audio settings). Settings sliders: Master + Music + Ambient + UI-SFX + Game-SFX. Asset workstream owns track composition + ambient bed; cue assignment is per-system wiring task during Bucket 4 (audio/UX polish).

### 3.32 HUD strip composition — D29 + D36 lock

**Single bottom strip. ~9 cells. No top strip, no corner clusters.** CityStats readouts move into `stats-panel` (toggleable via HUD). Bell + `?` DROPPED per D36.

| Slot | Cell | Behaviour |
| --- | --- | --- |
| 1 | City-name label | Static (set at new-game). |
| 2 | Budget readout | Clickable — opens `budget-panel` per D24. |
| 3 | Play / Pause toggle | Music-player style, icon flips per D5 corrected. |
| 4 | Speed-cycle | Click to cycle 1→2→3→4→1 per D5 corrected. |
| 5 | Map toggle | Opens floating `minimap` panel per D26. |
| 6 | Stats toggle | Opens `stats-panel` (3 tabs: Graphs / Demographics / CityStats) per D24. |
| 7 | Zoom-in | Camera control per D21. |
| 8 | Zoom-out | Camera control per D21. |
| 9 | Recenter | Camera control per D21. |

**Dropped from current scene baseline (19 cells → 9):** AUTO toggle, budget +/- buttons, budget-graph button, build-residential, build-commercial, speed-1x..speed-5x discrete buttons (now single speed-cycle), play button (now part of play/pause toggle), pause button (now part of play/pause toggle). **Per D36:** Bell (event-feed-panel trigger) + `?` (glossary-panel trigger) DROPPED.

UI need: hud-bar redesign — 9 cells, 3 zones (left = city + budget; center = time + map + stats; right = camera cluster).

### 3.31 Toolbar + subtype-picker contract — D23 lock

**One left-side vertical toolbar panel hosts every tool. One shared subtype-picker panel adapts to the selected tool.**

Tool categories in toolbar (top → bottom):

| Tool | Default subtype on click | Subtype-picker content |
| --- | --- | --- |
| R-zone | Light R | (Density auto-evolves — picker shows zone-info / clear) |
| C-zone | Light C | (Density auto-evolves — picker shows zone-info / clear) |
| I-zone | Manufacturing | 3 cards: Agriculture / Manufacturing / Tech (D4) |
| Road | Street | 4 cards: Street / Avenue / Arterial / Interstate |
| Power | Coal plant | 2 cards: Coal / Solar (D15 + D32) |
| Water | Reservoir | 2 cards: Reservoir / Desalination (D15 + D32) |
| Sewage | Basic plant | 2 cards: Basic / Treated (D15 + D32) |
| Service (S-zone) | Police | 7 cards: police / fire / edu / health / parks / public-housing / public-offices (D9) |
| Forests | Sparse + single-cell mode | 3 density cards (sparse / medium / dense) + 2 mode buttons (single-cell / spray) (D32) |
| Landmark | First unlocked City landmark | 4 cards: 2 City + 2 Region (D17) |
| Demolish (bulldoze) | — (no subtype) | — (picker stays closed; D31) |

**Interaction contract:**

1. Click any toolbar tool → that tool becomes active with its **default subtype pre-selected** (cursor primed for paint).
2. Same click → **subtype-picker panel opens** showing the category's subtypes with the default highlighted. Demolish tool skips this — picker stays closed (D31).
3. Click another subtype in picker → cursor switches; toolbar tool stays active.
4. Click another toolbar tool → subtype-picker re-renders for the new category, default subtype primed.
5. Click outside toolbar / picker (or press Esc) → tool deactivates, picker closes.

UI need: 11-row vertical toolbar (10 paint tools + Demolish) per D32, single shared subtype-picker panel with two layout variants (cards-only for 9 paint tools; cards + secondary mode-buttons for Forests), default-subtype + default-mode mapping per tool, picker open/close on toolbar click. Demolish row toggles bulldoze mode (mutually exclusive with paint tools).

**Existing implementation context (D32 reconciliation):** `Assets/Scripts/UI/SubtypePickerController.cs` already ships with `ToolFamily` enum covering 8 families (R / C / I / StateService / Roads / Forests / Power / Water). D32 lock aligns spec with code by splitting Utility into Power / Water / Sewage and keeping Forests as standalone tool. Sewage family must be added to the enum. Landmark family must be added. StateService = MVP's Service (S-zone). Spec table is now the canon — code catches up post-D32.

---

### 3.33 Paint interaction model — D31 lock: drag-paint canon

**Existing in code — keep as MVP canon. No redesign.**

Two paint flavors confirmed in the current build:

| Tool | Lifecycle | Source | Behavior |
| --- | --- | --- | --- |
| Zone (R / C / I / S) | drag-paint | `ZoneManager.cs:414` (`HandleZoning`) | mouse-down `StartZoning` → mouse-held `UpdateZoningPreview` (rectangle) → mouse-up `PlaceZoning` commits. Right-click cancels. |
| Road | drag-paint | `RoadManager.cs:142` (`HandleRoadDrawing`) | mouse-down sets `isDrawingRoad = true` + start cell → drag previews the line → mouse-up commits via `TryFinalizeManualRoadPlacement`. Right-click cancels (unless camera pan). |
| Power / Water / Sewage / Service / Landmark | single-cell click | (per-manager placement validators) | one click drops one prefab on the hovered cell. No drag. |
| Forests (D32) | mode-dependent | NEW (Stage TBD) | Player picks density (sparse/medium/dense) + mode (single-cell / spray) inside picker. **Single-cell mode** = click one cell, drop tree cluster matching density. **Spray mode** = drag-paint area (mouse-down → preview rectangle → mouse-up commit) — density controls trees-per-cell on commit. Right-click cancels in spray mode. |
| Demolish (bulldoze) | mode toggle + single-cell click | `UIManager.bulldozeMode` (cs:87) → `GridManager.DemolishCellAt(Vector2)` (cs:740) | toolbar Demolish click toggles `bulldozeMode = true`; subsequent left-click on a cell calls `DemolishCellAt` (one cell per click). Click another toolbar tool / Esc to exit mode. No drag-demolish in MVP. |

**Visual feedback (canon):**
- Zone drag → preview tiles spawned each frame from `previewZoningTiles` (cleared on commit / cancel).
- Road drag → preview line + cursor marker; ghost preview hidden on mouse-down (`uiManager.HideGhostPreview()`), restored on cancel.
- Bulldoze mode → cursor swap + button illumination (`bulldoze ? 1f : 0f` in `ToolbarDataAdapter.cs:381`).

**MVP rule:** all paint tools share this lifecycle shape (down → drag-preview → up-commit) for any tool that supports area placement. Single-cell tools (utility, service, landmark, demolish) skip the drag stage. No new tools introduce a different interaction model.

UI need: nothing new — paint canon already wired. Demolish addition reuses `bulldozeMode` flag + existing `bulldoze-button-64` toolbar button (currently in left-toolbar). Subtype-picker integration for paint tools is the only new wiring per §3.31.

---

## 4. Region scale — IN scope (deferred from City UI grilling)

Listed for completeness. **Not** part of this UI grilling — primitives produced from City grilling (tokens, components, panel layouts) reused at Region grilling.

- **RegionScene gameplay:** scale switch from City, region budget readout, regional aggregates (pop / happiness / pollution upscaled), inter-regional roads, regional utility pool readout, regional landmarks, deficit transfer surface, **country edge-signals** (national budget pressure, country utility pool level) shown at map borders. International hooks + national big-projects gating DROPPED (D3).
- **Country (simulated, no scene):** background simulation — national tax (set inside RegionScene), country budget, deficit + bonds, country utility pool from natural wealth. NO international hooks. NO national big-projects commission.
- **Scale-switch UX (City ↔ Region):** transition animation, per-scale `ScaleToolProvider`, procedural fog at scale boundary.

---

## 5. Out-of-scope (hard exclusions)

Cited from `docs/full-game-mvp-exploration.md §MVP scope — OUT`. Every bucket plan must reject scope creep that violates this list.

- Map / terrain creation tool (player-authored heightmap).
- Disasters as authored events (earthquakes, floods, fire-as-event). Fire = gameplay state only.
- Modding / Steam Workshop.
- Multiplayer / co-op.
- Cloud save sync.
- Achievements / meta progression.
- Localisation (English only).
- Controller / gamepad (mouse + keyboard only).
- Mobile / touch.
- Weather simulation.
- Day / night cycle.
- Seasons.
- Political / policy system beyond zoning + tax + budget (no elections, no parties).
- Research / tech tree.
- Procedural events / quests / storylets.
- Advanced economy (stock market, import / export depth).
- Dynamic terrain (erosion, river migration).
- Population individuality (named sims, bios, life events).
- Auto-telemetry.
- In-game bug-report UI.
- Crash reporter (testers report manually via web feedback form).
- Accessibility (colorblind modes, text scaling, subtitles, high-contrast).
- Steam / itch public store presence (private distribution only).
- WebGL build.
- Vehicle sprite variants, decoration / prop sprites, seasonal variants.
- Adaptive music (single shared track + per-scale ambient only).
- Second-country playable.
- **International hooks (border-sign UX, customs, trade deals) — entirely dropped (D3).**
- **National-scale big-projects commission / gating (D3).** Landmarks ship at City + Region scale only.
- Per-vehicle pathing (traffic flow abstraction only).
- Stock market, import / export depth, advanced bond market dynamics.
- **Tourism industrial subtype (D4).** I-zone splits into Agriculture / Manufacturing / Tech only.
- **Full-gridmap color-tint overlays (D6).** Overlays render on the minimap only — gridmap stays diegetic.
- **Onboarding / tutorial / coachmarks (D7).** Players discover the game on their own; tester reactions drive post-MVP UX iteration.
- **Speed-5x time step (D5 corrected).** Max sim speed = 4x.
- **Districts (D8).** No paint tool, no naming, no per-district stats / policy. City-scale work only.
- **Public transport (D11).** No bus / subway / tram / rail. Cars-only movement model.
- **Pedestrian sim (D11).** No walking agents. Traffic-flow heuristic remains (cars only).
- **CityStats ↔ web-dashboard parity (D12).** In-game CityStats ships standalone; web-dashboard mirror is post-MVP work.
- **Difficulty levels (D18).** No Easy/Normal/Hard split. Single sim tuning across all new games.
- Cert-based code signing (unsigned distribution acceptable for the curated tester pool).

---

## 6. UI surface implications — what the City grilling must produce

Derived from §3 features. Inventory of panels the grilling must lock. Each panel will be defined separately in `docs/ui-element-definitions.md` Phase 1.

| # | Panel slug (proposed) | Drives | Notes |
| --- | --- | --- | --- |
| 1 | `hud-bar` | Single bottom strip, ~9 cells per D29 + D36: city-name + budget + play/pause + speed-cycle + map + stats + zoom-in + zoom-out + recenter. Bell + `?` DROPPED per D36. CityStats readouts moved into `stats-panel`. | Redesign required — 19 → 9 cells. |
| 2 | `left-toolbar` (existing vertical panel) | 11 tool buttons per D23 + D31 + D32: R-zone / C-zone / I-zone / Road / Power / Water / Sewage / Service (S) / Forests / Landmark / Demolish. Paint tools (10) select default subtype + open subtype-picker on click. Demolish toggles `bulldozeMode` flag (no picker). | Existing scene panel — REUSED, NOT new. Paint canon (drag for area, click for single-cell, mode-dependent for Forests) per §3.33. |
| 3 | `subtype-picker` | Single shared panel per D23. Adapts content to active toolbar tool — shows subtypes with default highlighted. Card counts: R=3 · C=3 · I=3 (light/medium/heavy density per subtype; sim evolves WITHIN selected density — richer building / merge to bigger footprint, never crosses density tier) · StateZoning=7 (kind subtypes per D9, no density) · Road=4 · Power=2 · Water=2 · Sewage=2 · Forests=3 cards + 2 mode buttons (D32) · Landmark=4. | New. Replaces 4 separate per-category palette panels (road/utility/service/landmark). Two layout variants: cards-only (default) + cards+mode-buttons (Forests). |
| 8 | (folded into `minimap`) | Overlay toggles live INSIDE minimap panel per D26 — no separate panel. | Removed as standalone surface. |
| 9 | `info-panel` | One adaptive info surface. Content templates per click target (empty cell / R-C-I / road / S building / utility / landmark) per D20. | New. Replaces split cell-info + building-info. |
| 11 | (folded into `stats-panel` CityStats tab) | CityStats readouts NOT in HUD per D29 — live in stats-panel (CityStats tab). | Removed as standalone surface. |
| 12 | `stats-panel` | Single tabbed panel per D24. 3 tabs: **Graphs** (4 curves + range selector, D13) + **Demographics** full depth per D34 (population breakdown + age pyramid + education + income tiers + commute time + housing distribution) + **CityStats** (full dashboard view). | New. Replaces 3 separate panels. Demographics needs 3 chart primitives (histogram, age-pyramid, bar-chart). |
| 14 | `budget-panel` | Tax sliders + per-service allocation + bond dialog + deficit indicator. Opens when HUD budget readout clicked (D24). | New. Budget readout itself lives in HUD always-on. |
| 15 | `notifications-toast` | Side-of-screen toast stream per D27 + D36. Stacks right edge, ~5s auto-dismiss. Sole event-surface channel in MVP. | New. |
| 16 | (DROPPED — `event-feed-panel`) | DROPPED per D36. Persistent toast history deferred post-MVP. | — |
| 17 | `minimap` | Floating panel triggered by HUD `map` button per D26. Embeds overlay-toggle group (~13 toggles) inside the panel chrome. | Replaces current MAP button behaviour. Hosts overlays internally. |
| 18 | (DROPPED — `glossary-panel`) | DROPPED per D36. Tooltips primitive remains as sole discoverability channel. | — |
| 19 | `tooltip` | Hover-tooltip primitive (component-level). ~500ms delay, applies across all interactive elements per D28. | New primitive, not a panel. |
| 21 | `pause-menu` | Esc-triggered hub modal per D25. Buttons: Resume / Save / Load / Settings / Main menu / Quit. | New. |
| 22 | `save-modal` | Slot picker (D10 unlimited + name input). Opens nested from pause-menu. Back-arrow returns. | New. |
| 23 | `load-modal` | Slot picker (read-mode). Opens nested from pause-menu. Back-arrow returns. | New. |
| 24 | `settings-modal` | Audio + display + game settings (D14). Opens nested from pause-menu. Back-arrow returns. | New. |
| 25 | `new-game-form` | Map size + starting budget + city name + seed (D18). Pre-game flow, NOT nested under pause-menu. | New. |
| 26 | `main-menu` | Title screen per D30: Continue / New Game / Load / Settings / Quit. Continue resumes most-recent save. New Game opens `new-game-form`. Load opens `load-modal`. Settings opens `settings-modal`. | New (MainMenuController stub exists). |

**Total: ~13 panels + 1 component primitive** (post-D36 — event-feed-panel + glossary-panel dropped). Current scene has 1 panel (hud-bar) + 1 reused vertical toolbar. Grilling must lock ~11 new panel definitions plus the hud-bar redesign (19 → 9 cells per D29 + D36).

---

## 7. Companion workstream — `MainScene` → `CityScene` rename — DONE 2026-05-07

Rename completed 2026-05-07. Precondition for the multi-scale split (RegionScene + CountryScene siblings) cleared.

Surfaces touched:
- Unity scene file + meta (`Assets/Scenes/CityScene.unity`) + ProjectSettings build settings.
- C# code refs (managers, controllers, tests).
- IA docs + agent rules + skill bodies.
- Test fixtures + bridge command runners.
- BACKLOG live row prose (archive YAML left frozen — historical).

Historical references to the prior name remain in `ia/state/pre-refactor-snapshot/**`, archived BACKLOG rows, completed stage docs, and exploration / postmortem docs (frozen-in-time surfaces).

---

## 8. Source citations

| Source | Role |
| --- | --- |
| `docs/full-game-mvp-exploration.md` | Authoritative MVP scope (1064 lines, fourth pass, Framing F locked 2026-04-16) — IN list, OUT list, bucket structure, dependency graph, multi-scale data flows. |
| `docs/full-game-mvp-rollout-tracker.md` | 12-row rollout matrix. Tier lanes A→E. Per-bucket Order column. |
| `ia_master_plans` (DB) — slug `full-game-mvp` | Umbrella orchestrator stub (no stages — coordination only). |
| `Assets/UI/Snapshots/panels.json` (schema_v4) | Current scene baseline — 1 panel (hud-bar) + 19 children. |
| `docs/ideas/ui-elements-grilling.md` | UI grilling process spec (calibration system) — consumes this scope doc to derive panel inventory. |
| `docs/ui-element-definitions.md` | Output target — Phase 0 baseline annotation done; Phase 1 panel grilling pending. |

---

## 9. Changelog

| Date | Change |
| --- | --- |
| 2026-05-07 | Doc created. Scope locked from `full-game-mvp-exploration.md` Framing F. City scale isolated for current UI grilling; Region + Country deferred. |
| 2026-05-07 | D1 lock — 2 scenes only (CityScene + RegionScene). No CountryScene. Country = simulated layer at RegionScene borders. |
| 2026-05-07 | D2.1 lock — City↔Region signal contract: all 4 groups IN (money / people / resource / physical flows). |
| 2026-05-07 | D3 lock — Region↔Country signal contract: national budget+bonds + country utility pool IN. International hooks + national big-projects gating DROPPED. Landmarks reduced to City + Region scale only. |
| 2026-05-07 | D4 lock — Industrial subtypes reduced to 3: Agriculture / Manufacturing / Tech. Tourism subtype DROPPED from MVP. |
| 2026-05-07 | D5 lock — Time control cluster collapsed from 7 buttons to 2: Speed-cycle + Pause-toggle. Onboarding must teach the pause-toggle interaction. |
| 2026-05-07 | D6 lock — Overlays render on minimap only (NOT gridmap). Additive toggle stack — player can layer any combination. Full-gridmap color-tint overlays DROPPED from MVP. |
| 2026-05-07 | D5 corrected — Time controls = 2 buttons. Play/Pause toggle (music-player style, single button flips state) + Speed-cycle (1→2→3→4→1, single click). Max speed 4x. 5x DROPPED. No long-press interaction. |
| 2026-05-07 | D7 lock — Onboarding DROPPED. No tutorial in MVP. Players discover the game; tester reactions feed post-MVP UX iteration. |
| 2026-05-07 | D8 lock — Districts DROPPED. No paint tool, no naming, no per-district policy. City-scale work only. |
| 2026-05-07 | D9 lock — S-zone has 7 sub-types: 5 coverage services (police/fire/edu/health/parks) + 2 capacity services (public-housing, public-offices). Capacity services surface in budget+demographics, not overlay stack. |
| 2026-05-07 | D10 lock — Save slots = unlimited, player-named. Scroll list + sort + add/delete/rename. No fixed slot count. |
| 2026-05-07 | D11 lock — Public transport DROPPED. Pedestrian sim DROPPED. Cars are the only mode of movement. |
| 2026-05-07 | D12 lock — In-game CityStats dashboard SHIPS in MVP. Web-dashboard parity DEFERRED to post-MVP. |
| 2026-05-07 | D13 lock — Graphs panel = 4 curves (pop/happiness/pollution/budget) + 3-position range selector (week/month/all-time). No pan/zoom. |
| 2026-05-07 | D14 lock — Settings panel = full set: Audio (5 sliders) + Display (resolution + fullscreen) + Game (autosave interval + default speed). |
| 2026-05-07 | D15 lock — Utility plants = 2 variants per utility (dirty + clean tier). Power = coal/solar; Water = reservoir/desal; Sewage = basic/treated. 6 plant families total. |
| 2026-05-07 | D16 lock — R/C/I density tiers = 3 (light → medium → heavy). ~15 sprite families minimum for R/C/I (3 tiers × 5 zone variants). |
| 2026-05-07 | D17 lock — Landmarks = 4 total (2 City + 2 Region). National-scale dropped per D3. Specific identities + thresholds defer to Bucket 2/3 author time. |
| 2026-05-07 | D18 lock — New-game form = map size + starting budget + city name + seed. Difficulty DROPPED. Single sim tuning. |
| 2026-05-07 | D19 lock — Notifications = both surfaces. Toast (transient ~5s) + event feed panel (persistent scrollable history). |
| 2026-05-07 | D20 lock — Info panel = single unified surface. Click any target (cell / road / building / landmark) → same panel adapts content via template per target kind. Replaces split cell-info + building-info panels. |
| 2026-05-07 | D21 lock — Camera cluster = 3 buttons (zoom-in / zoom-out / recenter). Mouse wheel zoom + drag pan parallel. No rotate. Fixed-angle iso view. |
| 2026-05-07 | D22 lock — Zone palette hosted in existing left-side vertical toolbar panel (NOT new). HUD build-residential + build-commercial buttons DROPPED. Road / utility / service / landmark palette hosting decisions deferred (separate decisions). |
| 2026-05-07 | D23 lock — All tools (zones / roads / utilities / services / landmarks) hosted in left-toolbar (7 tools). One shared subtype-picker panel adapts content to active tool. Every tool selects a default subtype on click + opens picker with default highlighted. Replaces 4 separate per-category palette panels. |
| 2026-05-07 | D24 lock — Budget readout always-on in HUD; full budget editor opens as `budget-panel` on HUD click. Graphs / Demographics / CityStats fold into shared `stats-panel` (3 tabs). Replaces 3 separate panels. |
| 2026-05-07 | D25 lock — Pause-menu is the in-game hub modal. Save / Load / Settings open as nested modals from pause-menu (back-arrow returns). Sim paused while menu / sub-modals open. Main-menu + new-game stay separate (pre-game flow). |
| 2026-05-07 | D26 lock — Minimap hidden by default; HUD `map` button toggles a floating minimap panel. Overlay toggles (~13) embedded INSIDE minimap panel (no separate overlay-toggles panel). |
| 2026-05-07 | D27 lock — Toasts stack on right edge of screen (~5s auto-dismiss). Event-feed-panel opens via HUD bell-icon button (unread-count badge). |
| 2026-05-07 | D28 lock — Tooltips on hover (~500ms delay) across all interactive elements. Dedicated glossary-panel opens via HUD `?` button (alphabetical term list + detail view). |
| 2026-05-07 | D29 lock — HUD = single bottom strip, 11 cells: city-name + budget + play/pause + speed-cycle + map + stats + bell + ? + zoom-in + zoom-out + recenter. CityStats moved into stats-panel CityStats tab. Drops from current 19-cell scene: AUTO, budget +/-, budget-graph, build-R, build-C, speed-1..5 discrete, play, pause. |
| 2026-05-07 | D30 lock — Pre-game flow: main-menu (Continue / New Game / Load / Settings / Quit) → new-game-form OR load-modal → CityScene. Continue resumes most-recent save. |
| 2026-05-07 | D31 lock — Paint canon = existing in code (no redesign): Zone + Road = drag-paint (mouse-down → preview-on-drag → mouse-up commit; `ZoneManager.HandleZoning` + `RoadManager.HandleRoadDrawing`); Utility / Service / Landmark = single-cell click; Demolish = mode-toggle + single-cell click (`UIManager.bulldozeMode` → `GridManager.DemolishCellAt`). Demolish added as 8th toolbar tool (no subtype-picker). Picker family taxonomy reconciliation with existing `SubtypePickerController` 8-family enum deferred to D32+. |
| 2026-05-07 | D32 lock — Toolbar grows to 11 tools: Utility split into 3 separate tools (Power / Water / Sewage, 2 cards each); Forests added as standalone tool (3 density cards: sparse/medium/dense + 2 placement-mode buttons inside picker: single-cell / spray). Forests spray mode = drag-paint (density → trees-per-cell on commit). Subtype-picker gains a second layout variant (cards + secondary mode buttons) — first picker shape beyond cards-only. Code reconciliation: add `Sewage` + `Landmark` to `SubtypePickerController.ToolFamily` enum; rename `StateService` → `Service` for spec parity. Toolbar redesign owns 11 rows. |
| 2026-05-07 | D33 lock — Game-time = current `TimeManager.cs` canon: 1x = 1 real-sec per game-day. Day-tick fires daily updates; month-tick (game-Day==1) fires monthly updates + economy close. Budget close = monthly (taxes in, expenses out, balance updates, single notification toast). Drift to fix in code: `timeSpeeds = {0, 0.5, 1.0, 2.0, 4.0}` should become `{0, 1.0, 2.0, 3.0, 4.0}` to match D5 1/2/3/4 cycle (drop 0.5x, add 3x). Start date `2024-08-27` hardcoded in MVP; parameterization deferred. |
| 2026-05-07 | D34 lock — Demographics tab ships at full depth: population breakdown + age pyramid + education levels + income tiers + commute time + housing distribution (3 chart primitives needed: histogram + age-pyramid + bar-chart). Updated monthly (matches D33 budget close). City events MVP scope = service-driven (D9) + utility-driven (D15) + budget (D24) + pre-existing (landmark unlock / construction complete / validation failure) only. DROPPED: random natural events (earthquake / flood / wildfire) + economic shocks (recession / boom / I-tech-shift) — both deferred to post-MVP. |
| 2026-05-07 | D35 lock — Audio scope: 4 UI-SFX cue groups (toolbar + picker / paint commit per family / HUD button feedback / validation error). Toast pop + bell chime DROPPED. Music = 3–5 looping track playlist that rotates on track-end. Ambient = single city bed (traffic + birds + wind + distant crowd) layered on top. AudioMixer with 5 channels per D14 settings sliders (Master / Music / Ambient / UI-SFX / Game-SFX). `SubtypePickerController` already stubs `sfxPanelOpen` / `sfxPanelClose` / `sfxPickerConfirm` — reuse. |
| 2026-05-07 | D9/D16 clarification — R/C/I subtypes = density (light/medium/heavy, 3 cards each). Density-evolution stays WITHIN subtype: a placed light-residential cell can level-up into a richer light-residential building or merge with neighbors into a larger light-residential footprint, but never crosses into medium/heavy density. StateZoning (D9) has KIND subtypes (7 cards), no density. Picker card counts revised: R=3 · C=3 · I=3 · StateZoning=7 · Road=4 · Power=2 · Water=2 · Sewage=2 · Forests=3+2 · Landmark=4. |
| 2026-05-07 | D36 lock — Event-feed-panel DROPPED. Glossary-panel DROPPED. HUD reduces 11 → 9 cells (drop bell + `?`). Tooltips primitive remains. Persistent event history deferred post-MVP. Toast-only feedback channel; toast spec finalized in `ui-element-definitions.md` (5 tiers + city-milestone + service-coverage events). Inventory drops to ~13 panels + 1 primitive. |
