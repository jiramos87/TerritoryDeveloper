# Multi-Scale Simulation — Master Plan (MVP)

> **Status:** Global orchestrator for the three-scale MVP. Promoted from a brainstorm seed on 2026-04-11; seed history lives in git (see the `chore: brainstorm*` commits on `feature/multi-scale-plan`).
>
> **Scope rule:** This document holds **only** the minimum load-bearing work to prove that the three-scale game loop (city ↔ region ↔ country with dormant evolution + reconstruction) functions end-to-end. Everything else lives in `multi-scale-post-mvp-expansion.md`.
>
> **Authoritative sources (read first if landing cold):**
> - `BACKLOG.md` (open) · `BACKLOG-ARCHIVE.md` (history)
> - `CLAUDE.md` · `AGENTS.md` · `ARCHITECTURE.md`
> - `ia/rules/invariants.md` · `ia/specs/glossary.md` · `ia/specs/simulation-system.md`
> - `docs/information-architecture-overview.md`
> - Deferred work: `ia/projects/multi-scale-post-mvp-expansion.md`
> - MCP: prefer `mcp__territory-ia__*` (`backlog_issue`, `router_for_task`, `spec_section`, `invariants_summary`) over full file reads.

---

## 1. Vision [frozen]

**Territory Developer** is a deep, low-fidelity city-builder that lets the player move **up and down through simulation scales**, each with its own model, vocabulary, and tempo:

```
city  →  region  →  country   (MVP target)
world  →  solar   (post-MVP)
```

- **Deep, not wide.** Cheap visuals, long run times, many interlocking systems.
- **One active scale at a time.** The scale the player views runs its full simulation loop; other scales are dormant (no live tick).
- **Dormant scales evolve algorithmically at scale-switch time.** Evolution is a pure function `evolve(snapshot, Δt, params) → snapshot'`, owned by the parent-scale entity.
- **Three scales is the minimum target.** Two scales admits shortcuts; three forces the architecture to generalize to N.
- **Every scale runs on the same real-time calendar.** No per-scale tick periods, no dilation.

The player can in principle play as mayor, governor, or head of state using the same codebase and a different active scale.

---

## 2. MVP scope cut — what is in, what is out

**In scope (must ship for MVP to mean anything):**

- Parent-scale conceptual stubs inside the city (`region_id`, `country_id`, neighbor-city stub, cell-type split).
- City stability (crashers, data corruption, init races).
- Minimum observability (read current sim state at a glance).
- Pure-compute foundation + relational multi-scale save schema.
- Scale-neutral spine: `SimulationScale` enum, `ISimulationModel` contract, single shared real-time clock, child-scale entity model, scale unlock mechanism, scale-switch UX (top-bar button).
- Deterministic city / region / country evolution algorithms + snapshot → live reconstruction + procedural scale generator.
- Parity budget harness (empirical, playtest-driven).
- Minimum region + country active-scale loops + minimum head-of-state loop.
- Minimum player-authored dormant control (one parameter surface per parent scale).
- Multi-scale scenario test harness.

**Out of scope (deferred to `multi-scale-post-mvp-expansion.md`):**

- Climate v1 (biome, seasonal cycle, weather events, agricultural zones).
- Shaping events / disasters / defense structures / expropriation / per-element construction-plan models.
- Economic depth polish (districts, services coverage, density evolution, pollution depth).
- City shape polish (growth ring gradient tuning, multipolar centroids).
- Player-agency QoL (area demolition, forest hold-to-place).
- Performance envelope measurement beyond what parity harness needs.
- Progressive scale-switch loader (dashboard-first, chunked map). MVP ships a plain loading screen.
- Auto mode at region / country (dormant evolution already runs hands-off; active-scale auto mode is post-MVP).
- Shared cross-scale dashboard with per-scale read/write permissions. MVP ships one dashboard per active scale.
- Cross-scale inter-city tax transfer mechanic.
- Extended player-authored dormant control parameter surface (keeps only the minimum per scale).
- Process-engineering gap closures #2, #3, #5, #10 from brainstorm §2.20 (spec-reviewer subagent, per-phase risk checklist, per-phase budget, S/M/L spec sizing). MVP keeps current lifecycle skills plus the orchestrator distinction.
- NPC leaders / scale sovereigns (fully deferred — player is the only actor).
- World + solar scales, tech tree, epidemic propagation, fog of war, culture drift.

