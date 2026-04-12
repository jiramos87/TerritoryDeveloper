# Multi-Scale Simulation — Master Plan (MVP)

> **Status:** In Review
>
> **Scope:** Minimum load-bearing work to prove city ↔ region ↔ country game loop (dormant evolution + reconstruction). Everything else → `multi-scale-post-mvp-expansion.md`.
>
> **Vision + design principles:** `ia/specs/game-overview.md`
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Read first if landing cold:**
> - `BACKLOG.md` · `BACKLOG-ARCHIVE.md` · `CLAUDE.md` · `AGENTS.md` · `ARCHITECTURE.md`
> - `ia/rules/invariants.md` · `ia/specs/glossary.md` · `ia/specs/simulation-system.md`
> - Deferred work: `ia/projects/multi-scale-post-mvp-expansion.md`
> - MCP: prefer `mcp__territory-ia__*` over full file reads.

---

## Steps

### Step 1 — Parent-scale conceptual stubs

Make parent region and parent country **visible in city code and save data** before city MVP close. No playable region or country yet.

**Exit criteria:**

- Every city save carries non-null `region_id` and `country_id` (placeholders OK).
- At least one neighbor-city stub present at interstate border, readable by city sim (inert).
- Interstate connections admit "flow to/from parent-region neighbor" interpretation.
- Save/load round-trips with no regression.
- Cell-type split executed: `Cell` API → `CityCell` / `RegionCell` / `CountryCell`; city sim builds and runs against new types, no behavior regression.

**Art:** None (code-only stubs).

### Step 2 — City MVP close

City scale **stable enough and readable enough** to serve as aggregation source and reconstruction target. Not a finished city-builder loop.

**Exit criteria:**

- No crasher or data-corruption bug open at city scale.
- Player reads current city state at a glance (minimal dashboard + handful of charts).
- Single city tick cheap enough that running one dormant city alongside one active city is credible (target set in Step 3 parity harness).
- Parent-scale stubs from Step 1 consumed by at least one city system.

**Art:** None.

### Step 3 — Multi-scale infrastructure

Scale-neutral spine. After Step 3: pure-compute sim modules, `SimulationScale` enum + `ISimulationModel` contract, per-scale snapshot schema (city first), relational multi-scale save, deterministic city evolution algorithm, snapshot freeze/reconstruct, procedural-scale generator, scale-switch UX skeleton — all exercised by one scale (city).

**Exit criteria:**

- City dormant → snapshot + evolution → reconstruct round-trip inside parity budget (empirical playtest).
- Procedural city generated from region-like parameters + seed, loadable as normal `GameSaveData`.
- Save format holds cities inside region inside country node (relational), even if region/country evolution = stubs.
- Switch out of city and back round-trips correctly (Δt = 0 → same state modulo parity budget).
- Compute-lib modules callable headless for at least one non-trivial city sub-system AND city evolution algorithm.
- Every scale reads same real-time calendar via single shared clock API.
- Scale-switch UX — semantic zoom: continuous camera zoom across per-scale zoom bands (fixed transition points, same regardless of map size). Procedural fog/cloud mask hides scene swap in the transition band. Player can cancel mid-transition by scrolling back. Scale label appears when approaching threshold. Design details: `docs/scale-switch-ux-exploration.md`.
- Per-scale tool panel swap via `ScaleToolProvider`: toolbar rebuilds during fog mask per active scale. Minimal MVP tool sets — city: existing tools; region: found city + draw highway + budget; country: priorities + budget. Fixed always-visible strip for shared tools (demolish, inspect, speed control). Consistent semantic keybindings across scales. Per-scale tool state preserved in-session (not in save data — save persistence is post-MVP).
- Speed control unchanged across all scales.

**Art:** Procedural fog/cloud transition shader (fullscreen noise quad). Scale label UI. Per-scale toolbar icons (region + country tool sets).

### Step 4 — Region MVP

Region becomes **playable as active scale** with its own live-sim tick loop and deterministic evolution algorithm.

**Exit criteria:**

- Player switches city → region, sees other cities reconstructed from snapshots + pending deltas, plays region as active scale, switches back into any city (visited or procedural) without state loss and inside parity budget.
- Region active-scale tick: migration pressure, basic trade flow, founding new cities.
- Region evolution algorithm: deterministic `evolve` analogous to city algorithm.
- At least one economic flow crosses scales: city exports feed inter-city trade in region layer, balance feeds back into city evolution parameters on switch-down.
- Region has one natural resource type and supports founding new cities.
- Player-authored dormant control (minimum): from region view, player sets budget allocation per dormant child city.
- Save/load preserves city + region end-to-end in relational schema.
- Parity-budget checks for region evolution algorithm.
- City event bubble-up at switch-out visible in region dashboard (plain text summary sufficient).

**Art:** Region cell sprites, city-node visual at region zoom, region UI elements, procedural region art templates.

### Step 5 — Country MVP

Country becomes **playable as active scale**. After Step 5: three-scale MVP complete.

**Exit criteria:**

- Player switches to country map, plays country as active scale, exercises minimum head-of-state loop.
- Head-of-state loop (minimum): assign national budget across small fixed category set, launch at least one national infrastructure project propagating down to region/city, create at least one new region node.
- Country policy change while active baked into region and city evolution parameters on switch-down.
- Country evolution algorithm: deterministic fast-forward via long-period economic drift.
- Player-authored dormant control (minimum): from country view, player sets budget allocation per dormant region.
- Save/load preserves all three scales end-to-end in relational schema.
- Parity-budget checks for country evolution algorithm.
- Region/city events bubbled up at switch-out visible in country dashboard (plain text summary sufficient).

