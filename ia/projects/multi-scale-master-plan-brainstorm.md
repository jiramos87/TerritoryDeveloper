# Multi-Scale Simulation — Master Plan Brainstorm

> **Status:** Living brainstorm. **Not** yet an umbrella/tracker. No BACKLOG id yet. Purpose: keep the multi-scale conversation alive across agent sessions until it stabilizes enough to be promoted into a real umbrella project spec + per-step BACKLOG rows.
>
> **Authoritative sources (read these first if you land here fresh):**
> - `BACKLOG.md` (open issues) · `BACKLOG-ARCHIVE.md` (history)
> - `CLAUDE.md` (repo directives) · `AGENTS.md` (workflow) · `ARCHITECTURE.md` (runtime layers)
> - `ia/rules/invariants.md` · `ia/specs/glossary.md` · `ia/specs/simulation-system.md`
> - `docs/information-architecture-overview.md`
> - MCP: prefer `mcp__territory-ia__*` (`backlog_issue`, `router_for_task`, `glossary_discover`, `spec_section`, `invariants_summary`) over full file reads.

---

## 0. How to read and mutate this document

This document is a **seed**, not a contract. It will be rewritten many times as we learn. Rules for mutation:

1. **Immutable by default only where labeled `[frozen]`.** Everything else is fair game to rewrite — but append the change to the **Mutation log** at the bottom with a date and one-line rationale.
2. **Every Step has a Decision Log, Lessons Learned, and Open Questions section.** Lessons from a closed step must be reflected in the next step's skeleton before that next step enters "in progress".
3. **Task granularity inflates downward only when needed.** Don't pre-decompose all tasks for Step 3 while still shipping Step 1 — it will be wrong. Phases and tasks materialize late; steps and stages are the stable scaffolding.
4. **If a section becomes long enough to deserve its own file**, promote it. Suggested promotion path:
   - Brainstorm (this doc, `ia/projects/multi-scale-master-plan-brainstorm.md`)
   - → Umbrella tracker (`ia/projects/multi-scale-master-plan.md`, with step/stage/phase checkboxes, BACKLOG row ids, and `[x]` marks)
   - → Canonical glossary terms + per-step BACKLOG rows (`BUG-` / `FEAT-` / `TECH-`) + per-issue project specs under `ia/projects/{ISSUE_ID}.md`.
5. **Never leave a new term only in this doc.** When a term stabilizes (e.g. `simulation scale`, `city aggregate`, `dormant city`, `scale zoom`, `event bubble-up`, `reconstruction`), promote it to `ia/specs/glossary.md` with a spec pointer. Until promoted, mark the term `(provisional)` inline so agents know it is not canonical.

---

## 1. Vision [frozen]

**Territory Developer** is a deep, low-fidelity city-builder that lets the player move **up and down through simulation scales**, each with its own model, vocabulary, and tempo:

```
city  →  region  →  country  →  world  →  solar
```

- **Deep, not wide.** Cheap visuals, long run times, many interlocking systems.
- **One active scale at a time.** The scale the player is viewing runs its full simulation loop; other scales advance through cheaper aggregate updates.
- **Aggregation flows upward, reconstruction flows downward.** A region never simulates all of its cities in full; it consumes **city aggregates**. When the player zooms back into a city, missing detail is **reconstructed** from the aggregate trajectory (or generated from scratch if never visited).
- **Bidirectional feedback across scales.** Events bubble up (city fire → region news); constraints push down (country policy → city tax multiplier).
- **Three scales is the minimum target for the first multi-scale MVP.** Reason: two scales can always be implemented as a special case; three scales is the smallest number that forces the architecture to generalize to N.

The player can in principle play as **mayor, governor, head of state, or higher** using the same codebase and only a different active scale.

---

## 2. Core design insights (from the brainstorm sessions)

Captured from the conversation turns that produced this doc. Treat these as the "why" behind the plan.

### 2.1. Aggregation is the hinge

The entire vision rests on the fact that **a city can be represented cheaply enough that N of them can run at a coarser scale**. Without a well-defined **city aggregate digest** (population, RCI mix, GDP, happiness, pollution, exports, political lean, culture index, infra score, ...) the region sim has nothing to tick on. Therefore: the digest schema is a **load-bearing artifact**, not a cosmetic one. It must be designed before any region tick is written.

### 2.2. Reconstruction is the test of aggregation