---

## 3. Glossary seeds (MVP core)

Provisional terms used throughout this plan. Promote to `ia/specs/glossary.md` when each stabilizes.

- **Simulation scale** — named level of the simulation stack (`CITY`, `REGION`, `COUNTRY`). Enum + `ISimulationModel` contract.
- **Active scale** — the single scale currently running its full tick loop.
- **Dormant scale** — any scale that is not active. Holds a snapshot + evolution parameters. Does not tick. Evolution is applied by its **parent-scale entity**, not by itself.
- **Child-scale entity** — representation of a dormant child inside its parent. A region holds one entity per dormant city; a country holds one per dormant region. Carries a **pending evolution delta** layered over the child's last-materialized snapshot and a `last_active_at` calendar stamp.
- **Evolution algorithm** — pure function `evolve(snapshot, Δt, params) → snapshot'` that fast-forwards a dormant scale at scale-switch time. **Deterministic in MVP** (no shaping-events channel). Scale-specific: city, region, country.
- **Evolution parameters** — tunable inputs to an evolution algorithm for a given scale node: growth coefficients, policy multipliers, RNG seed, and (at region/country) player-authored parameters set from the parent scale's UI.
- **Evolution-invariant** — state the evolution algorithm must preserve verbatim. For the city: everything the player actively touched (main road backbone, landmarks, districts, player-assigned budgets, explicit zoning decisions). Evolution may **additively** create new main roads or density, but may not overwrite or remove a player-touched surface.
- **Evolution-mutable** — state the algorithm may rewrite: default-generated density, untouched cells, population mix, zoning not explicitly chosen.
- **Parity budget** — maximum allowed divergence between an algorithmic projection and a live-sim re-run over the same interval. **Measured empirically via playtest**, not a single static threshold.
- **Reconstruction** — materializing a playable live scale state from its snapshot + the parent entity's pending evolution delta up to "now". Happens at scale-switch time.
- **Procedural scale generation** — creation of a never-visited scale node (city, region) from parent-scale parameters + deterministic seed.
- **Scale switch** — UI-driven transition from one active scale to another via a top-bar button panel analogous to the existing speed-control panel. Steps: (a) save leaving scale, (b) apply entering scale's pending evolution delta, (c) lazy-load the entering scale into playable form. MVP ships a plain loading screen; progressive load is post-MVP.
- **Scale unlock** — persisted per-save state recording which scales the player has earned access to. Starts with `CITY` unlocked only. Trigger metric: **population threshold** (MVP drops the "% map usage" part; single-metric unlock is enough to prove the mechanic).
- **Multi-scale save tree** — relational: a main `game_save` table + per-scale tables (`city_nodes`, `region_nodes`, `country_nodes`), each with a JSON column for cell data + typed foreign-key columns for structural links + `evolution_params jsonb` + `pending_delta jsonb` + `last_active_at`.
- **City cell / Region cell / Country cell** — scale-specific refinements of the generic `Cell`. Same isometric primitive, sized and semantically typed per scale. The refactor executes in Step 0.
- **Parent-scale stub** — minimum conceptual representation of a parent scale inside the city MVP: `region_id` + `country_id` references + at least one neighbor-city stub + interstate-border data semantics admitting a region-facing interpretation. Shipped in Step 0.
- **Scale-switch event bubble-up / constraint push-down** — event and parameter transport across scales, applied **at switch time** (not continuously). MVP ships both as thin hooks.
- **Player-authored dormant control (minimum)** — at region scale, player sets **budget allocation per dormant child city**. At country scale, player sets **budget allocation per dormant region**. Extended parameter surface is post-MVP.

---

## 4. Master plan skeleton

Five blocks: **Step −1 → Step 0 → Step 1 → Step 2 → Step 3**.

### Step −1 — Process & lifecycle preparation (minimum)

**Goal:** Land the minimum agent/process infrastructure the rest of the plan depends on. Scope is intentionally tight: only the lifecycle-skill distinction between orchestrator docs and project specs, plus the backlog triage pass, plus this document's creation.

**Exit criteria:**