**Art:** Country cell sprites, region-node visual at country zoom, country UI elements, head-of-state UI.

---

## Existing backlog issues — role per step

Source of truth: `BACKLOG.md`. Only MVP-critical roles listed.

### Step 2 — City MVP close

| Issue | Role |
|---|---|
| `BUG-55` | Crashers + data corruption + sim logic |
| `BUG-16` | Geography/TimeManager init race |
| `BUG-17` | `cachedCamera` null in `ChunkCullingSystem` |
| `BUG-14` | Per-frame `FindObjectOfType` in UI |
| `FEAT-51` (scoped) | Minimal data dashboard + chart set |
| `TECH-82` Phase 1 | `city_metrics_history` |

### Step 3 — Multi-scale infrastructure

| Issue | Role |
|---|---|
| `TECH-38` | Pure compute modules |
| `TECH-15` | Geography init performance |
| `TECH-34` | `GridManager` region manifest |
| `TECH-82` Phases 2→4 | `city_events`, `grid_snapshots`, `buildings` identity |
| `TECH-18` | Postgres IA migration (relational) |
| `TECH-31` | Scenario generator (extended for snapshot + round-trip) |
| `FEAT-46` | Geography / parameter pipeline (feeds procedural generator) |

### Step 4 — Region MVP

| Issue | Role |
|---|---|
| `FEAT-09` | Trade / production / salaries (re-scoped to inter-city at region) |
| `FEAT-47` | Multipolar — region-level conurbation only |

---

## New feature rows to file

File under `§ Multi-scale simulation lane` in `BACKLOG.md` during backlog triage pass. Do **not** file earlier.

**Step 1:** (1) Parent-scale stub — `region_id` + `country_id` refs, neighbor-city stub, interstate-border semantics. (2) Cell-type split — `Cell` → `CityCell` / `RegionCell` / `CountryCell`.

**Step 3:** (3) `SimulationScale` enum + `ISimulationModel` contract. (4) Per-scale snapshot schema (city first). (5) Multi-scale relational save schema. (6) Single shared real-time clock. (7) Scale-switch-time event bubble-up / constraint push-down hooks (minimum). (8) Child-scale entity model. (9) City evolution algorithm (deterministic). (10) Snapshot → live reconstruction. (11) Procedural scale generator. (12) Scale-switch UX — semantic zoom + procedural fog mask + `ScaleToolProvider` + per-scale minimal toolbars + shared-tool strip. (13) Parity budget harness.

**Step 4:** (14) Region sim model (active tick + deterministic evolution + minimum content). (15) Inter-city trade network solver (minimum). (16) Player-authored dormant control — region (budget allocation per child city).

**Step 5:** (17) Country political/policy layer (active tick + deterministic evolution + minimum head-of-state loop). (18) Player-authored dormant control — country (budget allocation per child region). (19) Multi-scale scenario tests (extends `TECH-31`).

---

## Open questions — MVP decisions

Decided questions — reference only. Full post-MVP discussion: `multi-scale-post-mvp-expansion.md` §11.

- **Q-new-29** — Step 1 exit checklist locked at start of Step 1 stage 1.
- **Q-new-30** — Player invariants in `cell_data jsonb` under `player_invariants` subkey. Shaping-event logs deferred with shaping events.
- **Q-new-31** — Scale-switch hook baselines: `IChildScaleEntity.ApplyPendingDelta(snapshot, deltaParams) → snapshot'`, `IScaleSwitch.Out/In`. Concrete C# shapes lock when Step 3 stage 3 opens.
- **Q-new-32 through Q-new-38** — All deferred to post-MVP. See `multi-scale-post-mvp-expansion.md` §11.

---

## Pointers for fresh agent

1. **`ia/specs/game-overview.md`** — vision + design principles.
2. **`ia/projects/multi-scale-post-mvp-expansion.md`** — what is **not** in MVP.
3. **`BACKLOG.md`** — skim `§ Multi-scale simulation lane`, `§ Compute-lib program`, `§ Agent ↔ Unity & MCP context lane`, `§ High Priority`.
4. **`CLAUDE.md` + `ia/rules/invariants.md`** — hard rules.
5. **`ia/specs/simulation-system.md`** (via MCP `spec_section`) — current single-scale tick loop.
6. **`ARCHITECTURE.md`** — runtime layers, dependency map.
7. **`ia/rules/project-hierarchy.md`** — step/stage/phase/task semantics.
8. **`ia/rules/orchestrator-vs-spec.md`** — this doc is an orchestrator (permanent, not closeable).
9. **`docs/scale-switch-ux-exploration.md`** — Semantic zoom transition + per-scale tool panel design (committed to Step 3).
10. **Brainstorm seed history** in git only — `chore: brainstorm*` commits on `feature/multi-scale-plan`.

**Do:**

- Propose edits to step skeletons when a stage exposes a missing load-bearing item.
- Push MVP-scope-creep into `multi-scale-post-mvp-expansion.md`.
- Create step/stage orchestrators lazily when parent enters "in progress".

**Do not:**

- Resurrect N-tick aggregate publish model. Dormant scales evolve only via deterministic evolution algorithm.
- Resurrect time-dilation framing. Single shared real-time clock.
- Resurrect single-jsonb save tree. Save is relational.
- Resurrect NPC leader modeling. Player is the only actor in MVP.
- Reintroduce climate, shaping events, defense structures, expropriation, agricultural zones, progressive loading, shared cross-scale dashboard, auto mode, scale unlock, or process-engineering gap closures into MVP stages. All post-MVP.
- File BACKLOG rows for new FEAT ideas outside backlog triage pass.
- Give time estimates.

