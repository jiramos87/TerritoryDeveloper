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

### 2.7. Dormant scales evolve by algorithm, not by live tick

> Added 2026-04-11 as a consequence of Open Question #1.

Only the **active scale** runs a real simulation tick loop. Every other scale is **frozen in live-sim terms** and advances exclusively through a **deterministic (optionally seeded-stochastic) evolution algorithm** that runs **on zoom** — i.e., the moment the player moves between scales, the leaving scale is snapshotted, and the entering scale is **fast-forwarded** from its last snapshot to "now" by applying the evolution algorithm once over the elapsed in-game time.

Consequences:

- **No N-tick publish from dormant scales.** The old "city publishes aggregate every N ticks while the region ticks" model is discarded. Dormant cities do not tick at all.
- **Reconstruction becomes the application of the evolution algorithm.** Aggregate trajectory is no longer a time-ordered stream of sampled digests; it is the **input parameters** of `evolve(snapshot, Δtime, params) → snapshot'` plus whatever seeded RNG was used.
- **NPC leaders become evolution parameter modulators, not dormant players.** A head-of-state, governor, or AI mayor does not "play" their scale off-camera. They are represented as **modifiers on the evolution algorithm parameters** for the scales they influence — e.g., an industrialist country leader raises the industry-growth coefficient, an authoritarian leader dampens happiness variance.
- **Algorithms can be scale-specific.** City evolution uses one algorithm family, region evolution another, country another. Each is a **pure function** (plus seed) callable headless, which aligns naturally with `TECH-38` compute-lib.
- **Parity gap is a first-class concept.** When the player zooms back into a city that has been evolved algorithmically for 50 years, the reconstructed live sim must be **close enough** to the algorithmic projection that returning to live play does not feel like a discontinuity. The allowable divergence is the **parity budget** and must be measurable in tests.
- **What survives evolution** must be explicit. Some city features are **evolution-invariant** (preserved verbatim across algorithmic updates: core road backbone, landmark buildings, persistent player decisions); others are **evolution-mutable** (global stat levels like education / energy / water / happiness, density distributions, possibly new trunk road classes added by the algorithm). The split is a design surface of its own — see new Open Question block.

This is the biggest single decision in the brainstorm so far, and it simplifies Step 2 significantly: no event-delta log is needed for dormant cities, no real-time inter-scale bus is needed between ticks, and the save format can be a single snapshot per scale per node plus the algorithm's own parameter set.

### 2.8. Scale unlock is a progression mechanic, not a config toggle

> Added 2026-04-11 as a consequence of Open Question #5.

The long-term pitch is **freedom**: the player can move between scales at will. But the shipped loop **unlocks scales progressively**, starting with the city. A region scale becomes available only after the player's city crosses some unlock threshold (metric TBD — new open question). Country unlocks after region, and so on. Once unlocked, a scale is permanently available to that save.

Consequence: Step 2 must ship the **unlock mechanism** even though Step 2 only exercises it with a single scale. Step 3 is where the mechanism is first visible to the player.

### 2.9. A game is the set of scales, not a single city

> Added 2026-04-11 as a consequence of Open Question #10.

A "save" is no longer "one city the player built". It is **the full multi-scale tree** — active scale + all dormant scales, their snapshots, their evolution algorithm parameters, their unlock state, and any NPC leader modifiers. The city is one node in that tree, not the tree itself. This reframes Step 1's "close the city MVP" goal: we are closing the **city node schema**, not the entire game scope.

---

## 3. Glossary seeds (provisional — not yet canonical)

These terms are used throughout this doc. When any one stabilizes, promote it to `ia/specs/glossary.md` and delete the `(provisional)` tag here.