- Lifecycle skills (`project-spec-kickoff`, `project-spec-implement`, `project-spec-close`, `project-stage-close`, `project-new`) understand the orchestrator-vs-project-spec distinction and refuse to `closeout` an orchestrator document.
- A new orchestrator-close recipe exists (or `project-stage-close` is extended to cover it).
- Backlog triage pass executed: rows obsoleted by multi-scale are deleted, surviving rows are rewritten, new rows under `§ Multi-scale simulation lane` are filed.
- `ia/projects/multi-scale-master-plan.md` (this doc) exists and is linked from the brainstorm seed.

**Stages:**

1. **Lifecycle skill rewrite (minimum).** Teach `project-spec-*` skills the orchestrator distinction. Add an orchestrator-close recipe.
2. **Backlog triage pass.** Single dedicated session walking the full BACKLOG against this plan. Mark rows for deletion / rewrite / new-file. File all new `§ Multi-scale simulation lane` rows.
3. **Global orchestrator creation.** This document exists as of creation time; stage closes when Step −1 lessons are logged.

> Process-engineering gaps #2 (cross-review subagent), #3 (per-phase risk checklist), #5 (per-phase budget), #10 (S/M/L sizing) are **deferred to post-MVP**. Current skills are sufficient to ship the MVP.

### Step 0 — Parent-scale conceptual stubs

**Goal:** Make parent region and parent country **visible in city code and save data** before the city MVP is closed. No playable region or country yet.

**Exit criteria (locked at start of Step 0 stage 1, per Q-new-29 decision):**

- Every city save carries a non-null `region_id` and `country_id` (placeholder values allowed).
- At least one **neighbor-city stub** is present at an interstate border and is readable by the city sim (inert content).
- Interstate connections in the city data model admit a "flow to/from a parent-region neighbor" interpretation.
- Save/load round-trips the above with no regression.
- **Cell-type split refactor executed**: `Cell` API splits into `CityCell` / `RegionCell` / `CountryCell` (or equivalent), city sim builds and runs against the new types with no behavior regression.

**Stages:**

1. **Parent-id wiring.** Add `region_id` / `country_id` to city save data; default to placeholder constants; verify load / save round trip.
2. **Neighbor-city stub.** Surface at least one neighbor-city stub at an interstate border as readable inert content; cover it with a test-mode scenario.
3. **Interstate border semantics audit.** Audit current interstate-border code; record minimal change so it reads as "flow to/from a parent-region neighbor". May or may not ship code changes.
4. **Cell-type refactor execution.** Split the `Cell` API into scale-specific types. Stage exits when the city sim builds and runs against the new types with no regression.

> The world-climate pointer from the brainstorm's Step 0 is **dropped** — climate is fully post-MVP, so the pointer has nothing to point at. Reintroduce it only when climate work begins.

### Step 1 — City MVP close (minimum)

**Goal:** The city scale is **stable enough and readable enough** to serve as an aggregation source and a reconstruction target. Not "the city feels like a finished city-builder loop" — that was the brainstorm's larger ambition and is deferred to post-MVP. MVP only requires: no crashers, no data corruption, and a dashboard the sim can be verified against.

**Exit criteria:**

- No crasher or data corruption bug open at city scale.
- Player can read current city state at a glance (minimal dashboard + a handful of charts).
- A single city tick is cheap enough that **running one dormant city alongside one active city is credible**. Concrete target set in Step 2 stage 4 parity harness work.
- Parent-scale stubs from Step 0 are **consumed** by at least one city system (neighbor-city stub influences one demand signal, or interstate border reads as a region-flow edge).

**Stages:**

1. **Stability.** Close `BUG-55` (crashers + data corruption + sim logic), `BUG-16` / `BUG-17` (init races), `BUG-14` (per-frame `FindObjectOfType` in UI). City must not lose or corrupt state.
2. **Minimum observability.** `FEAT-51` scoped down — ship a minimal data dashboard + a small chart set that covers population, money, happiness, and one more sim-internal signal. `TECH-82` Phase 1 (`city_metrics_history`) to back it. Prerequisite for trusting any future aggregate.
3. **Parent-stub consumption.** At least one city system reads the Step 0 parent-scale stubs for real. Pick the lowest-cost coupling available.