If you can't rebuild a plausible city from its aggregate trajectory, the aggregate is lossy in the wrong places. So the two features are the **same feature seen from opposite sides**: aggregation defines what the region sees, reconstruction defines what the player sees when zooming back in. Design them together.

### 2.3. Three scales forces N-generalization

Two scales (city + region) admits shortcuts: hand-tuned coupling, bespoke save format, direct references. Three scales (city + region + country) forces:

- A scale enum (`SimulationScale`) and a polymorphic tick API.
- A hierarchical save format that doesn't special-case the top.
- An event bus that doesn't know how many levels exist.
- A dashboard that swaps metric sets by scale lookup, not by hardcoded `if/else`.

That extra generalization is cheap to add at three, expensive to retrofit at five.

### 2.4. Multi-scale time must be uniform

Each scale has a natural tick period (city = month, region = year, country = decade, ...). The architecture must treat these as **time dilation of a single world clock**, not as independent timelines. A single authoritative calendar prevents drift, rollback bugs, and "which year is it really" questions.

### 2.5. The existing backlog is not optional baggage

Several existing issues that currently look like "nice to have" become **hard prerequisites** under multi-scale (full mapping in §8). In particular: `TECH-38` compute-lib, `TECH-82` entity model, `TECH-16` tick performance, `TECH-15` geography init performance, `FEAT-47` multipolar centroids, and `BUG-55` stability. Skipping any one of them will show up as a load-bearing gap in Step 2 or Step 3.

### 2.6. The city MVP must be closable

Before we can freeze the city scale as an aggregation source, the city must **look, play, and read** as a finished city-builder loop. That is Step 1. Any gameplay gap left open in Step 1 reappears as "the region aggregate is missing X" during Step 3.

---

## 3. Glossary seeds (provisional — not yet canonical)

These terms are used throughout this doc. When any one stabilizes, promote it to `ia/specs/glossary.md` and delete the `(provisional)` tag here.

- **Simulation scale** (provisional) — a named level of the simulation stack (`CITY`, `REGION`, `COUNTRY`, `WORLD`, `SOLAR`). Enum + `ISimulationModel` contract.
- **Active scale** (provisional) — the single scale currently running its full tick loop; all other scales advance through aggregate steps.
- **City aggregate** / **city digest** (provisional) — compact struct sampled every N city ticks; the unit of inter-scale communication for cities.
- **Aggregate trajectory** (provisional) — time-ordered sequence of aggregates for a single city, used by reconstruction.
- **Dormant city** (provisional) — a city whose full tick loop is frozen; only its aggregate and event delta log advance.
- **Reconstruction** (provisional) — the inverse of aggregation: materializing a plausible full city state from aggregate + seed (and base snapshot, if any).
- **Procedural city generation** (provisional) — creation of a never-visited city's full state from region parameters + deterministic seed.
- **Scale zoom** (provisional) — the UX transition from one active scale to another; also the moment when aggregation/reconstruction run.
- **Event bubble-up** (provisional) — structured event propagation upward through the scale stack.
- **Constraint push-down** (provisional) — parameter propagation downward through the scale stack.
- **Multi-scale save tree** (provisional) — hierarchical save format: root → countries → regions → cities, each node carrying aggregate + optional full detail.

---

## 4. Master plan skeleton

Three steps. Steps 4+ (world, solar, long tail) are explicitly out of the first-playable multi-scale MVP and listed only as "parked" at the end.

### Step 1 — City MVP close

**Goal:** The city scale feels like a finished, stable city-builder loop. No crashes, observable metrics, meaningful gameplay tension, player agency to shape the city. After Step 1, the city model is **frozen enough to be used as an aggregation source** in Step 2 without ongoing churn.

**Exit criteria (provisional):**
- No crasher or data corruption bug open at city scale.
- Player can read, at a glance, how the city is doing (dashboard + charts).
- At least one gameplay tension loop beyond "place zones, money goes up" (service coverage, pollution consequences, districts, ...).
- Cities visually look right (growth gradient, no persistent grass gaps, working minimap).
- A single city tick is cheap enough that the **prospect** of running many aggregated cities in parallel is credible (concrete target to be set in a Step 2 stage, not now).

**Candidate stages (draft — rewrite freely):**

