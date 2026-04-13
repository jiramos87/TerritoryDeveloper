# Multi-Scale Simulation — Master Plan (MVP)

> **Status:** In Progress — Step 1 / Stage 1.1 done (TECH-87 + TECH-88 + TECH-89); Stage 1.2 next
>
> **Scope:** Min load-bearing work to prove city ↔ region ↔ country game loop (dormant evolution + reconstruction). Rest → `multi-scale-post-mvp-expansion.md`.
>
> **Vision + design principles:** `ia/specs/game-overview.md`
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Read first if landing cold:**
> - `ia/specs/game-overview.md` — vision + principles
> - `ia/specs/simulation-system.md` — current single-scale tick loop (MCP `spec_section`)
> - `ia/projects/multi-scale-post-mvp-expansion.md` — scope boundary (what's OUT of MVP)
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics
> - MCP: `backlog_issue {id}` per referenced id; never full `BACKLOG.md` read.

---

## Steps

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

### Step 1 — Parent-scale conceptual stubs

**Status:** In Progress — Stage 1.1

**Objectives:** surface parent region + country identity in city code + save. Land cell-type split as refactor base for parent scales. Plant neighbor-city stub + interstate-border read contract (inert). Zero behavior shift at city scale; no playable parent scales.

**Exit criteria:**

- Every city save carries non-null `region_id` + `country_id` (placeholders OK).
- ≥1 neighbor-city stub at interstate border, readable by city sim (inert).
- Interstate connections admit "flow to/from parent-region neighbor" interpretation.
- Save/load round-trips, no regression.
- Cell-type split: `Cell` API → `CityCell` / `RegionCell` / `CountryCell`; city sim builds + runs against new types, no behavior regression.

**Art:** None (code-only stubs).

**Relevant surfaces (load when step opens):** `Assets/Scripts/Grid/Cell.cs`, `Assets/Scripts/SaveSystem/GameSaveData.cs`, `Assets/Scripts/GridManager.cs`, `Assets/Scripts/InterstateManager.cs`, `ia/specs/save-system.md` (§schema), `ia/rules/invariants.md` (#1, #5).

#### Stage 1.1 — Parent-scale identity fields

**Status:** Draft

**Objectives:** city save + `GridManager` carry non-null `region_id` + `country_id` (placeholder GUIDs). Legacy saves migrate cleanly.

**Exit:**

- `GameSaveData` has non-null `region_id` + `country_id` (GUID).
- `GridManager` exposes read-only `ParentRegionId` / `ParentCountryId` set at load / new-game.
- Save/load round-trips both ids.
- Legacy saves migrate w/ placeholder ids; no data loss; save version bumped.
- Glossary rows land for **parent region id** + **parent country id**.

**Phases:**

- [x] Phase 1 — Schema + migration (data shape, version bump, legacy load path).
- [ ] Phase 2 — Runtime surface (`GridManager` properties + new-game placeholder allocation).
- [ ] Phase 3 — Round-trip + migration tests (testmode batch).

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T1.1.1 | 1 | **TECH-87** | Done | `GameSaveData` parent-id fields + save version bump + legacy migration + glossary rows. |
| T1.1.2 | 2 | **TECH-88** | Done | `GridManager` `ParentRegionId` / `ParentCountryId` surface + new-game placeholder allocation. |
| T1.1.3 | 3 | **TECH-89** | Done | Round-trip + legacy-migration tests (testmode batch scenario). |

#### Stage 1.2 — Cell-type split

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** `Cell` → `CityCell` / `RegionCell` / `CountryCell`. City sim unchanged in behavior. Invariants #1 (`HeightMap` ↔ `Cell.height` sync) and #5 (`GetCell` only) preserved.

**Exit:**

- `Cell` base type (abstract class or interface) carries coord + shared primitives.
- `CityCell` carries all existing city-scale fields.
- `RegionCell` + `CountryCell` land as thin placeholders (coord + parent id refs; no behavior).
- City sim compiles + runs against `CityCell`. Zero behavior regression (testmode smoke).
- `GridManager` typed surface — generic `GetCell<T>(x,y)` or scale-indexed overloads; existing `GetCell(x,y)` back-compat defaults to `CityCell`.
- Glossary rows land for three cell types.

**Phases:**

- [ ] Phase 1 — Base type extraction + `Cell` → `CityCell` rename (compile-only refactor).
- [ ] Phase 2 — `RegionCell` + `CountryCell` placeholder types + glossary rows.
- [ ] Phase 3 — `GridManager` typed surface + back-compat default.
- [ ] Phase 4 — Regression gate (`unity:compile-check` + testmode smoke + `HeightMap` integrity).

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T1.2.1 | 1 | **TECH-90** | Done | Extract `Cell` abstract base (coord, height, shared primitives). Compile-only; no rename yet. |
| T1.2.2 | 1 | **TECH-91** | Done | Rename `Cell` → `CityCell` across all city sim files. Preserve `HeightMap` sync (invariant #1). |
| T1.2.3 | 2 | **TECH-92** | Draft | `RegionCell` placeholder type (coord + parent-region-id; no behavior). Glossary row. |
| T1.2.4 | 2 | **TECH-93** | Draft | `CountryCell` placeholder type (coord + parent-country-id; no behavior). Glossary rows for all 3 cell types. |
| T1.2.5 | 3 | **TECH-94** | Draft | Generic `GetCell<T>(x,y)` or scale-indexed overloads on `GridManager`. Compile gate. |
| T1.2.6 | 3 | **TECH-95** | Draft | Back-compat `GetCell(x,y)` defaults to `CityCell`. Update all callers. Invariant #5 preserved. |
| T1.2.7 | 4 | **TECH-96** | Draft | Testmode smoke — city load + sim tick, no regression. |
| T1.2.8 | 4 | **TECH-97** | Draft | Testmode assertion — `HeightMap` / `CityCell.height` integrity (invariant #1). |

#### Stage 1.3 — Neighbor-city stub + interstate-border semantics

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** ≥1 neighbor stub per city at interstate border. Inert read contract for future cross-scale flow.

**Exit:**

- `NeighborCityStub` struct: `id` (GUID), display name, border side enum.
- New-game init places ≥1 stub at random interstate border (seed-deterministic).
- Interstate road exit binds to stub ref (lookup by border side).
- Flow consumer reads stub via inert API (returns 0 / empty; no behavior).
- Save/load preserves stubs + bindings round-trip.
- Glossary rows land for **neighbor-city stub** + **interstate border**.

**Phases:**

- [ ] Phase 1 — Stub schema + save wiring.
- [ ] Phase 2 — Interstate-border binding (new-game init + on-road-build at border).
- [ ] Phase 3 — City-sim inert read surface + glossary rows.
- [ ] Phase 4 — Round-trip + testmode smoke.

**Tasks:**

| Task | Phase | Issue | Status | Intent |
|---|---|---|---|---|
| T1.3.1 | 1 | _pending_ | _pending_ | `NeighborCityStub` struct (id GUID, display name, border side enum) + serialize schema. |
| T1.3.2 | 1 | _pending_ | _pending_ | `GameSaveData.neighborStubs` list + save version bump. |
| T1.3.3 | 2 | _pending_ | _pending_ | New-game init: place ≥1 stub at random interstate border (seed-deterministic). |
| T1.3.4 | 2 | _pending_ | _pending_ | On-road-build: road exit at border binds to stub ref by border side. |
| T1.3.5 | 3 | _pending_ | _pending_ | `GridManager.GetNeighborStub(side)` inert read contract (returns stub or null; no behavior). |
| T1.3.6 | 3 | _pending_ | _pending_ | Glossary rows for `neighbor-city stub` + `interstate border`. |
| T1.3.7 | 4 | _pending_ | _pending_ | Save/load round-trip test (stubs + bindings preserved). |
| T1.3.8 | 4 | _pending_ | _pending_ | Testmode smoke — stub at border after new-game; binding intact after road build at border. |

**Backlog state (Step 1):** Stage 1.1 tasks filed as BACKLOG rows + project specs under `§ Multi-scale simulation lane` (TECH-87 / TECH-88 / TECH-89). Stages 1.2 + 1.3 tasks stay in this doc; file BACKLOG rows + specs when parent stage → `In Progress`.

### Step 2 — City MVP close

**Status:** Draft (decomposition deferred until Step 1 → `Final`)

City scale **stable + readable enough** to serve as aggregation source + reconstruction target. Not a finished city-builder loop.

**Exit criteria:**

- No crasher / data-corruption bug open at city scale.
- Player reads city state at-a-glance (minimal dashboard + handful of charts).
- Single city tick cheap enough that one dormant city alongside one active city is credible (target set in Step 3 parity harness).
- Parent-scale stubs from Step 1 consumed by ≥1 city system.

**Art:** None.

**Relevant surfaces:** `backlog_issue BUG-55` / `BUG-16` / `BUG-17` / `BUG-14` / `FEAT-51` / `TECH-82`; `ia/specs/simulation-system.md`.

### Step 3 — Multi-scale infrastructure

**Status:** Draft (decomposition deferred until Step 2 → `Final`)

Scale-neutral spine. After Step 3: pure-compute sim modules, `SimulationScale` enum + `ISimulationModel` contract, per-scale snapshot schema (city first), relational multi-scale save, deterministic city evolution algorithm, snapshot freeze/reconstruct, procedural-scale generator, scale-switch UX skeleton — all exercised by one scale (city).

**Exit criteria:**

- City dormant → snapshot + evolution → reconstruct round-trip inside parity budget (empirical playtest).
- Procedural city generated from region-like params + seed, loadable as normal `GameSaveData`.
- Save format holds cities inside region inside country node (relational), even if region/country evolution = stubs.
- Switch out of city + back round-trips correctly (Δt = 0 → same state modulo parity budget).
- Compute-lib modules callable headless for ≥1 non-trivial city sub-system AND city evolution algorithm.
- Every scale reads same real-time calendar via single shared clock API.
- Scale-switch UX — semantic zoom: continuous camera zoom across per-scale zoom bands (fixed transition points, same regardless of map size). Zoom bands: city 2–30, transition 30–60, region 60–200, transition 200–400, country 400+. Procedural fog/cloud mask (fullscreen noise shader) hides scene swap in transition band. Player cancels mid-transition by scrolling back (fog reverses). Scale label appears near threshold (e.g. "Entering Region View"). Reconstruction latency mitigation: region shell pre-cached low-res; progressive reconstruction; snapshot cache per city node. Post-MVP alternatives (truly continuous rendering, animated fly-to, minimap click, asymmetric zoom-out vs zoom-in) → `multi-scale-post-mvp-expansion.md` §6.4.
- Per-scale tool panel swap via `ScaleToolProvider`: toolbar rebuilds during fog mask per active scale. Minimal MVP tool sets — city: existing tools; region: found city + draw highway + budget; country: priorities + budget. Fixed always-visible strip for shared tools (demolish, inspect, speed control). Consistent semantic keybindings across scales. Per-scale tool state preserved in-session (not in save — save persistence post-MVP).
- Speed control unchanged across all scales.

**Art:** Procedural fog/cloud transition shader (fullscreen noise quad). Scale label UI. Per-scale toolbar icons (region + country tool sets).

**Relevant surfaces:** `Assets/Scripts/SimulationManager.cs`, `Assets/Scripts/TimeManagement/TimeManager.cs`, `Assets/Scripts/SaveSystem/`, `ia/specs/simulation-system.md` (§tick-loop); `backlog_issue TECH-38` / `TECH-82` / `TECH-18` / `TECH-31` / `TECH-15` / `TECH-34` / `FEAT-46`; `ia/projects/multi-scale-post-mvp-expansion.md` §6.4 (scale-switch UX alternatives).

### Step 4 — Region MVP

**Status:** Draft (decomposition deferred until Step 3 → `Final`)

Region **playable as active scale** w/ own live-sim tick loop + deterministic evolution algorithm.

**Exit criteria:**

- Player switches city → region, sees other cities reconstructed from snapshots + pending deltas, plays region as active scale, switches back into any city (visited or procedural) w/o state loss + inside parity budget.
- Region active-scale tick: migration pressure, basic trade flow, founding new cities.
- Region evolution algorithm: deterministic `evolve` analogous to city algorithm.
- ≥1 economic flow crosses scales: city exports feed inter-city trade in region layer, balance feeds back into city evolution params on switch-down.
- Region has 1 natural resource type + supports founding new cities.
- Player-authored dormant control (min): from region view, player sets budget allocation per dormant child city.
- Save/load preserves city + region end-to-end in relational schema.
- Parity-budget checks for region evolution algorithm.
- City event bubble-up at switch-out visible in region dashboard (plain text summary OK).

**Art:** Region cell sprites, city-node visual at region zoom, region UI elements, procedural region art templates.

**Relevant surfaces:** `backlog_issue FEAT-09` / `FEAT-47`; `ia/specs/simulation-system.md`; region sim contracts land in Step 3 — fetch then.

### Step 5 — Country MVP

**Status:** Draft (decomposition deferred until Step 4 → `Final`)

Country **playable as active scale**. After Step 5: three-scale MVP complete.

**Exit criteria:**

- Player switches to country map, plays country as active scale, exercises min head-of-state loop.
- Head-of-state loop (min): assign national budget across small fixed category set, launch ≥1 national infrastructure project propagating down to region/city, create ≥1 new region node.
- Country policy change while active baked into region + city evolution params on switch-down.
- Country evolution algorithm: deterministic fast-forward via long-period economic drift.
- Player-authored dormant control (min): from country view, player sets budget allocation per dormant region.
- Save/load preserves all three scales end-to-end in relational schema.
- Parity-budget checks for country evolution algorithm.
- Region/city events bubbled up at switch-out visible in country dashboard (plain text summary OK).

**Art:** Country cell sprites, region-node visual at country zoom, country UI elements, head-of-state UI.

**Relevant surfaces:** country sim contracts land in Step 3 — fetch then; `ia/projects/multi-scale-post-mvp-expansion.md` (head-of-state scope boundary).

---

## Deferred decomposition

Steps 2–5 stay at skeleton granularity (Objectives implicit in step blurb + Exit criteria + Relevant surfaces). Full Stage / Phase / Task decomposition lands when parent step → `In Progress`. Candidate-issue pointers live inline on each step's **Relevant surfaces** line; new-feature-row candidates surface during that step's decomposition pass, filed under `§ Multi-scale simulation lane` in `BACKLOG.md`. Do NOT pre-file Step 2–5 rows.

---

## Orchestration guardrails

**Do:**

- Propose edits to step skeletons when stage exposes missing load-bearing item.
- Push MVP-scope-creep into `multi-scale-post-mvp-expansion.md`.
- Create step/stage orchestrators lazily when parent enters "in progress".

**Do not:**

- Resurrect N-tick aggregate publish model. Dormant scales evolve only via deterministic evolution algorithm.
- Resurrect time-dilation framing. Single shared real-time clock.
- Resurrect single-jsonb save tree. Save is relational.
- Resurrect NPC leader modeling. Player = only actor in MVP.
- Reintroduce climate, shaping events, defense structures, expropriation, agricultural zones, progressive loading, shared cross-scale dashboard, auto mode, scale unlock, or process-engineering gap closures into MVP stages. All post-MVP.
- File BACKLOG rows for new FEAT ideas outside backlog triage pass.
- Give time estimates.