> **Deferred to post-MVP:** `BUG-48` (minimap stale), `BUG-52` (AUTO zoning grass gaps), `BUG-20` (utility building load visual), `BUG-28` / `BUG-31` (sorting / prefab). `FEAT-52` services coverage. `FEAT-08` density evolution + spatial pollution. `FEAT-53` districts. `FEAT-43` growth ring gradient. `FEAT-47` multipolar centroids. `FEAT-35` area demolition. `FEAT-03` forest hold-to-place. `TECH-16` sim tick perf v2. `TECH-26` prevention scanner. Climate v1 + shaping-events stub.

### Step 2 — Multi-scale infrastructure (minimum)

**Goal:** Build the **scale-neutral spine**. After Step 2, the repo contains: pure-compute sim modules, `SimulationScale` enum + model contract, per-scale snapshot schema (city first), relational multi-scale save, a **deterministic city evolution algorithm** owned by a parent-region entity (pure function), snapshot-based freeze/reconstruct path, procedural-scale generator, scale unlock mechanism, scale-switch UX skeleton — all exercised by exactly one scale (city), which plays the role of both "active" and "evolved dormant" in isolation tests.

**Exit criteria:**

- A city can be put into dormant mode (snapshot + evolution params held by a parent-region entity), advanced by the city evolution algorithm over an arbitrary Δt, and reconstructed into a playable live city such that the divergence from a full live-sim run over the same interval stays inside the **parity budget** (measured empirically via playtest).
- A procedural city can be generated from region-like parameters + seed and loaded as a normal `GameSaveData`.
- The save format holds cities inside a region inside a country node in the relational schema, even if region and country evolution algorithms are stubs.
- Switching out of a city and back in round-trips correctly: snapshot → parent-entity pending delta (Δt = 0) → reconstruct produces the same city state modulo parity budget.
- Scale unlock state persists and is queryable; Step 2 ships with "only `CITY` unlocked" as the default. Unlock trigger uses the **population** threshold alone in MVP.
- Compute-lib modules are callable headless for at least one non-trivial city sub-system **and** for the city evolution algorithm.
- Every scale reads the **same real-time calendar** via a single shared clock API.

**Stages:**

1. **Pure-compute foundation.** `TECH-38` (core compute modules, non-negotiable), `TECH-15` (geography init performance via compute-lib), `TECH-34` (`GridManager` region manifest). Precondition for running a city tick and a headless city evolution algorithm.
2. **Entity model + relational save.** `TECH-82` Phases 2→4 (`city_events` for live-sim observability, `grid_snapshots`, `buildings` identity). `TECH-18` resolved to **yes, relationally**: main `game_save` table + per-scale tables (`city_nodes`, `region_nodes`, `country_nodes`) with `cell_data jsonb`, `evolution_params jsonb`, `pending_delta jsonb`, `last_active_at` columns. **Open columns decided (Q-new-30, MVP):** player-assigned invariants live inside the node's `cell_data jsonb` under a dedicated `player_invariants` subkey (no sibling table); shaping-event logs are **not in MVP** so the decision on their location is deferred with shaping events themselves. New TECH issue owns the save migration.
3. **Scale-neutral spine.**
   - `SimulationScale` enum + `ISimulationModel` contract (new FEAT).
   - Per-scale snapshot schema, city first (new FEAT).
   - Multi-scale relational save schema (new FEAT).
   - Single shared real-time clock — existing game clock promoted to scale-neutral; every scale reads it directly (new FEAT).
   - Scale-switch-time event bubble-up / constraint push-down hooks (new FEAT — scope reduced: no continuous bus, only switch-time projection, minimum surface).
   - Scale unlock state persisted per save + unlock API (new FEAT).
   - Child-scale entity model — parent-owned representation of a dormant child with pending delta and `last_active_at` (new FEAT).
   - **Scale-switch hook function signatures (Q-new-31 decision, MVP baseline):** `IChildScaleEntity.ApplyPendingDelta(snapshot, deltaParams) → snapshot'`, `IScaleSwitch.Out(activeScale, leavingTimestamp) → snapshotBundle`, `IScaleSwitch.In(targetScale, snapshotBundle, parentDelta) → liveState`. Concrete C# shapes lock when stage 3 opens.