1. **Stability.** Close BUG-55 (crashers + data corruption + sim logic), BUG-48 (minimap stale), BUG-52 (AUTO zoning grass gaps), BUG-20 (utility building load visual), BUG-16/17 (init races), BUG-28/31 (sorting/prefab). The city must not lose or corrupt state.
2. **Observability.** FEAT-51 (data dashboard + charts) + TECH-82 Phase 1 (`city_metrics_history`). Player and designer can both read what the sim is doing. Prerequisite for trusting any future aggregate.
3. **Economic depth & tension.** FEAT-52 (services coverage), FEAT-08 (density evolution + spatial pollution), FEAT-53 (districts), and the already-shipped monthly maintenance / tax→demand loop. Turns the city into a system with stakes.
4. **City shape correctness.** FEAT-43 (growth ring gradient tuning), and the parts of FEAT-47 (multipolar centroids) that are still city-internal. The city must look like a city to a human eye before it can be a node in a region.
5. **Quality-of-life player agency.** FEAT-35 (area demolition), FEAT-03 (forest hold-to-place), and the smaller QoL features that stop manual editing from hurting.
6. **Performance envelope for later aggregation.** TECH-16 (tick perf v2, harness labels), BUG-14 (per-frame `FindObjectOfType`), TECH-26 (prevention scanner). Measure where the single-city tick spends its time. Don't micro-optimize yet, but produce the harness.

### Step 2 — Multi-scale infrastructure (minimum)

**Goal:** Build the **scale-neutral spine** needed to support any number of scales, **without yet adding a second playable scale**. After Step 2, the repo contains: pure-compute sim modules, a `SimulationScale` enum and model contract, a city aggregate digest, a hierarchical save tree, an event bubble-up bus, a dormant-city freeze/reconstruct path, a procedural-city generator, and a scale zoom transition UX skeleton — all exercised by **exactly one scale (city)**, which now plays the role of both "active" and "aggregated" in isolation tests.

**Exit criteria (provisional):**
- A city can be put into **dormant mode**, advanced N aggregate ticks, and reconstructed back into a plausible live city with bounded divergence.
- A procedural city can be generated from region-like parameters + seed and loaded as a normal `GameSaveData`.
- The save format holds cities inside a region inside a country node, even if region and country tick loops are stubs.
- The event bus carries at least one event type end-to-end through the stack (e.g., a city fire event propagates to a region-level log sink, even if the sink is inert).
- Compute-lib modules are callable headless (without Unity) for at least one non-trivial city sub-system.

**Candidate stages (draft):**

1. **Pure-compute foundation.** TECH-38 (core compute modules), TECH-15 (geography init performance via compute-lib), TECH-34 (`GridManager` region manifest), TECH-32/TECH-35 research as gated spikes. Precondition for running a city tick anywhere that isn't the main Unity loop.
2. **Entity model and event log.** TECH-82 Phases 1→4 (metrics history, `city_events`, `grid_snapshots`, `buildings` identity), TECH-81 (knowledge graph, if scale dependency queries justify it), TECH-18 decision (Postgres IA migration or stay file-backed for now).
3. **Scale-neutral spine.**
   - `SimulationScale` enum + `ISimulationModel` contract (new FEAT).
   - City aggregate digest schema (new FEAT).
   - Multi-scale save tree format (new FEAT).
   - Event bubble-up / constraint push-down bus (new FEAT).
   - Multi-scale time dilation clock (new FEAT).
4. **Dormant-reconstruct-generate loop.**
   - Dormant city freeze + delta log (new FEAT, consumes TECH-82 Phase 2).
   - Aggregate → detail reconstruction (new FEAT).
   - Procedural city generator (new FEAT, consumes FEAT-46 parameter pipeline).
5. **Scale zoom UX skeleton.** Camera / UI path to zoom "out" from the city to a stub region view and back. Lo-fi region view is inert (no region sim yet) but the transition runs the aggregation/reconstruction round trip against real city state.
6. **Harness + tests.** TECH-31 scenario generator extended to emit aggregate-shaped scenarios; `agent-test-mode-verify` covers a dormant→reconstruct round trip. `docs/agent-led-verification-policy.md` updated if new driver kinds are needed.

### Step 3 — Region + Country MVP

**Goal:** Two additional scales (region and country) run real tick loops, consume city aggregates, produce cross-scale feedback, and are playable as scale-locked experiences. After Step 3, we have the **three-scale MVP** — proof that the architecture generalizes.