- **Simulation scale** (provisional) — a named level of the simulation stack (`CITY`, `REGION`, `COUNTRY`, `WORLD`, `SOLAR`). Enum + `ISimulationModel` contract.
- **Active scale** (provisional) — the single scale currently running its full tick loop; all other scales are dormant.
- **Dormant scale** (provisional) — any scale that is not the active scale. Holds a snapshot + evolution algorithm parameters; does not tick.
- **Evolution algorithm** (provisional) — pure function (with optional seeded RNG) `evolve(snapshot, Δtime, params) → snapshot'` that fast-forwards a dormant scale on zoom. Replaces the prior "dormant tick + aggregate publish" model. Scale-specific: city evolution, region evolution, country evolution.
- **Evolution parameters** (provisional) — the tunable inputs to an evolution algorithm for a given scale node: growth coefficients, policy multipliers, NPC leader modifiers, RNG seed.
- **Evolution-invariant** (provisional) — state that an evolution algorithm must preserve verbatim (core road backbone, landmark buildings, explicit player decisions). Designed per scale.
- **Evolution-mutable** (provisional) — state an evolution algorithm is allowed to rewrite (global stat levels, density distributions, possibly new trunk road classes).
- **Parity budget** (provisional) — the maximum allowed divergence between an algorithmic projection and a live-sim re-run of the same scale over the same interval. Measurable in tests; gates reconstruction fidelity.
- **Scale sovereign** / **NPC leader** (provisional) — the AI entity associated with a dormant scale (mayor, governor, head-of-state). Does not run a live simulation; represented as a bundle of modifiers on that scale's evolution parameters.
- **Reconstruction** (provisional) — materializing a playable live city (or region, or country) state from its snapshot + the result of applying the evolution algorithm up to "now".
- **Procedural scale generation** (provisional) — creation of a never-visited scale node (city, region) from parent-scale parameters + deterministic seed. Generalization of the earlier "procedural city generation".
- **Scale zoom** (provisional) — the UX transition from one active scale to another. Triggers: (a) snapshot of the leaving scale, (b) evolution fast-forward of the entering scale, (c) reconstruction of the entering scale into playable form.
- **Scale unlock** (provisional) — persistent per-save state recording which scales the player has earned access to. Starts with city unlocked only.
- **Multi-scale save tree** (provisional) — hierarchical save format: root → countries → regions → cities, each node carrying a snapshot + evolution parameters + unlock state. **Stored as a single `jsonb` column in Postgres**, not as files on disk (see Open Question #6 resolution).
- **Event bubble-up** (provisional — scope reduced) — structured event propagation upward through the scale stack, applied **at zoom time** (not continuously). E.g., zooming out of a city whose last year contained a large fire re-labels that city's node in the region view.
- **Constraint push-down** (provisional — scope reduced) — parameter propagation downward through the scale stack, applied **at zoom time**. E.g., country policy multipliers are baked into a city's evolution parameters when the player zooms down to it.

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

**Goal:** Build the **scale-neutral spine** needed to support any number of scales, **without yet adding a second playable scale**. After Step 2, the repo contains: pure-compute sim modules, a `SimulationScale` enum and model contract, a **snapshot schema** for the city scale, a hierarchical save tree (stored as `jsonb`), a **city evolution algorithm** (pure function, scale-specific), a snapshot-based freeze/reconstruct path, a procedural-scale generator, a scale unlock mechanism, and a scale zoom transition UX skeleton — all exercised by **exactly one scale (city)**, which now plays the role of both "active" and "evolved dormant" in isolation tests.

**Exit criteria (provisional):**
- A city can be put into **dormant mode** (snapshot + evolution params), advanced by the city evolution algorithm over an arbitrary Δtime, and reconstructed into a playable live city such that the divergence from a full live-sim run over the same interval stays inside the **parity budget**.
- A procedural city can be generated from region-like parameters + seed and loaded as a normal `GameSaveData`.
- The save format holds cities inside a region inside a country node in a single `jsonb` document, even if region and country evolution algorithms are stubs.
- Zooming out of a city and back in round-trips correctly: snapshot → evolve(Δt=0) → reconstruct produces the same city state modulo parity budget.
- The scale unlock state is persisted and queryable; Step 2 ships with "only CITY unlocked" as the default.
- Compute-lib modules are callable headless (without Unity) for at least one non-trivial city sub-system, **and** for the city evolution algorithm.

**Candidate stages (draft):**

1. **Pure-compute foundation.** TECH-38 (core compute modules), TECH-15 (geography init performance via compute-lib), TECH-34 (`GridManager` region manifest), TECH-32/TECH-35 research as gated spikes. Precondition for running a city tick **and** a city evolution algorithm anywhere that isn't the main Unity loop.
2. **Entity model and Postgres save.** TECH-82 Phases 1→4 (metrics history, `city_events`, `grid_snapshots`, `buildings` identity) — **scoped down**: the `city_events` delta log is no longer the spine of dormant updates, but it is still useful for live-sim observability inside the active scale. TECH-81 (knowledge graph, only if scale dependency queries justify it). TECH-18 **resolved to yes**: migrate save format to a Postgres `jsonb` column (see Open Question #6 resolution). New TECH issue needed to own the save migration.
3. **Scale-neutral spine.**
   - `SimulationScale` enum + `ISimulationModel` contract (new FEAT).
   - **Scale snapshot schema** — per-scale snapshot struct (city first) capturing everything evolution-invariant + everything the evolution algorithm needs as input (new FEAT). Replaces the earlier "city aggregate digest" item.
   - Multi-scale save tree format, `jsonb`-backed (new FEAT).
   - Zoom-time event bubble-up / constraint push-down (new FEAT — scope reduced: no continuous bus, only zoom-time projection).
   - Multi-scale time dilation clock (new FEAT).
   - **Scale unlock state** — persisted per save, with an API for scales to become available (new FEAT).
4. **City evolution algorithm + reconstruct-generate loop.**
   - **City evolution algorithm** — pure `evolve(snapshot, Δt, params) → snapshot'` for the city scale, with seeded RNG (new FEAT). Operates on evolution-mutable state only; preserves evolution-invariant state verbatim.
   - **Snapshot → live reconstruction** — rebuild a playable live city state from a snapshot (new FEAT).
   - **Procedural scale generator** — materialize a never-visited city from parent-scale parameters + seed (new FEAT, consumes FEAT-46 parameter pipeline). Generalizes to region later.
   - **Parity budget harness** — measurable comparison between `evolve(snapshot, Δt)` and a full live-sim run over the same Δt (new TECH). Gates the stage.
5. **Scale zoom UX skeleton.** Camera / UI path to zoom "out" from the city to a stub region view and back. Lo-fi region view is inert (no region evolution yet) but the transition runs the full snapshot → evolve → reconstruct round trip against real city state. Also ships the **scale unlock** UX entry point (even if only CITY is unlocked at this step).
6. **Harness + tests.** TECH-31 scenario generator extended to emit snapshot-shaped scenarios and parity-budget round-trip scenarios. `agent-test-mode-verify` covers a zoom-out → evolve(Δt=N years) → zoom-in round trip. `docs/agent-led-verification-policy.md` updated if new driver kinds are needed.

### Step 3 — Region + Country MVP

**Goal:** Two additional scales (region and country) become **playable as the active scale**, each with its own live-sim tick loop **and** its own evolution algorithm (used when it is dormant). After Step 3, we have the **three-scale MVP** — proof that the architecture generalizes from one scale to N, with the active/dormant asymmetry exercised in both directions.

**Exit criteria (provisional):**
- Player can zoom out from a city to a region map, see other cities reconstructed from their snapshots, play the region as the active scale (live sim), and zoom back in to any city (visited or procedural) without state loss and inside the parity budget.
- Player can zoom out further to a country map, play the country as the active scale, and exercise a **minimum head-of-state loop**: assign a national budget, launch at least one national infrastructure project that propagates down to a region/city, set one international-relations posture, and create at least one new region node (expanding the map).
- A country policy change made while the country is the active scale is **baked into region and city evolution parameters** the next time the player zooms down.
- A city event that occurred during live city play (e.g., a large fire) is **bubbled up at zoom-out time** and visible in the region and country dashboards.
- Dashboard adapts metrics to the active scale **and** shows child-scale rollups (country → region stats; region → city stats; city → only its own — see §12 resolution of Open Question #12).
- Save/load preserves all three scales end-to-end in the single `jsonb` document.
- Region unlock and country unlock are exercised: the player starts with only CITY unlocked, crosses the region unlock threshold, then the country unlock threshold.
- At least one **economic flow** crosses scales: city exports feed inter-city trade in the region layer's live sim, and the resulting inter-city trade balance feeds back into city evolution parameters when the player zooms down.

**Candidate stages (draft):**

1. **Region sim model (active + evolution).** Coarse region grid/graph; nodes = city snapshots, ports, farmland, wilderness; edges = road/rail/sea lanes.
   - Active-scale region tick: migration pressure, trade flow solver, regional economy, new-trunk-infrastructure placement.
   - **Region evolution algorithm**: pure function analogous to the city one, operating on region evolution-mutable state (aggregate migration, regional economy levels, optional new major roads). Runs on zoom when region is dormant.
   - Re-scopes FEAT-09 (trade/production/salaries) to the region active-tick layer. Consumes FEAT-47 multipolar for connurbation.
2. **Country sim model (active + evolution + head-of-state loop).** Political/policy layer.
   - Active-scale country tick: advances elections, budget execution, international relations, national infrastructure projects, region creation.
   - **Country evolution algorithm**: when the country is dormant, fast-forwards via long-period political/economic drift with NPC leader modifiers applied.
   - **Head-of-state loop (minimum playable)**:
     - Assign national budget across a small fixed set of categories.
     - Launch **national infrastructure projects** that touch one or more child regions/cities (e.g., a cross-region highway, a national power grid upgrade).
     - Set international-relations stance against one to three neighboring countries (stubbed — neighbors can be inert).
     - Reorder national priorities (affects next-tick resource allocation).
     - **Create a new region/ville node** and fix its initial evolution parameters (consumes the procedural scale generator from Step 2).
3. **Inter-scale wiring (zoom-time).** Full event bubble-up + constraint push-down across all three scales, applied at zoom time. Scale-aware dashboard (FEAT-51 extension) with **drill-down rollups** (country sees region stats; region sees city stats; city sees only its own). All non-active cities in the active region stay dormant and are evolved on demand.
4. **Procedural content at region scale.** Unvisited region nodes generate plausible cities on demand (Step 2 generator, now driven by region sim parameters). FEAT-15 ports + FEAT-16 trains + FEAT-39 sea + FEAT-10 regional bonus evolved into real region-layer content.
5. **Playability pass (per-scale minimum player loops).** Each scale ships a minimum coherent player loop:
   - **City**: already established in Step 1.
   - **Region**: influence migration, trade, inter-city infrastructure; place or upgrade regional trunk roads/rails/ports.
   - **Country**: the head-of-state loop from stage 2 above.
6. **Multi-scale save, load, and test harness.** Scenario generator (TECH-31) extended to multi-scale scenarios per Open Question #13; compile-gate + test mode driver coverage for three scales; parity-budget checks for region and country evolution algorithms. Verification policy updated.

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

## 6. Proposed orchestrator / tracker artifact — **delegated hierarchy**

> Rewritten 2026-04-11 per Open Question #14 resolution.

A single monolithic tracker is too heavy. Instead, the plan is tracked by a **small hierarchy of orchestrator documents**, each one owning exactly one level of the step → stage → phase → task breakdown, each one dispatching work to the next level down. The dispatch is **not automated** — the orchestrators are reading material for agents, not runnable programs. They exist so that no single document has to hold the entire plan in its head.

### 6.1. Document hierarchy

```
ia/projects/multi-scale-master-plan.md                       ← global orchestrator (step-level)
 └─ ia/projects/multi-scale/step-{N}-{slug}.md                ← stage-level orchestrator per step
     └─ ia/projects/multi-scale/step-{N}/stage-{M}-{slug}.md  ← phase-level orchestrator per stage
         └─ BACKLOG row (FEAT-/TECH-/BUG-)                    ← task level; lives in BACKLOG.md, not in a tracker doc
```

- **Global orchestrator** (`multi-scale-master-plan.md`) — step checkboxes, pointers to each step file, cross-cutting Decision Log, global Lessons Learned.
- **Step orchestrator** — stage checkboxes, pointers to each stage file, step-level Decision Log + Lessons Learned.
- **Stage orchestrator** — phase checkboxes, per-phase task lists (each task = a BACKLOG row id), stage-level Decision Log + Lessons Learned.
- **Task = BACKLOG row** — the actual unit of work. One BACKLOG row per task. No task ever lives only inside a tracker file; if it isn't in `BACKLOG.md`, it doesn't exist.

### 6.2. Coordination is human, not automated

- Orchestrators are read by the agent (or the human) at the start of a session to figure out what to do next. They are **not** executed.
- Multiple agents may work in parallel on different tasks under the same phase, provided their BACKLOG rows have no file-level overlap (see Open Question Q-new-6 on parallel agent safety).
- Some tasks will themselves spawn sub-tasks (sub-issues). Sub-tasks are tracked as **child BACKLOG rows** with a `Depends on:` back to the parent row and a short pointer in the stage orchestrator.

### 6.3. Existing lifecycle skills need to learn this hierarchy

The current project-spec lifecycle skills (`project-spec-kickoff`, `project-spec-implement`, `project-spec-close`, `project-stage-close`, `project-new`) assume a flat "one BACKLOG row ↔ one `ia/projects/{ISSUE_ID}.md` spec" model. Under this hierarchy:

- A **task** (BACKLOG row) still owns its own `ia/projects/{ISSUE_ID}.md` project spec — unchanged.
- A **phase / stage / step** owns an **orchestrator document**, not a project spec, and is **not closeable via `closeout`** (closeout still only closes BACKLOG rows).
- The **global orchestrator** is not a BACKLOG row and must never be deleted by a closeout.

→ Therefore the lifecycle skills must be taught to distinguish orchestrator documents from project specs, and a new orchestrator-close recipe (or an extension of `project-stage-close`) is needed for closing a phase / stage / step. See new Open Question Q-new-8.

### 6.4. Promotion rules (unchanged from §0)

- Brainstorm (this doc) — stays until Step 1's stages are locked.
- Global orchestrator promotes out of this doc first, once the step/stage skeleton in §4 stops churning.
- Step and stage orchestrators are created **lazily**, only when their parent step or stage enters "in progress".
- BACKLOG rows for the new FEAT/TECH ideas in §9 are still **not filed yet** — wait for Step 1 stages to lock.
- New BACKLOG rows generated by this plan land under a new section heading in `BACKLOG.md` — proposed name **§ Multi-scale simulation lane** — ordered between the existing § Gameplay & simulation lane and § High priority.

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

### Backlog audit needed before Step 2 opens

> Added 2026-04-11 per Open Question #9 resolution.

Once this master plan stabilizes, a substantial slice of the existing BACKLOG will be **obsoleted, rewritten, or replaced** by multi-scale rows. Examples the human has already flagged:

- Some existing `FEAT-` rows will be deleted outright because the evolution-algorithm approach removes their motivation.
- Other existing rows (notably `FEAT-09`, `FEAT-47`, and parts of `TECH-82`) will be **deeply rewritten**, not merely re-scoped with a one-line note.
- Several entirely new rows will be filed under § Multi-scale simulation lane.

Rather than do these edits piecemeal as each stage opens, schedule a **dedicated backlog triage pass** — a single session that walks the full BACKLOG against this document and (a) marks rows for deletion, (b) rewrites the surviving rows' bodies, (c) files the new rows. The triage pass is itself a task under Step 1's closing stage, not a floating chore. It is tracked as a new open question (Q-new-7) until scheduled.

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

Split into two groups: **§10.A Resolved** (human answered 2026-04-11; decisions now live in the body of the doc) and **§10.B Open** (still need a decision before they gate work).

### 10.A. Resolved (2026-04-11)

Each resolved entry keeps the original question for traceability and records the decision + where it now lives in the body.

1. ~~**Aggregation cadence.**~~ **Resolved → no cadence.** Dormant scales do not publish aggregates on a cadence. A dormant scale's state is advanced by its **evolution algorithm** on zoom, over the elapsed Δtime. See §2.7 and Step 2 stage 4.
2. ~~**Aggregate schema ownership.**~~ **Resolved → replaced by scale snapshot schema.** The "aggregate digest" is gone. The scale snapshot schema lives in compute-lib (pure) so the evolution algorithm can run headless. See §3 glossary seed "scale snapshot" (implicit under `Reconstruction` + `Evolution parameters`).
3. ~~**Reconstruction fidelity target.**~~ **Partially resolved → parity budget concept.** Fidelity is measured as **divergence from a hypothetical live-sim run** over the same interval. Global stat levels (education, energy, water, happiness) are easy to keep parity on; urbanization shape and building-level detail are **the hard part**. The numeric target itself is still open — see new Q-new-2 and Q-new-3 below.
4. ~~**Procedural city generator determinism.**~~ **Resolved in spirit → deterministic function of (parent-scale params, seed).** Consistent with evolution algorithms being pure functions. State dependence on "last N region states" is rejected because it would break headless replay. See §3 glossary "Procedural scale generation".
5. ~~**Scale lock vs scale freedom.**~~ **Resolved → freedom, gated by progressive unlock.** The player can jump between scales at will **once a scale is unlocked**. Scales unlock in order starting from CITY. The unlock mechanism itself is new scope — see §2.8 and new Q-new-4 (unlock triggers).
6. ~~**Postgres yes or no.**~~ **Resolved → yes, save-as-jsonb.** The save file is stored as a single `jsonb` column in Postgres, not a folder of files on disk. This impacts backup, sharing, modding, and migration — see new Q-new-5 for the migration path sub-question.
7. ~~**Unity vs headless.**~~ **Resolved → evolution algorithms are headless, live ticks run in Unity.** The active scale's live simulation runs in the normal Unity loop (where GridManager, MonoBehaviour, etc. live). Evolution algorithms for dormant scales are pure compute and run headless in compute-lib, so they can be tested and measured without Unity.
9. ~~**Existing FEAT-09 re-scope.**~~ **Resolved → dedicated backlog triage pass.** Many issues will be deleted, rewritten, or newly filed. A single triage session will handle the whole BACKLOG in one pass instead of row-by-row edits during stage work. See §8 "Backlog audit needed before Step 2 opens" and new Q-new-7.
11. ~~**Player loop at country scale.**~~ **Resolved → head-of-state loop defined.** Minimum playable country loop: national budget, national infra projects, international-relations stance, priority reordering, new region creation + initial evolution parameter assignment. See Step 3 stage 2.
12. ~~**Dashboard scale switching.**~~ **Resolved → drill-down rollups.** The dashboard shows stats for the active scale **and** for its children. Country dashboard surfaces region + city rollups; region surfaces city rollups; city surfaces only its own. Implementation detail (single canvas with controller vs. per-scale canvas) is refined in new Q-new-9.
13. ~~**Testing substrate.**~~ **Resolved → per-scale test scenarios required.** Each scale needs its own scenario format in TECH-31. The existing 32×32 city map does not generalize. See new Q-new-10 for the shape of non-city scenarios.
14. ~~**Skill dispatch.**~~ **Resolved → delegated hierarchy of orchestrator documents.** Replaces the single "master-plan-navigator" skill with a document hierarchy (global → step → stage → BACKLOG row). No automated dispatch; orchestrators are reading material. Existing lifecycle skills need to be taught to distinguish project specs from orchestrator documents. See §6 and new Q-new-8.

### 10.B. Still open

Carried over from the original list, still unresolved:

8. **Save format versioning.** Per-scale schema version? Global save version? Both? Extra weight now that the save is a single `jsonb` document — migrations become row-level. Needs a concrete versioning strategy before Step 2 stage 2.
10. **Multipolar FEAT-47 split.** City-internal multipolar (Step 1) vs cross-city connurbation (Step 3) — same row, or split into two rows? Likely absorbed into the backlog triage pass (Q-new-7), but flagged here for visibility.
15. **Where does world scale sneak in.** Even parked, does the save format already need a `world` root, or can world be retrofitted later without a migration? More urgent now that save-as-jsonb is decided.

### 10.C. New questions planted 2026-04-11

Downstream of the resolutions above. Ordered by how soon they need an answer.

- **Q-new-1. Evolution algorithm shape.** Pure deterministic function `f(state, Δt, params) → state'`, or hybrid deterministic + seeded stochastic? Determinism gives reproducibility and clean tests; seeded stochastic gives surprise and "the city kept growing in unexpected ways". Probably hybrid with an explicit seed field on the snapshot. Governs testability and save format.
- **Q-new-2. Parity budget units.** How is the divergence between `evolve(snapshot, Δt)` and a full live-sim run measured? Per-metric absolute error? L2 norm across a fixed metric vector? Symbolic invariants (road topology preserved, landmark buildings preserved)? The answer determines the Step 2 parity-budget harness.
- **Q-new-3. Evolution-invariant vs. evolution-mutable split per scale.** For the city scale specifically, enumerate what the algorithm is allowed to rewrite (global stat levels, density distributions, possibly new trunk roads/highways) vs. what it must preserve verbatim (core road backbone, landmark buildings, explicit player edits, district assignments). Repeat for region and country scales later. Load-bearing for Step 2 stage 4.
- **Q-new-4. Scale unlock trigger.** What concrete metric unlocks region from city, and country from region? Candidates: population threshold, GDP threshold, story/narrative milestone, explicit player choice (pay a cost to unlock). Affects Step 2 stage 3 (unlock state) and Step 3 stage 5 (playability pass).
- **Q-new-5. Save-as-jsonb migration path.** Current save is file-backed. Migration needs: Postgres schema (table, column, indices on what), backup story, export/import flow for sharing saves, modding story for editing saves, versioning (ties to Open Q #8). Probably a new dedicated TECH spec before Step 2 stage 2.
- **Q-new-6. Parallel agent safety map.** Which stages/phases/tasks can run in parallel sessions without merge conflict risk? Needs a dependency graph at the task level (BACKLOG row level) that marks file-level overlaps. Candidate for a tooling spike under Step 1's closing stage — possibly a `validate:parallel-safe` script that reads the stage orchestrators and flags overlapping file touches.
- **Q-new-7. Backlog triage pass scheduling.** When is the triage pass that rewrites/deletes/creates BACKLOG rows run? Options: (a) at the very end of Step 1, as the final task before Step 2 opens; (b) as a standalone task slotted between steps; (c) incrementally, one row at a time, as each stage opens. Leaning (a) because it minimizes churn during Step 1 and gives the triage a stable target.
- **Q-new-8. Lifecycle skill rewrite scope.** `project-spec-kickoff`, `project-spec-implement`, `project-spec-close`, `project-stage-close`, `project-new` currently assume flat "one BACKLOG row ↔ one project spec". Under the delegated hierarchy they need to distinguish orchestrator documents from project specs, and a new phase/stage/step-close recipe is needed. Rewrite in place, or file new sibling skills? Affects `ia/skills/*` directly.
- **Q-new-9. Dashboard drill-down UX.** Single uGUI canvas with a tree/breadcrumb navigator across scales, or per-scale canvas with a shared child-stats panel? Refines the resolved Open Q #12. Affects FEAT-51 extension scope in Step 3 stage 3.
- **Q-new-10. Non-city test scenarios.** What does a region test scenario look like? A country test scenario? Probably a JSON document describing: initial snapshot(s), scheduled external events, expected final state, parity budget bounds. Needs a shape before TECH-31 is extended.
- **Q-new-11. Algorithm parameter learning from played city.** When the player plays a city as the active scale and then leaves, does the city's evolution algorithm record **parameter adjustments** derived from how that specific city actually behaved under live play (calibration), or does every city revert to the region-wide default evolution params when it goes dormant? Calibration adds long-term character to visited places but complicates save format. Defaults are simpler and more predictable.
- **Q-new-12. Event bubble-up / constraint push-down scope at zoom time.** Now that these are zoom-time projections instead of a continuous bus, what are the exact hooks? E.g., on zoom-out from city → region: run city evolution up to zoom-out moment → project city events of the last Δt into region's event digest → stamp the region node with the up-projection. Needs a concrete function signature in Step 2 stage 3.
- **Q-new-13. NPC leader modifier model.** How are "scale sovereigns" (governors, heads of state) represented? A bundle of per-parameter multipliers? A small set of policy enums that each map to multipliers? A scripted decision tree? Affects Step 3 stage 2 (country) most directly, but relevant at region scale too.
- **Q-new-14. Parallelism across sessions — durable contract.** Multiple agents working simultaneously need a durable "who is touching what" register so that the parent human does not manually coordinate. Candidate: a new `ia/orchestrator-locks/` directory or a BACKLOG-level `In progress by:` field that agents must claim before editing files cited by a task. Related to Q-new-6.
- **Q-new-15. "Out-of-the-box" arista — player-authored evolution parameters.** Since evolution is now a parameterized function, the player could in principle **edit the evolution parameters of a dormant scale they own** directly (a mayor-mode budget slider that also nudges the city's unattended-growth coefficient). This would turn "setting policy" into a lightweight dormant-play mechanic. Out-of-scope for MVP, but worth planting now so it is not designed out by accident.

---

## 11. Mutation log

Append-only. One line per change, with date and short rationale.

- **2026-04-10** — Initial seed of this brainstorm document. Captures the vision, the three-step skeleton, the existing-issue-to-step mapping, the provisional glossary, the orchestrator/skill proposal, and the open questions. No BACKLOG rows filed yet. No umbrella tracker yet.
- **2026-04-11** — Human answered Open Questions 1–7 and 9–14. Major consequences: (a) dormant scales evolve via a pure evolution algorithm on zoom, not via N-tick aggregate publishes (new §2.7); (b) NPC leaders become evolution parameter modulators (§2.7, glossary "scale sovereign"); (c) scales unlock progressively starting from CITY (new §2.8); (d) a save is the full multi-scale tree, not a single city (new §2.9); (e) save format migrates to Postgres `jsonb` column; (f) country scale gains a concrete head-of-state loop (Step 3 stage 2); (g) dashboard is drill-down by hierarchy; (h) the tracker is replaced by a delegated hierarchy of orchestrator documents with no automation (§6 rewritten); (i) lifecycle skills need to learn to distinguish project specs from orchestrator docs; (j) backlog triage pass scheduled as a dedicated task. Step 2 stages 3–4 rewritten to drop the aggregate-digest/delta-log model in favor of snapshot + evolution algorithm. §10 split into Resolved / Open / New-planted; 15 new open questions planted (Q-new-1..15) including parity budget, evolution-invariant vs mutable split, parallel agent safety, and out-of-the-box player-authored evolution parameters.

---

## 12. Pointers for a fresh agent

If you are picking this up cold, this is the minimum reading to not waste time:

1. **This document, section 1 and section 2** — vision and design insights. Ten minutes.
2. **`BACKLOG.md`** — skim § Compute-lib program, § Agent ↔ Unity & MCP context lane, § IA evolution lane, § Economic depth lane, § Gameplay & simulation lane, § High Priority. Twenty minutes.
3. **`CLAUDE.md` + `ia/rules/invariants.md`** — hard rules. Do not skip.
4. **`ia/specs/simulation-system.md`** (via `mcp__territory-ia__spec_section`, not full read) — current single-scale tick loop.
5. **`ARCHITECTURE.md`** — runtime layers, dependency map, especially GridManager hub trade-off.
6. **`docs/information-architecture-overview.md`** — the IA stack so you know why we lean on MCP + specs + glossary.

**What the human wants next (as of 2026-04-11):**

- Continue the conversation that produced this document. The brainstorm is still living — the 2026-04-11 turn resolved Open Questions 1–7 and 9–14 but planted 15 new questions (Q-new-1..15) and the next pass should push on those, especially Q-new-1 (evolution algorithm shape), Q-new-2 (parity budget units), Q-new-3 (evolution-invariant vs mutable split), and Q-new-6 (parallel agent safety map).
- Do not yet file BACKLOG rows for the new FEAT ideas in §9 — still waiting until Step 1's stages are locked.
- Do not yet create the global orchestrator `ia/projects/multi-scale-master-plan.md` — wait until §4 skeleton stops churning. The new §6.1 hierarchy is the **target shape** for when it does.
- Do not create a `multi-scale-master-plan-navigator` skill — the decision is now that there is **no automated navigator**, just a hierarchy of orchestrator documents read by the agent.
- **Do** schedule the **backlog triage pass** (Q-new-7) as a task under Step 1's closing stage, before Step 2 opens.
- **Do** propose edits to this brainstorm's §4 skeleton — that is still the point of the exercise.
- **Do** think about the **lifecycle skill rewrite** (Q-new-8) — existing `project-spec-*` skills need to learn about orchestrator documents before Step 2 opens.

**What the human does not want:**

- Time estimates for any step, stage, phase, or task.
- Premature decomposition of Step 3 while Step 1 is still open.
- Scope creep toward world / solar scales before the three-scale MVP ships.
- New speculative abstractions in code before the plan itself is stable.
- Resurrection of the old "N-tick aggregate publish" model anywhere. Dormant scales evolve only via the evolution algorithm.

---

*End of brainstorm seed. Treat the whole document as provisional except for sections 0, 1, and the mutation log.*