4. **City evolution algorithm + reconstruct / generate loop.**
   - City evolution algorithm — deterministic `evolve(snapshot, Δt, params) → snapshot'` owned by the parent region entity, with seeded RNG. Operates on evolution-mutable state only; preserves evolution-invariant state verbatim. **No shaping-events channel in MVP.**
   - Snapshot → live reconstruction — rebuild a playable live city from snapshot + pending delta (new FEAT).
   - Procedural scale generator — materialize a never-visited city from parent-scale parameters + seed (new FEAT, consumes `FEAT-46`). Generalizes to region in Step 3.
   - Parity budget harness — empirical comparison via playtest round-trips (new TECH). Gates the stage.
5. **Scale switch UX skeleton (minimum).** Top-bar button panel analogous to the existing speed-control panel, exposing scale selection. Click → save the leaving scale → apply pending delta → lazy-load the entering scale behind a **plain loading screen**. Ships the scale unlock UX entry point (even if only `CITY` is unlocked at this stage). Progressive / chunked / dashboard-first loading is **post-MVP**.
6. **Harness + tests.** `TECH-31` scenario generator extended to emit snapshot-shaped scenarios and parity-budget round-trip scenarios. `agent-test-mode-verify` covers a scale-switch-out → evolve(Δt=N years) → scale-switch-back-in round trip. `docs/agent-led-verification-policy.md` updated if new driver kinds are needed. Test cases run through the existing agent ↔ Unity bridge tooling.

> **Deferred to post-MVP:** `TECH-32` / `TECH-35` research gates, `TECH-81` knowledge graph, `TECH-77` / `TECH-78` / `TECH-79` / `TECH-80` / `TECH-83` IA evolution lane, shaping-events framework promotion, progressive scale-switch loader.

### Step 3 — Region + Country MVP

**Goal:** Region and country become **playable as the active scale**, each with its own live-sim tick loop **and** its own deterministic evolution algorithm. After Step 3, we have the **three-scale MVP**.

**Exit criteria:**

- Player can switch from a city to a region map, see other cities reconstructed from their parent-entity snapshots + pending deltas, play the region as the active scale, and switch back into any city (visited or procedural) without state loss and inside the parity budget.
- Player can switch to a country map, play the country as the active scale, and exercise a **minimum head-of-state loop**: assign a national budget across a small fixed set of categories, launch at least one national infrastructure project that propagates down to a region/city, create at least one new region node.
- A country policy change made while the country is active is **baked into region and city evolution parameters** the next time the player switches down.
- A city event that occurred during live city play is **bubbled up at switch-out time** and visible in the region and country dashboards (plain text summary is enough — rich dashboarding is post-MVP).
- Save/load preserves all three scales end-to-end in the relational schema.
- Region unlock and country unlock are exercised: player starts with only `CITY` unlocked, crosses the region unlock population threshold, then the country unlock population threshold.
- At least one **economic flow** crosses scales: city exports feed inter-city trade in the region layer's live sim, and the resulting balance feeds back into city evolution parameters on switch-down.
- Region has **one natural resource type** and supports **founding new cities**. Multi-climate geography, coastal/polar dimensions, and richer resource systems are post-MVP.
- Country owns **national budget** and the **minimum head-of-state loop** above. International relations, war/border expansion, and natural-resource-as-national-priority are post-MVP.
- Player-authored dormant control is live **at the minimum surface**: from the region view, player sets budget allocation per dormant child city; from the country view, player sets budget allocation per dormant region. Extended parameter surfaces are post-MVP.

**Stages:**