**Exit criteria (provisional):**
- Player can zoom out from a city to a region map, see other aggregated cities, watch region sim tick, and zoom back in to any city (visited or procedural) without state loss.
- Player can zoom out further to a country map, see multiple regions, see at least one country-level policy knob propagate down as a city sim parameter, and see at least one city event propagate up as a country-level statistic.
- Dashboard adapts metrics to the active scale (extension of FEAT-51).
- Save/load preserves all three scales end-to-end.
- At least one **economic flow** crosses scales: city exports feed inter-city trade in the region layer and change city demand on return.

**Candidate stages (draft):**

1. **Region sim model.** Coarse region grid/graph; nodes = city aggregates, ports, farmland, wilderness; edges = road/rail/sea lanes. Region tick: migration pressure, trade flow solver, regional economy. Re-scopes FEAT-09 (trade/production/salaries) to the region layer. Consumes FEAT-47 multipolar for connurbation.
2. **Country sim model.** Political/policy layer: elections, budget, tax, trade deals, immigration. Country tick: produce parameter deltas that push down. Consume region stats to push up.
3. **Inter-scale wiring.** Full event bubble-up + constraint push-down across all three scales. Scale-aware dashboard (FEAT-51 extension). Dormant-city freeze wired for all non-active cities in the active region.
4. **Procedural content at region scale.** Unvisited region nodes generate plausible cities on demand (Step 2 generator, now driven by region sim parameters). FEAT-15 ports + FEAT-16 trains + FEAT-39 sea + FEAT-10 regional bonus evolved into real region-layer content.
5. **Playability pass.** Each scale has a minimum player loop: at region scale the player influences migration/trade/infrastructure; at country scale the player sets policy and budget. Not deep — deep is for later — but **coherent**.
6. **Multi-scale save, load, and test harness.** Scenario generator (TECH-31) extended to multi-scale scenarios; compile-gate + test mode driver coverage for three scales. Verification policy updated.

### Parked (not in the first multi-scale MVP)

- **World scale** — global climate, commodity prices, sea level drift.
- **Solar scale** — epochs, catastrophes, long-term resource depletion.
- **Era / tech tree** shared across scales.
- **Epidemic propagation**, **NPC leaders / agents**, **fog of war / exploration**, **culture drift**, **refugee flows**.
- **Photo mode + time-lapse replay**, **tutorial / onboarding**, **achievements** — player-value items from the prior brainstorm turn that are not load-bearing for multi-scale and should be scheduled against whatever scale is active at the time.

---

## 5. Document hierarchy model

Four levels, loosely bound. Mutable at the top, crystallized at the bottom.

```
Step        — major product milestone (e.g. "City MVP close", "Multi-scale infra", "Region + Country MVP")
 └─ Stage   — coherent sub-milestone inside a step (e.g. "Stability", "Observability", "Economic depth")
     └─ Phase  — shippable compilable increment inside a stage (measured in merged PRs, not calendar time)
         └─ Task — atomic file-level unit of work (maps to a BACKLOG row — BUG / FEAT / TECH / ART / AUDIO)
```

**Rules:**

- **Step and stage are stable.** They can be reworded, split, merged — but they are the scaffolding agents navigate by. Treat them as "chapter headings": rewrite rarely.
- **Phase is semi-stable.** A phase either (a) has not started, in which case it can be rewritten freely, or (b) is "in progress", in which case only the active phase's scope is frozen until it ships. Future phases inside the same stage remain advisory.
- **Task is the volatile tier.** Tasks inside a not-yet-started phase are strongly advisory — a single sentence pointing at a BACKLOG id is enough. Tasks are only fully defined when the phase enters "in progress", and each task should correspond to exactly one BACKLOG row (existing or newly filed).
- **Learnings flow backward.** When a task closes, a one-line Lesson Learned is appended to its phase. When a phase closes, its lessons roll up into the stage. When a stage closes, lessons roll up into the step's Decision Log and feed the next step's skeleton.

---

## 6. Proposed orchestrator / tracker artifact