1. **Region sim model (active + evolution, minimum).** Coarse region grid/graph using region cells; nodes = city child-entities, farmland, wilderness; edges = road lanes. Active-scale region tick: migration pressure, basic trade flow, founding new cities. Region evolution algorithm: deterministic `evolve` analogous to the city algorithm, operates on region evolution-mutable state. Re-scopes `FEAT-09` (trade / production / salaries) to the region active-tick layer. Consumes `FEAT-47` multipolar only as it applies to region-level conurbation.
2. **Country sim model (active + evolution + minimum head-of-state loop).** Political/policy layer using country cells. Active-scale country tick: advances budget execution, national infrastructure projects, region creation. Country evolution algorithm: deterministic fast-forward via long-period economic drift. **No NPC-leader modifiers.** Head-of-state loop (minimum playable): assign national budget across a small fixed category set; launch at least one national infrastructure project that touches one or more child regions/cities; create a new region node and fix its initial evolution parameters (consumes the procedural scale generator from Step 2 stage 4).
3. **Inter-scale wiring (scale-switch-time).** Minimum event bubble-up + constraint push-down across all three scales, applied at switch time. All non-active cities in the active region stay dormant and are evolved on demand via their parent region's child-scale entities. Per-scale dashboards surface **active-scale-only** stats in MVP; shared cross-scale dashboard is post-MVP.
4. **Procedural content at region scale.** Unvisited region nodes generate plausible cities on demand (Step 2 generator, now driven by region sim parameters). Minimum coverage; richer region content (`FEAT-15` ports, `FEAT-16` trains, `FEAT-39` sea, `FEAT-10` regional bonus) is post-MVP.
5. **Player-authored dormant control (minimum surface).** Region UI exposes **budget allocation per dormant child city**. Country UI exposes **budget allocation per dormant region**. That is the entire MVP surface. Extended parameters (resource allocation, energy-grid connections, transport-line connections, designate regional capital, inter-region transport priorities, national infrastructure project routing, regional autonomy, war/peace) are post-MVP.
6. **Multi-scale save, load, and test harness.** `TECH-31` scenario generator extended to multi-scale scenarios; compile-gate + test mode driver coverage for three scales via the existing agent ↔ Unity bridge tooling; parity-budget checks for region and country evolution algorithms. Verification policy updated.

> **Deferred to post-MVP:** multi-climate geography, natural-resource-as-national-priority, international relations, war/border expansion, auto mode at region/country, extended dormant control parameter surface, shared cross-scale dashboard, per-scale playability polish pass, `FEAT-15` ports, `FEAT-16` trains, `FEAT-14` vehicle traffic, `FEAT-39` sea/shore band, `FEAT-40` water sources/drainage, `FEAT-48` water body volume, `FEAT-10` regional bonus, `TECH-83` sim parameter tuning.

---

## 5. Document hierarchy

Four levels, loosely bound.

```
Step        — major product milestone
 └─ Stage   — coherent sub-milestone inside a step
     └─ Phase  — shippable compilable increment (measured in merged PRs)
         └─ Task — atomic file-level unit of work (maps to a BACKLOG row)
```

- **Step and stage are stable.** Scaffold agents navigate by.
- **Phase is semi-stable.** Rewritable until it enters "in progress"; active phase is frozen until it ships.
- **Task is volatile.** Tasks are only fully defined when the phase enters "in progress". Each task corresponds to exactly one BACKLOG row.
- **Learnings flow backward.** Task closes → phase Lessons Learned. Phase closes → stage rollup. Stage closes → step Decision Log → next step's skeleton.

**Orchestrator document hierarchy:**

```
ia/projects/multi-scale-master-plan.md                       ← global orchestrator (this doc)
 └─ ia/projects/multi-scale/step-{N}-{slug}.md                ← stage-level orchestrator per step (lazy)
     └─ ia/projects/multi-scale/step-{N}/stage-{M}-{slug}.md  ← phase-level orchestrator per stage (lazy)
         └─ BACKLOG row (FEAT-/TECH-/BUG-)                    ← task level; lives in BACKLOG.md
```

- A **task** (BACKLOG row) owns its own `ia/projects/{ISSUE_ID}.md` project spec.
- A **phase / stage / step** owns an orchestrator document, not a project spec, and is **not closeable via `closeout`**.
- The global orchestrator (this doc) must never be deleted by a closeout.
- Step and stage orchestrators materialize **lazily**, only when their parent enters "in progress".

---

## 6. Existing backlog issues — role per step (MVP only)

Source of truth is always `BACKLOG.md`. Only **MVP-critical** roles appear here; everything else ships through the post-MVP expansion doc.

### Step 1 — City MVP close

| Issue | Role | Stage |
|---|---|---|
| `BUG-55` | Stability — crashers + data corruption + sim logic fixes | Stability |
| `BUG-16` | Geography/TimeManager init race | Stability |
| `BUG-17` | `cachedCamera` null in `ChunkCullingSystem` | Stability |
| `BUG-14` | Per-frame `FindObjectOfType` in UI | Stability |
| `FEAT-51` (scoped down) | Minimum data dashboard + small chart set | Minimum observability |
| `TECH-82` Phase 1 | `city_metrics_history` | Minimum observability |