Once the skeleton above stabilizes enough to stop churning (likely after Step 1's stages are locked), promote it into a sibling file:

- **Path:** `ia/projects/multi-scale-master-plan.md`
- **Shape:** umbrella tracker with step → stage → phase → task checkboxes, each task row citing its BACKLOG id, each phase row citing a PR link or commit range once closed, each stage/step row carrying a Decision Log + Lessons Learned block.
- **Relation to this doc:** the tracker is the **current state**; this brainstorm is the **rationale**. When the two disagree, the tracker wins for "what is true now" and this brainstorm wins for "why did we decide this".
- **Relation to BACKLOG.md:** the tracker **does not replace** `BACKLOG.md`. Every task in the tracker must map to a BACKLOG row, and BACKLOG remains the authoritative priority/status source. The tracker is a cross-cutting view on top of BACKLOG.
- **New BACKLOG rows generated by this plan:** file them under a new section heading in `BACKLOG.md` — proposed name **§ Multi-scale simulation lane** — ordered between the existing § Gameplay & simulation lane and § High priority. Give them normal `FEAT-` / `TECH-` ids in sequence.

---

## 7. Proposed new skill

A new skill would make the plan navigable without having to re-read all three documents every session.

- **Path:** `ia/skills/multi-scale-master-plan-navigator/SKILL.md` (plus `.claude/skills/multi-scale-master-plan-navigator` symlink per repo convention).
- **Triggers:** "master plan", "what's next in the multi-scale plan", "advance step X", "close phase Y", "inject lesson from task Z".
- **Tool recipe:**
  1. Read `ia/projects/multi-scale-master-plan.md` (tracker). If it does not exist, read `ia/projects/multi-scale-master-plan-brainstorm.md` (this doc) and warn that the plan is still in brainstorm stage.
  2. Use `mcp__territory-ia__backlog_search` / `backlog_issue` to resolve cited BACKLOG ids.
  3. Use `mcp__territory-ia__router_for_task` to pull relevant spec slices for the active phase.
  4. Emit: current step, current stage, current phase, list of open tasks with BACKLOG ids, and the next recommended action.
  5. On "close phase", append Lessons Learned to the phase block, mark the phase `[x]`, and propose whether the next phase is ready to start (or whether the stage rolls up).
- **Non-goals:** does **not** implement code, does **not** close BACKLOG issues (that is `closeout`), does **not** run Verification (that is `/verify`).
- **Relation to existing skills:** this skill sits above `project-spec-kickoff`, `project-spec-implement`, `project-spec-close`, `project-stage-close`, and `agent-test-mode-verify` — it chooses **which** of them to dispatch next based on plan state.

---

## 8. Existing backlog issues — role per step

This is the bridge between the current BACKLOG and the new plan. Source of truth for status is always `BACKLOG.md`; this table only records the **role** each issue plays.

### Step 1 — City MVP close

| Issue | Role | Stage |
|---|---|---|
| `BUG-55` | Stability — crashers + data corruption + sim logic fixes | Stability |
| `BUG-52` | AUTO zoning grass-gap regression | Stability |
| `BUG-48` | Minimap stale refresh | Stability |
| `BUG-20` | Utility building load visual | Stability |
| `BUG-16` | Geography/TimeManager init race | Stability |
| `BUG-17` | `cachedCamera` null in ChunkCullingSystem | Stability |
| `BUG-28` | Slope/interstate sorting order | Stability |
| `BUG-31` | Interstate border prefabs | Stability |
| `BUG-14` | Per-frame `FindObjectOfType` in UI | Performance envelope |
| `FEAT-51` | Game data dashboard + charts | Observability |
| `TECH-82` (Phase 1) | `city_metrics_history` | Observability |
| `FEAT-52` | City services coverage | Economic depth & tension |
| `FEAT-08` | Zone density evolution + spatial pollution | Economic depth & tension |
| `FEAT-53` | Districts | Economic depth & tension |
| `FEAT-09` | Trade / production / salaries (**re-scope** to inter-city at Step 3) | Parked pending Step 3 re-scope |
| `FEAT-43` | Urban growth ring gradient | City shape correctness |
| `FEAT-47` | Multipolar centroids (city-internal portions) | City shape correctness |
| `FEAT-35` | Area demolition drag | Player agency QoL |
| `FEAT-03` | Forest hold-to-place | Player agency QoL |
| `TECH-16` | Sim tick perf v2 + harness | Performance envelope |
| `TECH-26` | Per-frame `FindObjectOfType` scanner | Performance envelope |

### Step 2 — Multi-scale infrastructure (minimum)

| Issue | Role | Stage |
|---|---|---|
| `TECH-38` | Pure compute modules (non-negotiable) | Pure-compute foundation |
| `TECH-15` | Geography init performance | Pure-compute foundation |
| `TECH-34` | `GridManager` region manifest | Pure-compute foundation |
| `TECH-32` | Urban growth ring what-if tooling | Pure-compute foundation (research gate) |
| `TECH-35` | Property-based invariant fuzzing | Pure-compute foundation (research gate) |
| `TECH-82` (Phases 2–4) | `city_events`, `grid_snapshots`, `buildings` identity | Entity model and event log |
| `TECH-81` | Knowledge graph (cross-scale dependency queries) | Entity model and event log |
| `TECH-18` | Postgres IA migration (decision gate) | Entity model and event log |
| `TECH-31` | Scenario generator (extended for aggregate scenarios) | Harness + tests |
| `FEAT-46` | Geography / parameter pipeline (feeds procedural city generator) | Dormant-reconstruct-generate loop |
| `FEAT-47` | Multipolar (now feeding connurbation at region scale) | Scale-neutral spine |
| `TECH-77` / `TECH-78` / `TECH-79` / `TECH-80` / `TECH-83` | IA evolution lane (discretionary, schedule where they unblock plan work) | Varies |

### Step 3 — Region + Country MVP

| Issue | Role | Stage |
|---|---|---|
| `FEAT-09` | Trade / production / salaries (**now** inter-city at region scale) | Region sim model |
| `FEAT-10` | Regional monthly bonus (evolves into region sim stub) | Region sim model |
| `FEAT-47` | Multipolar / connurbation — full cross-city scope | Region sim model |
| `FEAT-15` | Port system (trade edges) | Region sim model |
| `FEAT-16` | Train system (trade edges) | Region sim model |
| `FEAT-14` | Vehicle traffic (aggregated flow at region scale) | Region sim model |
| `FEAT-39` | Sea / shore band (coastal region semantics) | Procedural content at region scale |
| `FEAT-40` | Water sources and drainage | Procedural content at region scale |
| `FEAT-48` | Water body volume budget | Procedural content at region scale |
| `FEAT-51` (extension) | Scale-aware dashboard | Inter-scale wiring |
| `TECH-83` | Sim parameter tuning and experiments | Playability pass |

### Issues that are **not** on the critical path

Everything else in the current BACKLOG remains on its existing priority. It is **not required** by this plan; it will continue to be scheduled as normal. Do not re-prioritize issues just because they are not listed above.

---

## 9. New feature ideas — provisional list

From the brainstorm turn. These do not yet have BACKLOG ids. File them under **§ Multi-scale simulation lane** in `BACKLOG.md` when the plan stabilizes (likely at the end of Step 1 or beginning of Step 2).

**Scale-neutral spine (Step 2):**

1. **Simulation scale manager** — `SimulationScale` enum, `ISimulationModel` contract, active scale switch, transition API.
2. **City aggregate snapshot (digest)** — compact struct sampled per N city ticks, consumed by region sim.
3. **Multi-scale time dilation clock** — single authoritative calendar, time dilates with active scale.
4. **Multi-scale save tree** — hierarchical save format; per-node aggregate + optional full detail; incremental streaming on zoom.
5. **Event bubble-up / constraint push-down bus** — cross-scale event and parameter transport.
6. **Dormant city freeze + delta log** — pause full city tick, advance aggregate only; consumes TECH-82 Phase 2.
7. **Aggregate → detail reconstruction** — rebuild a full city state from aggregate trajectory (+ base snapshot if any).
8. **Procedural city generator** — materialize a never-visited city from region parameters + seed; consumes FEAT-46.
9. **Scale zoom transition UX skeleton** — camera / UI path from city → stub region and back; runs the aggregation/reconstruction round trip.

**Region + Country content (Step 3):**

10. **Region simulation model** — coarse grid/graph, tick loop, migration/trade/economy.
11. **Inter-city trade network solver** — distributes production/consumption across region graph edges.
12. **Country political & policy layer** — elections, budget, tax, trade, immigration; parameter deltas pushed down.
13. **World climate + commodity trade macro** — parked for post-MVP, but keep the aggregation contract world-ready.
14. **Scale-aware dashboard** — FEAT-51 extension; metric set swaps by active scale.
15. **Multi-scale scenario tests** — extends TECH-31 scenario generator to three-scale round trips.

---

## 10. Open questions

Ordered roughly by how soon they need an answer.

1. **Aggregation cadence.** How often does a city publish a new aggregate? Every N city ticks, every month, every year? Decision gates the region tick rate.
2. **Aggregate schema ownership.** Lives in compute-lib (pure)? In a new `Aggregates/` folder? In `Assets/Scripts/Simulation/`? Affects where Step 2 modules sit.
3. **Reconstruction fidelity target.** How close does a reconstructed city need to be to its pre-dormant state? Cell-perfect? Stat-equivalent? Plausible only? Governs how much delta log we carry.
4. **Procedural city generator determinism.** Same seed + same region params = same city forever? Or same + last N region states? Affects save format and replay behavior.
5. **Scale lock vs scale freedom.** Can the player switch scale at will, or only at save points? Hard lock is cheaper; freedom is the pitch.
6. **Postgres yes or no.** TECH-18 migration (full Postgres IA + gameplay data) drastically changes the hierarchical save story. Decide before Step 2 stage 2.
7. **Unity vs headless.** Does the region tick run inside Unity's main loop, or in a worker thread / external process via compute-lib? Affects threading and save format.
8. **Save format versioning.** Per-scale schema version? Global save version? Both? (Probably both — global major, per-scale minor.)
9. **Existing FEAT-09 re-scope.** Does the current FEAT-09 row stay and get re-scoped in place, or do we archive it and file a new region-scope issue? Affects BACKLOG cleanliness.
10. **Multipolar FEAT-47 split.** City-internal multipolar (Step 1) vs cross-city connurbation (Step 3) — same row, or split into two rows? Affects closeout semantics.
11. **Player loop at country scale.** What does the player actually do as head of state in the MVP — set 3–5 policy knobs? Manage a budget? Declare one war? Needs a minimum definition before Step 3 stage 2.
12. **Dashboard scale switching.** Reuse the same uGUI canvas with a scale-aware controller, or one canvas per scale? Affects FEAT-51 scope.
13. **Testing substrate.** Does the three-scale MVP need its own scenario format in TECH-31, or does the existing 32×32 test map generalize?
14. **Skill dispatch.** Should the master-plan-navigator skill call sub-skills directly, or only recommend them? Affects orchestration complexity.
15. **Where does world scale sneak in.** Even parked, does the save format already need a `world` root, or can world be retrofitted later without a migration?

---

## 11. Mutation log

Append-only. One line per change, with date and short rationale.

- **2026-04-10** — Initial seed of this brainstorm document. Captures the vision, the three-step skeleton, the existing-issue-to-step mapping, the provisional glossary, the orchestrator/skill proposal, and the open questions. No BACKLOG rows filed yet. No umbrella tracker yet.

---

## 12. Pointers for a fresh agent

If you are picking this up cold, this is the minimum reading to not waste time:

1. **This document, section 1 and section 2** — vision and design insights. Ten minutes.
2. **`BACKLOG.md`** — skim § Compute-lib program, § Agent ↔ Unity & MCP context lane, § IA evolution lane, § Economic depth lane, § Gameplay & simulation lane, § High Priority. Twenty minutes.
3. **`CLAUDE.md` + `ia/rules/invariants.md`** — hard rules. Do not skip.
4. **`ia/specs/simulation-system.md`** (via `mcp__territory-ia__spec_section`, not full read) — current single-scale tick loop.
5. **`ARCHITECTURE.md`** — runtime layers, dependency map, especially GridManager hub trade-off.
6. **`docs/information-architecture-overview.md`** — the IA stack so you know why we lean on MCP + specs + glossary.

**What the human wants next (as of 2026-04-10):**

- Continue the conversation that produced this document.
- Do not yet file BACKLOG rows for the 15 new FEAT ideas — wait until Step 1's stages are locked.
- Do not yet create `ia/projects/multi-scale-master-plan.md` (the tracker) — wait until the skeleton in section 4 stops churning.
- Do not yet create the `multi-scale-master-plan-navigator` skill — it is blocked on the tracker existing.
- **Do** push on the open questions in section 10, especially 1–8, because they gate any concrete implementation work.
- **Do** propose edits to this brainstorm's section 4 skeleton — that is the point of the exercise.

**What the human does not want:**

- Time estimates for any step, stage, phase, or task.
- Premature decomposition of Step 3 while Step 1 is still open.
- Scope creep toward world / solar scales before the three-scale MVP ships.
- New speculative abstractions in code before the plan itself is stable.

---

*End of brainstorm seed. Treat the whole document as provisional except for sections 0, 1, and the mutation log.*