### Step 2 — Multi-scale infrastructure

| Issue | Role | Stage |
|---|---|---|
| `TECH-38` | Pure compute modules (non-negotiable) | Pure-compute foundation |
| `TECH-15` | Geography init performance | Pure-compute foundation |
| `TECH-34` | `GridManager` region manifest | Pure-compute foundation |
| `TECH-82` Phases 2→4 | `city_events`, `grid_snapshots`, `buildings` identity | Entity model + relational save |
| `TECH-18` | Postgres IA migration (resolved yes, relationally) | Entity model + relational save |
| `TECH-31` | Scenario generator (extended for snapshot + round-trip scenarios) | Harness + tests |
| `FEAT-46` | Geography / parameter pipeline (feeds procedural generator) | City evolution + reconstruct/generate |

### Step 3 — Region + Country MVP

| Issue | Role | Stage |
|---|---|---|
| `FEAT-09` | Trade / production / salaries (**re-scoped** to inter-city at region scale) | Region sim model |
| `FEAT-47` | Multipolar — region-level conurbation only | Region sim model |

All other existing BACKLOG rows referenced by the brainstorm are deferred to `multi-scale-post-mvp-expansion.md`.

---

## 7. New feature rows to file (MVP only)

File under `§ Multi-scale simulation lane` in `BACKLOG.md` during Step −1 stage 2 (backlog triage pass). Do **not** file earlier.

**Step 0:**

1. Parent-scale stub feature — `region_id` + `country_id` references, neighbor-city stub, interstate-border "flow to neighbor" data semantics.
2. Cell-type split execution — `Cell` → `CityCell` / `RegionCell` / `CountryCell` refactor.

**Step 2 — scale-neutral spine:**

3. `SimulationScale` enum + `ISimulationModel` contract.
4. Per-scale snapshot schema (city first).
5. Multi-scale relational save schema.
6. Single shared real-time clock (scale-neutral promotion).
7. Scale-switch-time event bubble-up / constraint push-down hooks (minimum).
8. Scale unlock state + unlock API.
9. Child-scale entity model.
10. City evolution algorithm (deterministic).
11. Snapshot → live reconstruction.
12. Procedural scale generator.
13. Scale switch UX (top-bar button panel, plain loading screen).
14. Parity budget harness.

**Step 3 — region + country:**

15. Region simulation model (active tick + deterministic evolution algorithm + minimum content).
16. Country political/policy layer (active tick + deterministic evolution algorithm + minimum head-of-state loop).
17. Inter-city trade network solver (minimum).
18. Player-authored dormant control (minimum surface: budget allocation per child).
19. Multi-scale scenario tests (extends `TECH-31`).

Everything else from brainstorm §9 (climate v1, agricultural zones, shaping events, defense structures, expropriation, construction-plan models, progressive loader, auto mode, shared dashboard, inter-city tax transfer, process-engineering gaps) lives in `multi-scale-post-mvp-expansion.md`.

---

## 8. Open questions — MVP decisions

The brainstorm left ten open questions (Q-new-29..Q-new-38). MVP decisions below; full post-MVP discussion lives in `multi-scale-post-mvp-expansion.md`.

- **Q-new-29 (Step 0 exit checklist lock-in moment).** **Locked at start of Step 0 stage 1.** Step −1 may ship without Step 0 being scoped; Step 0 scoping happens at stage 1 opening.
- **Q-new-30 (relational save schema open columns).** **Decided for MVP:** player-assigned invariants live inside each node's `cell_data jsonb` under a dedicated `player_invariants` subkey. Shaping-event logs are not in MVP, so the decision on their location is deferred with shaping events themselves (post-MVP).
- **Q-new-31 (scale-switch hook function signatures).** **Baseline decided:** `IChildScaleEntity.ApplyPendingDelta(snapshot, deltaParams) → snapshot'`, `IScaleSwitch.Out(activeScale, leavingTimestamp) → snapshotBundle`, `IScaleSwitch.In(targetScale, snapshotBundle, parentDelta) → liveState`. Concrete C# shapes lock when Step 2 stage 3 opens.
- **Q-new-32 (inter-city tax transfer mechanic UI).** **Deferred to post-MVP.** MVP ships only budget allocation per child as the player-authored dormant control surface; no direct tax-transfer mechanic.
- **Q-new-33 (country-view dormant control parameter list).** **Deferred to post-MVP.** MVP country-view dormant control is **budget allocation per dormant region** — one parameter, nothing else.
- **Q-new-34 (weather event vs shaping event boundary).** **Moot for MVP.** Both weather events and shaping events are post-MVP. Boundary decision deferred.
- **Q-new-35 (agricultural zone semantic split).** **Moot for MVP.** Agricultural zones are post-MVP. Split decision deferred.
- **Q-new-36 (construction-plan model interface).** **Moot for MVP.** The only evolution-placeable element type in MVP is the existing road (which already has `PathTerraformPlan`). No new per-element models ship in MVP. Interface work is post-MVP.
- **Q-new-37 (process-engineering gap implementation order inside Step −1).** **Moot for MVP.** Load-bearing gaps #2, #3, #5, #10 are **deferred to post-MVP**. Step −1 only ships the lifecycle-skill orchestrator distinction + backlog triage.
- **Q-new-38 (spec-reviewer subagent scope and verdict shape).** **Deferred to post-MVP.** No spec-reviewer subagent in MVP.
- **Q-new-28 (agent-parallelization abstraction).** **Deferred to a separate brainstorm thread**, per the brainstorm's own deferral. Not in MVP.

---

## 9. Pointers for a fresh agent

1. **This document, section 1 and section 2** — vision + MVP scope cut.
2. **`ia/projects/multi-scale-post-mvp-expansion.md`** — what is explicitly **not** in MVP (read so you don't accidentally pull post-MVP scope into Step 1 or Step 2).
3. **`BACKLOG.md`** — skim `§ Multi-scale simulation lane` (created during Step −1 stage 2), `§ Compute-lib program`, `§ Agent ↔ Unity & MCP context lane`, `§ IA evolution lane`, `§ High Priority`.
4. **`CLAUDE.md` + `ia/rules/invariants.md`** — hard rules. Do not skip.
5. **`ia/specs/simulation-system.md`** (via `mcp__territory-ia__spec_section`) — current single-scale tick loop.
6. **`ARCHITECTURE.md`** — runtime layers, dependency map.
7. **Brainstorm seed history** lives in git only — see the `chore: brainstorm*` commits on `feature/multi-scale-plan`. Read for rationale on decisions, not for scope.

**Do:**

- Propose edits to the Step −1 / Step 0 / Step 1 / Step 2 / Step 3 skeletons when a stage exposes a missing load-bearing item.
- Push any surfaced MVP-scope-creep back into `multi-scale-post-mvp-expansion.md` before it lands in a stage.
- Create step and stage orchestrators lazily when their parent enters "in progress".

**Do not:**

- Resurrect the old "N-tick aggregate publish" model. Dormant scales evolve only via the deterministic evolution algorithm.
- Resurrect time-dilation framing. Single shared real-time clock across all scales.
- Resurrect single-jsonb save tree. The save is relational.
- Resurrect NPC leader modeling. Player is the only actor in MVP.
- Reintroduce climate, shaping events, defense structures, expropriation, agricultural zones, progressive loading, shared cross-scale dashboard, auto mode, or process-engineering gap closures into MVP stages. All post-MVP.
- File BACKLOG rows for new FEAT ideas outside Step −1 stage 2 (backlog triage pass).
- Create a `multi-scale-master-plan-navigator` skill. No automated navigator.
- Give time estimates for any step, stage, phase, or task.

---

## 10. Mutation log

Append-only. One line per change.

- **2026-04-11** — Initial MVP cut promoted from the brainstorm seed (now retained only in git history on `feature/multi-scale-plan`, `chore: brainstorm*` commits). Scope slimmed aggressively: climate, shaping events, defense structures, expropriation, construction-plan models, progressive loader, auto mode, shared cross-scale dashboard, city economic-depth polish, city shape polish, QoL features, and process-engineering gap closures all moved to `multi-scale-post-mvp-expansion.md`. Open questions Q-new-29..Q-new-38 decided toward MVP minimum (see §8). Brainstorm file deleted the same day.
