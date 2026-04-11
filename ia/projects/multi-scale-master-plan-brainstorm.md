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
5. **Never leave a new term only in this doc.** When a term stabilizes (e.g. `simulation scale`, `dormant scale`, `child-scale entity`, `scale switch`, `shaping event`, `parent-scale stub`, `reconstruction`), promote it to `ia/specs/glossary.md` with a spec pointer. Until promoted, mark the term `(provisional)` inline so agents know it is not canonical.

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

### 2.4. Single shared real-time clock across all scales

> Rewritten 2026-04-11 (second pass). The earlier "time dilation of a single world clock" framing is **retired**. See mutation log.

Every scale runs on the **same real-time clock**. A head-of-state coordinates day-to-day on the same calendar a mayor uses. No per-scale natural tick period, no dilation, no "the region ticks yearly while the city ticks monthly". Simpler to reason about, closer to how an actual head-of-state experiences time, and kills a whole class of "which year is it really" bugs before they exist.

Consequence: when the player switches scales, the **leaving** scale stops live-ticking and the **entering** scale begins live-ticking from the same calendar moment. Dormant scales do not advance during another scale's live play — their evolution is applied at **scale switch** time (see §2.11) by the parent-scale entity against the elapsed Δt since that child's last snapshot. Compatible with §2.7 because evolution is triggered on switch, not on a timer.

> Third-pass clarification (2026-04-11, Q-new-16 resolved): because every scale reads the same calendar, Δt tracking is trivial — each child-scale entity just stores a `last_active_at` calendar stamp, and at switch-in the evolution algorithm runs over `(last_active_at → now)`. No dilation math, no per-scale clock reconciliation, no "which year is it really" logic. The question "how does the parent know how much time has passed" has no hidden difficulty once the shared clock is accepted.

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

> Second-pass refinements (2026-04-11):
>
> - **Evolution writes flow through the parent entity.** A dormant city is represented inside its parent region as a **child-scale entity** that owns the city's dormant state. When the region applies evolution, it mutates that child-scale entity. When the player switches down into the city, the entity's mutations are materialized into the city's live state at load time. Same pattern applies region ↔ country. See §2.14.
> - **Evolution is not fully deterministic.** Shaping events and disasters (see §2.12) inject variance. Outcomes depend on how well the player prepared the entity (defensive infrastructure, redundant services, budget reserves). Preparedness has a resource cost and is its own design surface.
> - **Evolution-invariant = whatever the player touched.** Since NPC leaders are deferred (see resolution of Q-new-11 in §10), the invariant set for a city is everything the player actively placed or assigned: main road backbone, landmarks, districts, player-assigned budgets, explicit zoning decisions. Evolution may still **additively** create new main roads or extend density, but it cannot overwrite or remove a player-touched surface.

### 2.8. Scale unlock is a progression mechanic, not a config toggle

> Added 2026-04-11 as a consequence of Open Question #5.

The long-term pitch is **freedom**: the player can move between scales at will. But the shipped loop **unlocks scales progressively**, starting with the city. A region scale becomes available only after the player's city crosses some unlock threshold (metric TBD — new open question). Country unlocks after region, and so on. Once unlocked, a scale is permanently available to that save.

Consequence: Step 2 must ship the **unlock mechanism** even though Step 2 only exercises it with a single scale. Step 3 is where the mechanism is first visible to the player.

### 2.9. A game is the set of scales, not a single city

> Added 2026-04-11 as a consequence of Open Question #10.

A "save" is no longer "one city the player built". It is **the full multi-scale tree** — active scale + all dormant scales, their snapshots, their evolution algorithm parameters, their unlock state, and any NPC leader modifiers. The city is one node in that tree, not the tree itself. This reframes Step 1's "close the city MVP" goal: we are closing the **city node schema**, not the entire game scope.

### 2.10. Parent scales exist from day one (even in the city MVP)

> Added 2026-04-11 (second pass) as a consequence of the resolution of Open Question #15.

Even before region or country is playable, the city MVP assumes a **parent region** and a **parent country** exist. Their influences are **simulated against the city** from day one — that is already partially true today via interstate borders and "neighbor cities" stubs. The player simply cannot access those parent scales until they unlock them (§2.8).

Consequence: a new **Step 0** lands **before** Step 1 in the master plan — its job is to make the conceptual existence of parent scales explicit in code and save data (placeholder `region_id` / `country_id`, neighbor-city stubs, a world-climate pointer, interstate-border flow semantics). See Step 0 in §4.

### 2.11. Scale switch, not scale zoom

> Added 2026-04-11 (second pass) as a consequence of the second-pass scale-transition decisions.

The transition between scales is a **UI button switch**, not a camera zoom. A new top-bar button panel (analogous to the existing speed-control panel) lets the player pick the active scale. On click:

1. The current scale's state is **saved**.
2. The target scale's evolution algorithm is applied from its last snapshot up to "now" (per §2.4 shared clock).
3. The target scale is **lazy-loaded** into playable form (center-out, loading screen, UX-tuned).

This retires the earlier "zoom out / zoom in" framing. The map's existing zoom levels still exist **inside** each scale (for camera control) — they are orthogonal to scale switching. Glossary rename: **scale zoom** → **scale switch** (§3).

### 2.12. Shaping events and disasters are the non-determinism engine

> Added 2026-04-11 (second pass) as a consequence of the resolution of Q-new-1.

Evolution is not fully deterministic. The variance comes from **shaping events** — disasters, catastrophes, unexpected milestones — that the evolution algorithm folds in when fast-forwarding a dormant scale. These events are not random noise; they are a first-class domain with:

- **A taxonomy** (fire, flood, earthquake, drought, pandemic, boom, crash, ...).
- **Frequency and severity curves** per scale and per geography.
- **Player preparedness mechanics** — defensive infrastructure, redundant services, budget reserves, explicit preparation projects. Preparedness has a resource cost.
- **Defense structures** — new building families the player can place to reduce event impact.

Consequence: shaping events thread across all three scales (a city fire is a city-scale event; a regional drought affects many cities; a country pandemic affects many regions). The framework is cross-scale from day one. See the new feature row in §9.

### 2.13. Climate is in the city MVP

> Added 2026-04-11 (second pass) as a consequence of the resolution of Open Question #15.

Climate is not parked until region. A **minimum climate model** lands in the city MVP (Step 1), coupled to existing systems where it already matters (agriculture, zone demand, pollution dispersion). At region and country scale, climate gains dimensions (multi-climate regions, polar regions, gradients), but the core concept must already exist at city scale so the region layer has something to generalize from. Tied to §2.10 Step 0: the city reads a **world-climate pointer** for its climate value, even if that pointer holds a constant in Step 0.

### 2.14. Parent-scale entities own child evolution writes

> Added 2026-04-11 (second pass) as a consequence of the resolution of Q-new-3.

Every dormant scale node is represented **inside its parent** as a child-scale entity (a region holds one entity per dormant city; a country holds one per dormant region). Evolution writes happen **on that entity**, not on the child scale's own state. When the player switches down into the child, those writes are materialized into the child's live state at load time.

Reason: it is the parent's simulation that has the context to decide what should happen to its children (regional migration pressure decides which city grows; country policy decides which region gets investment). Putting the mutation on the parent's side also means the child's own save JSON stays untouched between switches — evolution is diffed in, not written through.

Consequence for the save schema: a city node in the save has two layers — (a) its last-materialized live snapshot, frozen at the moment the player switched away, and (b) the **pending evolution delta** held by its parent region entity. Load time = live snapshot + apply pending delta → playable state.

### 2.15. Player-authored dormant control is core, not moonshot

> Added 2026-04-11 (second pass) as a consequence of the resolution of Q-new-15.

The first pass marked this as an "out-of-the-box" moonshot to keep alive. The second pass promotes it to a **core MVP mechanic**: from the **region** view, the player controls evolution parameters of dormant cities in that region; from the **country** view, the player controls evolution parameters of dormant regions. The player is literally setting policy for their own dormant children from the parent scale's UI.

This is what makes "playing as head of state" actually feel like playing: most of a head-of-state's job is not ticking their own scale — it is deciding how their subordinate scales should behave while unattended. The parameter surface IS the gameplay.

Scope boundaries still to define: which parameters are exposed at each scale (growth rate? zoning mix? budget splits? priority orderings?), and how the UI surfaces them without becoming a spreadsheet. See new Q-new-26.

### 2.16. Climate v1 is concrete — seasonal cycle + weather events + agriculture hook

> Added 2026-04-11 (third pass) as a consequence of the resolution of Q-new-18.

Climate v1 for the city MVP (§2.13) is no longer a hand-wave. Concrete scope:

- **One biome per city.** A region can hold multiple climates; a country can hold multiple regional climates (including polar gradients). A **single city** has exactly one climate type.
- **Seasonal cycle.** Time-of-year drives a small set of per-month modifiers (temperature band, precipitation band, sun/day length).
- **Weather events** are a first-class sub-system, parameterized by:
  - **Wind speed + direction** — feeds pollution dispersion and (later) fire spread.
  - **Frosts** — seasonal damage to agriculture and happiness penalty.
  - **Storms** — transient desirability/happiness hit, property damage hook (shaping-events tie-in, §2.17).
  - **Droughts** — agriculture failure, water stress, demand shifts.
  - **Heat waves** — happiness, health demand, industrial output dip.
- **Systems climate couples into** (Step 1): desirability, happiness, pollution dispersion, demand, and a **new agriculture building family** treated as a sub-type of **industrial-light zoning**. Agriculture zones are the first climate-sensitive city system and the canonical testbed for all of the above.
- **Cross-scale continuity.** At region and country scale climate gains dimensions (multi-biome, polar bands, gradients) — but the city's single-biome model is the seed the coarser scales aggregate from.

Consequence for Step 1: climate v1 is no longer "pick the lowest-cost coupling first". It is **the agriculture hook plus weather events plus seasonal cycle**, all three, wired into the existing desirability/happiness/pollution/demand pipelines.

### 2.17. Shaping events have a concrete starter taxonomy, and defense structures are a named building family

> Added 2026-04-11 (third pass) as a consequence of the resolution of Q-new-19.

Shaping events (§2.12) have a starter taxonomy and a named cross-scale defense mechanic:

**Event taxonomy (starter set):**

| Event | Primary scale | Also visible at |
|---|---|---|
| Earthquake — regional | Region | City (impact), country (news) |
| Earthquake — local | City | Region (aggregate damage) |
| Tsunami — regional | Region | City (coastal impact) |
| Tsunami — local | City | Region (coastal impact) |
| Landslide — local | City | Region (if major) |
| Storm flood — local | City | Region (if aggregated) |
| River flood — local | City | Region (if upstream source crosses borders) |

Pandemic / boom / crash / drought from §2.12 remain in the taxonomy as additional types; the table above is the **earliest concrete wave** the player can prepare for.

**Defense structures — starter family:**

- **Elevated construction** — build on raised terrain vs floods. Not a new building, a **placement rule** the player opts into per-site.
- **No-build zone declaration** — player-authored invariant that forbids construction on a tile or district. Evolution algorithms must respect it (ties into §2.18).
- **Early-warning station** — a new building type that reduces event impact via advance notice (lower damage, happiness preserved).
- **Concrete containment wall** — a new building family, placed linearly, that blocks landslides and storm surges along its footprint.
- **Artificial channel terraforming** — a terraforming operation that reshapes the heightmap to create a drainage channel, used to prevent river floods. Uses the existing road-preparation-family terraform infrastructure.

Consequence: Step 1's "Shaping-events stub" stage (§4 Step 1 stage 8) now ships **one concrete disaster type** (candidate: local storm flood, because it composes cleanly with the climate weather events in §2.16), a **preparedness cost surface**, and **at least two defense-structure options** from the starter family. The Step 2 framework expansion (§4 Step 2 stage 6) covers the rest of the taxonomy.

### 2.18. Parent-writes-child conflict = no-build-zone replan + expropriation + per-element plan models

> Added 2026-04-11 (third pass) as a consequence of the resolution of Q-new-20.

When a parent's evolution algorithm wants to additively place a road, district, or service in a cell that already holds a player-authored invariant (§2.7 refinements), the conflict model is:

1. **Player invariants act as a forbidden-cell mask.** Evolution reads the mask at switch-out time.
2. **Evolution replans around the mask.** A new road path routes around landmarks; a new district seeds in the nearest legal cluster; a new service building offsets to the nearest legal lot.
3. **When replanning fails** (island, no legal path, forbidden footprint), evolution **does not silently drop the change**. It escalates to an **expropriation** mechanic: the parent scale flags the target cell for expropriation, the player sees a notification at switch-in, and must approve (with happiness/money cost) or counter-plan (accepting a growth penalty).
4. **Every new element type** (regional road, inter-city rail, national infrastructure, defense structure, agricultural zone, etc.) needs its **own construction-plan model** — analogous to `PathTerraformPlan` for roads — that knows how to compose with the forbidden-cell mask and how to report a replan failure to the expropriation escalator.

Consequence: the project reinvokes the old **expropriation** concept (previously parked). It lands as a Step 2 scale-neutral-spine concern, not a Step 3 region concern, because the conflict model must exist the moment the first parent-owned evolution algorithm runs (city dormant under a stub parent region).

Per-element construction-plan models become their own design surface: how many models, shared interface, what they return on success, on replan, on expropriation. See new Q-new-29.

### 2.19. Scale-switch loading budget — 1–2 s, progressive, dashboard-first

> Added 2026-04-11 (third pass) as a consequence of the resolution of Q-new-24.

The scale-switch transition (§2.11) targets a **1–2 second** visible wait, not instant and not a long loading screen. Shape of the transition:

- **Dashboard first.** The target scale's dashboard renders immediately (from the snapshot + pending delta), before the map is fully ready. The player has something to read within ~200 ms.
- **Map loads in chunks.** The target scale's map is built **center-out** (or "player-last-position-first"), one chunk at a time, while the dashboard is already usable.
- **Input is gated** on at least one playable chunk being ready, not on the full map.

Consequence: Step 2 stage 5 (scale-switch UX skeleton) must commit to a **progressive load pipeline**, not a single monolithic rebuild. Budget is expressed as "dashboard in <300 ms, first interactive chunk in <2 s, remaining chunks fill in background". Exact numbers are aspirational; the shape is the commitment.

### 2.20. Process-engineering practices adopted into the plan

> Added 2026-04-11 (third pass). Twelve process-engineering principles were studied and triaged into the plan; the source study has been folded in here and is no longer kept as a separate doc.

The 12 process-engineering principles split into two groups for this plan:

**Already practiced in territory-developer (keep doing, don't re-invent):**

- Decomposition into short, verifiable phases — this is exactly the Step / Stage / Phase / Task hierarchy in §5.
- Spec / implementation / verification separation — already exists via `spec-kickoff`, `spec-implementer`, `verifier` subagents.
- Glossary control — `ia/specs/glossary.md` + `ia/rules/terminology-consistency.md` + MCP `glossary_lookup`.
- Automatic quality gates — `npm run validate:all`, `unity:compile-check`, `db:bridge-preflight`, `unity:testmode-batch`.
- Issue traceability — every BACKLOG row has an id, every project spec is keyed on the id.
- Templates / recipes — `ia/skills/*/SKILL.md` lifecycle recipes.
- Preflight + circuit breakers — invariant preflight MCP tool, Unity compile check, bridge preflight.
- Context reduction on demand — MCP spec slicing instead of full spec loads (`CLAUDE.md` §2).
- Disciplined closeout — `closeout` subagent + lesson migration.

**Load-bearing gaps the master plan must close before Step 2 infra work:**

- **Cross-review gate between kickoff and implementer** (principle: cross-review). A third independent reader agent reviews a project spec before the implementer touches it. Today kickoff writes the plan and implementer follows it without a second set of eyes. Candidate owner: a new subagent `spec-reviewer` launched by `/kickoff` on completion.
- **Per-phase risk / preflight checklist** (principle: per-phase risk preflight). Declarative impact / reversibility / touched-dependencies checklist before each phase of the Implementation Plan. Today preflight is invariant-focused, not phase-structural.
- **Explicit per-phase budget** (principle: explicit per-phase budget). Soft limit on steps / tokens / time per phase before prompting for human confirmation. Stops silent drift on long multi-stage specs (exactly the shape of Step 2 and Step 3).
- **Automatic retrospective at closeout** (principle: automatic retro at closeout). In addition to lesson migration, produce a "what failed / what worked / reusable pattern" digest. Feeds the lifecycle skills themselves.
- **Size classification of specs** (principle: spec size classification). S/M/L labels on project specs drive different workflows — a trivial BUG does not need the same kickoff-review-implement-verify-closeout pipeline as a multi-stage TECH. Today one flow fits all.
- **Cross-project dependency index** (principle: cross-project dependency index). Which open BACKLOG items touch shared files. Gates the agent-parallelization abstraction (Q-new-28) — you cannot parallelize safely without it.
- **Phase metrics dashboard** (principle: phase metrics dashboard). Time / retries / gate-fail counts per phase per project. Currently intuited, not measured. Nice-to-have for the three-scale MVP; load-bearing for ongoing process improvement after it ships.

**Parked as post-MVP polish:**

- Glossary gate in PRs — already a convention, check exists only informally. File as a small TECH row during the backlog triage pass.
- Dry-run closeout — only matters when a spec with many migrated lessons closes. Defer until the first Step 2 stage closes with >3 lessons.
- Consolidated decision journal — the `project_spec_journal_*` MCP tools already exist; the gap is a single unified view. Defer.

Consequence for the plan order: the gaps above land as a **preparation block before Step 0** — see the new pre-Step-0 block in §4. They are not part of Step 0 itself because Step 0 operates on code (parent-scale stubs in the city sim); the preparation block operates on IA + skills + subagents. They run in parallel with the lifecycle skill rewrite (Q-new-27 resolved) because they touch the same surfaces.

---

## 3. Glossary seeds (provisional — not yet canonical)

These terms are used throughout this doc. When any one stabilizes, promote it to `ia/specs/glossary.md` and delete the `(provisional)` tag here.

- **Simulation scale** (provisional) — a named level of the simulation stack (`CITY`, `REGION`, `COUNTRY`, `WORLD`, `SOLAR`). Enum + `ISimulationModel` contract.
- **Active scale** (provisional) — the single scale currently running its full tick loop; all other scales are dormant.
- **Dormant scale** (provisional) — any scale that is not the active scale. Holds a snapshot + evolution algorithm parameters; does not tick. Evolution is applied by its **parent-scale entity** (see **Child-scale entity**), not by the dormant scale itself.
- **Child-scale entity** (provisional, added 2026-04-11 second pass) — the representation of a dormant child inside its parent scale. A region holds one entity per dormant city; a country holds one per dormant region. Evolution writes happen on the entity, and are materialized into the child's live state at load time (scale switch). Carries a **pending evolution delta** layered over the child's last-materialized snapshot.
- **Evolution algorithm** (provisional) — hybrid function `evolve(snapshot, Δt, params, events) → snapshot'` that fast-forwards a dormant scale at scale-switch time. Has a deterministic backbone (growth, migration, economy) plus a stochastic **shaping events** channel (disasters, milestones). Replaces the prior "dormant tick + aggregate publish" model. Scale-specific: city evolution, region evolution, country evolution.
- **Evolution parameters** (provisional) — the tunable inputs to an evolution algorithm for a given scale node: growth coefficients, policy multipliers, RNG seed, and (at region and country scale) **player-authored parameters** set from the parent scale's UI (see §2.15 and the resolution of Q-new-15).
- **Evolution-invariant** (provisional, updated 2026-04-11 second pass) — state an evolution algorithm must preserve verbatim. For the city scale: everything the player actively touched (main road backbone, landmarks, districts, player-assigned budgets, explicit zoning decisions). Evolution may **additively** create new main roads or density, but may not overwrite or remove a player-touched surface.
- **Evolution-mutable** (provisional, updated 2026-04-11 second pass) — state an evolution algorithm is allowed to rewrite: default-generated density, zoning not explicitly chosen by the player, population mix, untouched cells. Additive creation (new main roads, new density clusters) also counts as mutable.
- **Parity budget** (provisional, updated 2026-04-11 second pass) — the maximum allowed divergence between an algorithmic projection and a live-sim re-run of the same scale over the same interval. **Measured empirically** via playtest, mutation testing, and training runs rather than a single static numeric threshold. Still gates reconstruction fidelity.
- **Shaping event** (provisional, added 2026-04-11 second pass) — a disaster, catastrophe, or unexpected milestone injected into a scale's evolution (fire, flood, earthquake, drought, pandemic, boom, crash). Has a taxonomy, frequency/severity curves, and **preparedness mechanics** the player can invest in to reduce impact. Cross-scale from day one.
- **Defense structure** (provisional, added 2026-04-11 second pass) — building family the player can place to reduce shaping-event impact. Preparedness has a resource cost; evolution outcomes for a dormant scale depend on how well the player prepared it before switching away.
- **Scale sovereign** / **NPC leader** (provisional, **deferred**) — the AI entity associated with a dormant scale. **Not in scope for the three-scale MVP** (see resolution of Q-new-11 and Q-new-13 in §10). Until reintroduced, the player is the only actor; dormant-scale outcomes depend only on prior player preparation and shaping events.
- **Reconstruction** (provisional) — materializing a playable live scale state from its snapshot + the parent entity's pending evolution delta up to "now". Happens at scale-switch time.
- **Procedural scale generation** (provisional) — creation of a never-visited scale node (city, region) from parent-scale parameters + deterministic seed. Generalization of the earlier "procedural city generation".
- **Scale switch** (provisional, **renamed** 2026-04-11 second pass from *scale zoom*) — the UI-driven transition from one active scale to another, triggered from a top-bar button panel analogous to the existing speed-control panel. Steps: (a) save the leaving scale, (b) apply the entering scale's pending evolution delta, (c) lazy-load the entering scale into playable form (center-out, loading screen).
- **Scale unlock** (provisional) — persistent per-save state recording which scales the player has earned access to. Starts with city unlocked only. Trigger metrics: **population threshold** plus **% of map used through construction or decisions** (per resolution of Q-new-4).
- **Multi-scale save tree** (provisional, **re-shaped** 2026-04-11 second pass) — hierarchical save data stored **relationally**: a main `game_save` table + per-scale tables (`city_nodes`, `region_nodes`, `country_nodes`), each holding per-scale cell data as a JSON column plus typed foreign-key columns for structural links. Replaces the earlier "single `jsonb` document" framing from the first pass.
- **City cell** / **Region cell** / **Country cell** (provisional, added 2026-04-11 second pass) — scale-specific refinements of the generic `Cell` concept. Same isometric grid primitive at each scale, but sized and semantically typed per scale (city cell = current `Cell`; region cell = coarser; country cell = coarsest). Cell size and zoom levels are preserved across scales. Implies a cell-type refactor of the current `Cell` API — see Q-new-21.
- **Climate** (provisional, added 2026-04-11 second pass; refined third pass per Q-new-18) — first-class per-scale attribute. City: **exactly one biome** with a seasonal cycle. Region: multiple climates possible inside one region (coastal vs inland, gradients). Country: polar regions and climate gradients. Drives a **weather event** sub-system (wind speed/direction, frosts, storms, droughts, heat waves) that couples into desirability, happiness, pollution dispersion, demand, and a new **agricultural zone** family under industrial-light zoning. Lands in the city MVP (Step 1), not parked for region.
- **Weather event** (provisional, added 2026-04-11 third pass) — climate-driven sub-system: wind speed + direction, frosts, storms, droughts, heat waves. Continuous low-severity tail of the shaping-event spectrum. Distinct from but adjacent to **shaping events** — see Q-new-31 for the boundary question.
- **Agricultural zone** (provisional, added 2026-04-11 third pass) — new building family treated as a sub-type of **industrial-light zoning**. First climate-sensitive city system; canonical testbed for the climate v1 hooks (frost damage, drought failure, seasonal yield).
- **Expropriation** (provisional, added 2026-04-11 third pass; reinvoked from a previously parked concept) — escalation path when a parent-scale evolution algorithm cannot replan around a player-authored invariant. The parent flags the target cell, the player sees a notification at scale-switch-in time, and must approve (with happiness/money cost) or counter-plan (accepting a growth penalty). See §2.18.
- **Construction-plan model** (provisional, added 2026-04-11 third pass) — per-element planning struct (analogous to `PathTerraformPlan` for roads) describing how that element type composes with the forbidden-cell mask, what it returns on success, replan, or failure, and how it escalates to expropriation. Each new evolution-placeable element type ships its own model. See §2.18 and Q-new-29.
- **Defense structure family** (provisional, added 2026-04-11 third pass; replaces vague `Defense structure` row) — concrete starter set of preparedness mechanics: **elevated construction** (placement rule), **no-build zone declaration** (player invariant), **early-warning station** (new building), **concrete containment wall** (linear building family), **artificial channel terraforming** (terraform op via the existing road-preparation family). Cost surface drives shaping-event outcomes for dormant scales.
- **Inter-city tax transfer** (provisional, added 2026-04-11 third pass) — region-scale fiscal mechanic that lets the region shift money between its child cities (give to one, levy more from another). City-internal budget categories remain city-controlled; the region moves only top-line transfers. New tax type pending design — see Q-new-32.
- **Progressive scale-switch load** (provisional, added 2026-04-11 third pass) — the loading shape committed in §2.19: dashboard renders first (<300 ms), map loads center-out / player-last-position-first in chunks, input gates on the first interactive chunk (<2 s), background fills the rest. Drives Step 2 stage 5 implementation shape.
- **Parent-scale stub** (provisional, added 2026-04-11 second pass) — minimum conceptual representation of a parent scale inside the city MVP: `region_id` + `country_id` references, neighbor-city stubs at interstate borders, a world-climate pointer, interstate-flow semantics. Shipped in Step 0 so the city has a parent to be a child of from day one, even though regions and countries are not yet playable.
- **Event bubble-up** (provisional — scope reduced) — structured event propagation upward through the scale stack, applied **at scale-switch time** (not continuously). E.g., switching out of a city whose last year contained a large fire re-labels that city's child-scale entity in the region view.
- **Constraint push-down** (provisional — scope reduced) — parameter propagation downward through the scale stack, applied **at scale-switch time**. E.g., country policy multipliers are baked into a region's child-scale entities (and, transitively, their city children) when the player switches down.

---

## 4. Master plan skeleton

**Four** steps plus a **pre-Step-0 preparation block**. Steps 5+ (world, solar, long tail) are explicitly out of the first-playable multi-scale MVP and listed only as "parked" at the end.

> Step numbering shifted 2026-04-11 (second pass): a new **Step 0 — Parent-scale conceptual stubs** is inserted before the former "Step 1". The remaining steps keep their labels ("City MVP close", "Multi-scale infrastructure", "Region + Country MVP") but are now Step 1, Step 2, Step 3 under Step 0. Step 0 is the earliest work in the plan, not a prerequisite "outside" the plan.
>
> Third pass (2026-04-11) inserts a **Step −1 — Process & lifecycle preparation** *before* Step 0 — see Q-new-27 resolution and §2.20. Step −1 operates on IA / skills / subagents (not on game code), so it does not delay Step 0's code work; the two could even overlap if a second agent picks up Step 0 stability work. Step −1 is the master plan's first task because the lifecycle skill rewrite is a precondition for the rest of the plan to be executable by agents at all.

### Step −1 — Process & lifecycle preparation

**Goal:** Land the agent / process infrastructure the rest of the master plan depends on **before** any game-code work starts. Three deliverables: (a) the lifecycle skill rewrite (Q-new-27 resolved), (b) the process-engineering gap closures from §2.20, (c) the backlog triage pass that promotes this brainstorm into the global orchestrator (Q-new-7 resolved).

Rationale: §2.20 + Q-new-27 + Q-new-7. The current `project-spec-*` skills assume one BACKLOG row → one project spec. The §6 hierarchy assumes orchestrator documents that those skills do not yet understand. Running Step 0 without first teaching the skills the new shape produces friction on every single project spec for the rest of the plan.

**Exit criteria (provisional):**

- Lifecycle skills (`project-spec-kickoff`, `project-spec-implement`, `project-spec-close`, `project-stage-close`, `project-new`) understand the orchestrator-vs-project-spec distinction (§6.3) and refuse to closeout an orchestrator document.
- A new orchestrator-close recipe exists (or `project-stage-close` is extended) — Q-new-8 resolved-and-implemented, not just resolved-on-paper.
- A `spec-reviewer` subagent (or equivalent) sits between `spec-kickoff` and `spec-implementer` and is wired into the `/kickoff` flow (process-engineering gap #2 from §2.20).
- A per-phase risk / preflight checklist template exists in `ia/templates/` and is referenced by `spec-implementer` (gap #3).
- A per-phase budget convention is documented in `ia/rules/` and `spec-implementer` honors it (gap #5).
- Project specs carry an explicit S/M/L size label in frontmatter; lifecycle skills branch on it (gap #10).
- Backlog triage pass is executed: existing rows obsoleted by multi-scale are deleted, surviving rows are rewritten, new rows under § Multi-scale simulation lane are filed.
- Global orchestrator `ia/projects/multi-scale-master-plan.md` is created from the stabilized §4 skeleton (per §6.4 promotion rule), and this brainstorm doc is linked to it but **not yet deleted**.

**Candidate stages (draft — rewrite freely):**

1. **Lifecycle skill rewrite.** Walk every `project-spec-*` skill against the §6 hierarchy. Rewrite where the assumption "spec ↔ BACKLOG row" breaks. Add an orchestrator-close recipe.
2. **Process-engineering gap closures.** Implement gaps #2, #3, #5, #10 from §2.20 in IA + subagents. Defer #1, #6, #7 until after the master plan is created (they consume process metrics that don't exist yet).
3. **Backlog triage pass.** Single dedicated session walking the full BACKLOG against this brainstorm. Mark for deletion / rewrite / new-file. File all new § Multi-scale simulation lane rows.
4. **Brainstorm → orchestrator promotion.** Create `ia/projects/multi-scale-master-plan.md` from the §4 skeleton. Link this brainstorm as the seed history. Do **not** yet create per-step or per-stage orchestrators (they materialize lazily, §6.4).

### Step 0 — Parent-scale conceptual stubs

**Goal:** Make the conceptual existence of a parent region and parent country **visible in city code and save data** before the city MVP is closed. Step 0 ships nothing playable at region or country scale — it only guarantees that the city knows it is a child of something, and that the interstate borders and neighbor-city stubs the city already renders are backed by real (even if placeholder) parent-scale references.

Rationale: §2.10. If the city MVP is closed without parent references, every downstream step has to retrofit them into existing save data. Step 0 pays that tax upfront while the blast radius is small.

**Exit criteria (provisional):**

- Every city save carries a non-null `region_id` and `country_id` (placeholder values are allowed; the point is the column exists and is wired).
- At least one **neighbor-city stub** is present at an interstate border and is readable by the city sim (it does not yet evolve — it is inert content).
- The city reads its climate value via a **world-climate pointer** (even if that pointer returns a single constant in Step 0 — see §2.13).
- Interstate connections in the current city model "flow to/from a neighbor region" in their data shape, not just "off-map exit". No behavior change required; only the data shape needs to admit a region-facing interpretation.
- Save/load round-trips all of the above with no regression against the current city-only save path.
- The **cell-type split refactor is executed** in Step 0 (not just decided): the current `Cell` API splits into `CityCell` / `RegionCell` / `CountryCell` (or an equivalent shape that admits scale-specific semantics on a shared isometric primitive). Per Q-new-21 resolution, semantic preparation refactors for the scale structure must land in Step 0, not deferred.

**Candidate stages (draft — rewrite freely):**

1. **Parent-id wiring.** Add `region_id` / `country_id` references to city save data; default them to placeholder constants; verify load / save round trip.
2. **Neighbor-city stubs.** Surface at least one neighbor-city stub at an interstate border as readable inert content; cover it with a test-mode scenario.
3. **World-climate pointer.** Introduce the climate pointer API at city level (single constant value is fine); couple to whichever existing city system is easiest to sanity-check.
4. **Interstate border semantics.** Audit the current interstate-border code and record what minimal change is needed so it reads as "flow to/from a parent-region neighbor" rather than "off-map exit". May or may not ship code changes in Step 0 — may be an audit-only stage whose output is the Step 1 stability stage's marching orders.
5. **Cell-type refactor execution.** Split the current `Cell` API into scale-specific cell types (`CityCell` / `RegionCell` / `CountryCell` or equivalent). Per Q-new-21 resolution this is **executed** in Step 0, not deferred. Stage exits when the city sim builds and runs against the new types with no behavior regression.

### Step 1 — City MVP close

**Goal:** The city scale feels like a finished, stable city-builder loop. No crashes, observable metrics, meaningful gameplay tension, player agency to shape the city. After Step 1, the city model is **frozen enough to be used as an aggregation source** in Step 2 without ongoing churn.

**Exit criteria (provisional):**
- No crasher or data corruption bug open at city scale.
- Player can read, at a glance, how the city is doing (dashboard + charts).
- At least one gameplay tension loop beyond "place zones, money goes up" (service coverage, pollution consequences, districts, ...).
- Cities visually look right (growth gradient, no persistent grass gaps, working minimap).
- A single city tick is cheap enough that the **prospect** of running many aggregated cities in parallel is credible (concrete target to be set in a Step 2 stage, not now).
- **Climate v1 lands** (§2.13): city holds a single climate type with at least a minimal seasonality hook and couples into one existing system (agriculture / zone demand / pollution — pick the lowest-cost coupling first).
- **Parent-scale stubs from Step 0 are consumed** by at least one city system (e.g., neighbor-city stub influences a demand signal, interstate border reads as a region-flow edge).

**Candidate stages (draft — rewrite freely):**

1. **Stability.** Close BUG-55 (crashers + data corruption + sim logic), BUG-48 (minimap stale), BUG-52 (AUTO zoning grass gaps), BUG-20 (utility building load visual), BUG-16/17 (init races), BUG-28/31 (sorting/prefab). The city must not lose or corrupt state.
2. **Observability.** FEAT-51 (data dashboard + charts) + TECH-82 Phase 1 (`city_metrics_history`). Player and designer can both read what the sim is doing. Prerequisite for trusting any future aggregate.
3. **Economic depth & tension.** FEAT-52 (services coverage), FEAT-08 (density evolution + spatial pollution), FEAT-53 (districts), and the already-shipped monthly maintenance / tax→demand loop. Turns the city into a system with stakes.
4. **City shape correctness.** FEAT-43 (growth ring gradient tuning), and the parts of FEAT-47 (multipolar centroids) that are still city-internal. The city must look like a city to a human eye before it can be a node in a region.
5. **Quality-of-life player agency.** FEAT-35 (area demolition), FEAT-03 (forest hold-to-place), and the smaller QoL features that stop manual editing from hurting.
6. **Performance envelope for later aggregation.** TECH-16 (tick perf v2, harness labels), BUG-14 (per-frame `FindObjectOfType`), TECH-26 (prevention scanner). Measure where the single-city tick spends its time. Don't micro-optimize yet, but produce the harness.
7. **Climate v1 + parent-stub consumption.** Introduce the concrete climate model from §2.16: single biome per city, seasonal cycle, weather events (wind speed/direction, frosts, storms, droughts, heat waves), and the new **agricultural zone** family under industrial-light zoning. Wire climate into desirability, happiness, pollution dispersion, and demand. At least one city system must also read the Step 0 parent-scale stubs for real. New FEATs pending — do not file yet; wait for the backlog triage pass (Q-new-7 resolution, scheduled in Step −1).
8. **Shaping-events stub.** Minimum viable shaping-events framework (§2.12 + §2.17 starter taxonomy). Ships **one concrete disaster type** (candidate: **local storm flood**, because it composes cleanly with the §2.16 weather events), a **preparedness cost surface**, and **at least two defense-structure options** from the §2.17 starter family (candidates: no-build zone declaration + early-warning station). Full taxonomy expands in Step 2 stage 6. New FEAT pending.

### Step 2 — Multi-scale infrastructure (minimum)

**Goal:** Build the **scale-neutral spine** needed to support any number of scales, **without yet adding a second playable scale**. After Step 2, the repo contains: pure-compute sim modules, a `SimulationScale` enum and model contract, a **snapshot schema** for the city scale, a **relational multi-scale save** (main `game_save` table + per-scale tables with JSON cell-data columns — see §2.14, §3 glossary, and Q-new-22), a **city evolution algorithm** owned by a parent-region entity (pure function, scale-specific), a snapshot-based freeze/reconstruct path, a procedural-scale generator, a scale unlock mechanism, a **scale switch** transition UX skeleton (top-bar button panel, §2.11), and the **shaping-events framework** (§2.12) promoted from its Step 1 stub into a cross-scale system — all exercised by **exactly one scale (city)**, which now plays the role of both "active" and "evolved dormant" in isolation tests.

**Exit criteria (provisional):**
- A city can be put into **dormant mode** (snapshot + evolution params held by a parent-region entity, §2.14), advanced by the city evolution algorithm over an arbitrary Δt, and reconstructed into a playable live city such that the divergence from a full live-sim run over the same interval stays inside the **parity budget** (measured empirically per the resolution of Q-new-2, not as a single static threshold).
- A procedural city can be generated from region-like parameters + seed and loaded as a normal `GameSaveData`.
- The save format holds cities inside a region inside a country node in a **relational schema** (`game_save` main + per-scale tables with JSON cell-data columns), even if region and country evolution algorithms are stubs.
- **Switching** out of a city and back in round-trips correctly: snapshot → parent-entity pending delta (Δt=0) → reconstruct produces the same city state modulo parity budget.
- The scale unlock state is persisted and queryable; Step 2 ships with "only CITY unlocked" as the default. Unlock trigger uses the **population + % map-usage** metric pair (per resolution of Q-new-4), even if the Step 2 harness only exercises it against a single scale.
- Compute-lib modules are callable headless (without Unity) for at least one non-trivial city sub-system, **and** for the city evolution algorithm.
- **Every scale runs on the same real-time clock** (§2.4). No per-scale time-dilation clock is ever introduced.
- **Shaping-events framework** (§2.12) is generalized from its Step 1 stub: multi-event-type taxonomy, severity curves, preparedness hooks, defense-structure family. Exercised against the city scale in Step 2; consumed by region/country in Step 3.

**Candidate stages (draft):**

1. **Pure-compute foundation.** TECH-38 (core compute modules), TECH-15 (geography init performance via compute-lib), TECH-34 (`GridManager` region manifest), TECH-32/TECH-35 research as gated spikes. Precondition for running a city tick **and** a city evolution algorithm anywhere that isn't the main Unity loop.
2. **Entity model and relational save.** TECH-82 Phases 1→4 (metrics history, `city_events`, `grid_snapshots`, `buildings` identity) — **scoped down**: the `city_events` delta log is no longer the spine of dormant updates, but it is still useful for live-sim observability inside the active scale. TECH-81 (knowledge graph, only if scale dependency queries justify it). TECH-18 **resolved to yes, relationally**: a main `game_save` table + per-scale tables (`city_nodes`, `region_nodes`, `country_nodes`), each with a JSON column for its cell data and typed columns for structural links and evolution parameters (see §3 glossary and Q-new-22). New TECH issue needed to own the save migration.
3. **Scale-neutral spine.**
   - `SimulationScale` enum + `ISimulationModel` contract (new FEAT).
   - **Scale snapshot schema** — per-scale snapshot struct (city first) capturing everything evolution-invariant + everything the evolution algorithm needs as input (new FEAT). Replaces the earlier "city aggregate digest" item.
   - Multi-scale relational save schema (new FEAT — replaces the first-pass `jsonb`-backed save tree).
   - **Scale-switch-time** event bubble-up / constraint push-down (new FEAT — scope reduced: no continuous bus, only switch-time projection).
   - **Single shared real-time clock** — no dilation (§2.4). The existing game clock is promoted to scale-neutral and every scale reads from it directly (new FEAT, much smaller than a dilation clock).
   - **Scale unlock state** — persisted per save, with an API for scales to become available (new FEAT). Trigger metric: population + % map usage (Q-new-4 resolution).
   - **Child-scale entity model** — the parent-owned representation of a dormant child, holding its pending evolution delta (new FEAT, see §2.14).
4. **City evolution algorithm + reconstruct-generate loop.**
   - **City evolution algorithm** — hybrid `evolve(snapshot, Δt, params, events) → snapshot'` owned by the parent region entity, with seeded RNG and a shaping-events channel (new FEAT). Operates on evolution-mutable state only; preserves evolution-invariant state verbatim.
   - **Snapshot → live reconstruction** — rebuild a playable live city state from a snapshot + the parent entity's pending delta (new FEAT).
   - **Procedural scale generator** — materialize a never-visited city from parent-scale parameters + seed (new FEAT, consumes FEAT-46 parameter pipeline). Generalizes to region later.
   - **Parity budget harness** — empirical comparison between `evolve(snapshot, Δt)` and a full live-sim run over the same Δt, via playtest + mutation + training runs (new TECH, per Q-new-2 resolution). Gates the stage.
5. **Scale switch UX skeleton.** Top-bar button panel (§2.11) analogous to the existing speed-control panel, exposing scale selection. Click → save the leaving scale → apply pending delta → lazy-load the entering scale (center-out, loading screen). For Step 2, the only target is a stub region view, but the transition runs the full snapshot → pending-delta → reconstruct round trip against real city state. Ships the **scale unlock** UX entry point too (even if only CITY is unlocked at this step).
6. **Shaping-events framework (cross-scale).** Promote the Step 1 single-event stub into a real framework (§2.12): event taxonomy, frequency/severity curves, preparedness hooks, defense-structure family. Exercised against the city scale within Step 2.
7. **Harness + tests.** TECH-31 scenario generator extended to emit snapshot-shaped scenarios and parity-budget round-trip scenarios. `agent-test-mode-verify` covers a scale-switch-out → evolve(Δt=N years) → scale-switch-back-in round trip. `docs/agent-led-verification-policy.md` updated if new driver kinds are needed. Test cases are driven through the same agent ↔ Unity bridge tooling already in use (per Q-new-10 resolution).

### Step 3 — Region + Country MVP

**Goal:** Two additional scales (region and country) become **playable as the active scale**, each with its own live-sim tick loop **and** its own evolution algorithm (used when it is dormant). After Step 3, we have the **three-scale MVP** — proof that the architecture generalizes from one scale to N, with the active/dormant asymmetry exercised in both directions.

**Exit criteria (provisional):**
- Player can switch from a city to a region map, see other cities reconstructed from their parent-entity snapshots + pending deltas, play the region as the active scale (live sim), and switch back in to any city (visited or procedural) without state loss and inside the parity budget.
- Player can switch further to a country map, play the country as the active scale, and exercise a **minimum head-of-state loop**: assign a national budget, launch at least one national infrastructure project that propagates down to a region/city, set one international-relations posture, declare the territorial/border adjustments needed to found a new region, and create at least one new region node (expanding the map).
- A country policy change made while the country is the active scale is **baked into region and city evolution parameters** the next time the player switches down.
- A city event that occurred during live city play (e.g., a large fire) is **bubbled up at switch-out time** and visible in the region and country dashboards.
- Dashboard is a **shared screen with per-scale read/write permissions** and surfaces key stats from other scales alongside the active scale's stats (see §3 glossary "Dashboard" and resolution of Q-new-9).
- Save/load preserves all three scales end-to-end in the relational schema (game_save main + per-scale tables).
- Region unlock and country unlock are exercised: the player starts with only CITY unlocked, crosses the region unlock threshold (population + % map usage), then the country unlock threshold.
- At least one **economic flow** crosses scales: city exports feed inter-city trade in the region layer's live sim, and the resulting inter-city trade balance feeds back into city evolution parameters when the player switches down.
- **Region has natural resources, multi-climate geography** (coastal vs inland, possibly polar or equatorial bands), and the ability to **found new cities** inside the region.
- **Country owns international relations**, can **declare war** and expand borders to host new regions, manages **natural resources as a national priority**, and supports **polar regional climate gradients**.
- **Player-authored dormant control is live**: from the region view, the player can tune the evolution parameters of dormant cities in that region; from the country view, the player can tune the evolution parameters of dormant regions (see §2.15 / resolution of Q-new-15).
- **Auto mode** exists at region and country scale — a hands-off default that keeps the scale ticking without per-decision player input.

**Candidate stages (draft):**

1. **Region sim model (active + evolution).** Coarse region grid/graph using **region cells** (§3 glossary); nodes = city child-entities, ports, farmland, wilderness; edges = road/rail/sea lanes. Isometric map convention, cell size, zoom levels, and speed controls are preserved from city scale.
   - Active-scale region tick: migration pressure, trade flow solver, regional economy, new-trunk-infrastructure placement, founding new cities, regional projects (big industrial plants, regional parks, regional infrastructure — a brainstorm surface of its own).
   - **Region evolution algorithm**: function analogous to the city one, operating on region evolution-mutable state (aggregate migration, regional economy levels, optional new major roads), runs at scale-switch time when the region is dormant.
   - **Multi-climate geography**: coastal and lake shorelines crossing multiple cities, rivers threading through cities, climate gradients across the region.
   - Re-scopes FEAT-09 (trade/production/salaries) to the region active-tick layer. Consumes FEAT-47 multipolar for connurbation.
2. **Country sim model (active + evolution + head-of-state loop).** Political/policy layer, using **country cells**.
   - Active-scale country tick: advances elections, budget execution, international relations, national infrastructure projects, region creation, war/border expansion, natural-resource allocation.
   - **Country evolution algorithm**: when the country is dormant, fast-forwards via long-period political/economic drift. No NPC-leader modifiers in the MVP — NPCs are deferred (Q-new-11, Q-new-13 resolved).
   - **Head-of-state loop (minimum playable)**:
     - Assign national budget across a small fixed set of categories.
     - Launch **national infrastructure projects** that touch one or more child regions/cities (e.g., a cross-region highway, a national power grid upgrade).
     - Set **international-relations stance** against one to three neighboring countries (stubbed — neighbors can be inert). Includes the ability to **declare war** and, conditionally, expand borders.
     - Reorder national priorities (affects next-tick resource allocation).
     - **Create a new region node** inside the country's territory and fix its initial evolution parameters (consumes the procedural scale generator from Step 2).
     - Treat **natural resources as a national priority** — allocate extraction rights, strategic reserves, export policy.
3. **Inter-scale wiring (scale-switch-time).** Full event bubble-up + constraint push-down across all three scales, applied at switch time. **Shared dashboard** (FEAT-51 extension) with per-scale read/write permissions and cross-scale key stats — replaces the earlier "drill-down rollup" framing (see resolution of Q-new-9 in §10). All non-active cities in the active region stay dormant and are evolved on demand via their parent region's child-scale entities.
4. **Procedural content at region scale.** Unvisited region nodes generate plausible cities on demand (Step 2 generator, now driven by region sim parameters). FEAT-15 ports + FEAT-16 trains + FEAT-39 sea + FEAT-10 regional bonus evolved into real region-layer content.
5. **Player-authored dormant control.** Region UI exposes tunable evolution parameters for dormant cities owned by that region. Country UI exposes tunable evolution parameters for dormant regions. Granularity still to be decided (see Q-new-26). Consumes the child-scale entity model from Step 2 stage 3.
6. **Shaping-events framework promoted to region and country scale.** Region-scale disasters (drought across multiple cities, regional flood, industrial-belt collapse); country-scale disasters (pandemic, trade crisis, sovereign default). Consumes the Step 2 framework directly.
7. **Auto mode at region and country scale.** Hands-off default per scale so the player can set policy, switch out, and still have their dormant regions/countries advance sensibly. Couples into the player-authored parameters from stage 5.
8. **Playability pass (per-scale minimum player loops).** Each scale ships a minimum coherent player loop:
   - **City**: already established in Step 1.
   - **Region**: influence migration, trade, inter-city infrastructure; place or upgrade regional trunk roads/rails/ports; found new cities.
   - **Country**: the head-of-state loop from stage 2 above.
9. **Multi-scale save, load, and test harness.** Scenario generator (TECH-31) extended to multi-scale scenarios (shape defined in the master-plan doc per Q-new-10 resolution); compile-gate + test mode driver coverage for three scales via the existing agent ↔ Unity bridge tooling; parity-budget checks for region and country evolution algorithms. Verification policy updated.

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

From the brainstorm turns. These do not yet have BACKLOG ids. File them under **§ Multi-scale simulation lane** in `BACKLOG.md` when the plan stabilizes — per the resolution of Q-new-7, the backlog triage pass runs at brainstorm-close / master-plan-creation time, as the first task of the master plan.

> Rewritten 2026-04-11 (second pass). Items related to time-dilation clocks, jsonb-single-document save trees, and NPC leaders are **removed** or replaced. Items related to Step 0, climate, shaping events, player-authored dormant control, relational save, and scale-switch UX are **added**.

**Parent-scale conceptual stubs (Step 0):**

1. **Parent-scale stub feature** — wire `region_id` + `country_id` references into city save data, neighbor-city stubs at interstate borders, a world-climate pointer, interstate-border "flow to neighbor" data semantics.
2. **Cell-type split plan** — decision artifact + stub refactor for `Cell` → `city cell` / `region cell` / `country cell`. Execution may happen in Step 1 or Step 2; the decision lives in Step 0 (see Q-new-21).

**City MVP extensions (Step 1):**

3. **Climate v1 (city)** — single biome per city, seasonal cycle, weather events (wind speed/direction, frosts, storms, droughts, heat waves), coupling into desirability / happiness / pollution dispersion / demand. See §2.16.
3a. **Agricultural zone family** — new building family treated as a sub-type of industrial-light zoning. First climate-sensitive city system; canonical testbed for the climate v1 hooks. See §2.16 and Q-new-35.
4. **Shaping-events stub** — one concrete disaster type (candidate: local storm flood, composing with the §2.16 weather events), preparedness cost surface, at least two defense-structure options from the §2.17 starter family. Full framework lands in Step 2.

**Scale-neutral spine (Step 2):**

5. **Simulation scale manager** — `SimulationScale` enum, `ISimulationModel` contract, active-scale switch, transition API.
6. **Child-scale entity model** — parent-owned representation of a dormant child (holds last snapshot + pending evolution delta + evolution parameters). See §2.14.
7. **Scale snapshot schema** — per-scale snapshot struct (city first) capturing everything evolution-invariant + everything the evolution algorithm needs as input.
8. **Multi-scale relational save** — main `game_save` table + per-scale tables (`city_nodes`, `region_nodes`, `country_nodes`), each with a JSON column for cell data and typed columns for structural links. Replaces the first-pass jsonb-single-document framing.
9. **Single shared real-time clock** — promote the existing game clock to scale-neutral; every scale reads from it. No dilation.
10. **Scale-switch-time event bubble-up / constraint push-down** — cross-scale event and parameter transport, applied only at switch time.
11. **Dormant scale freeze + pending-delta application** — pause full city tick; evolution advances the parent-entity's pending delta; applied at switch-in time.
12. **Snapshot + pending-delta → live reconstruction** — rebuild a full live city state from a snapshot plus its parent's pending delta.
13. **Procedural scale generator** — materialize a never-visited city (later region) from parent-scale parameters + seed. Consumes FEAT-46.
14. **Scale switch UX (top-bar button panel)** — button strip analogous to the existing speed-control panel; save-on-click + lazy-load center-out + loading screen.
15. **Scale unlock** — persisted per save; unlock trigger = population threshold + % map usage; API for unlocking scales.
16. **Shaping-events framework (cross-scale)** — promoted from the Step 1 stub: full §2.17 starter taxonomy (regional/local earthquakes, regional/local tsunamis, local landslides, local storm floods, local river floods), frequency/severity curves, preparedness mechanics, complete defense structure family (elevated construction, no-build zone declaration, early-warning station, concrete containment wall, artificial channel terraforming).
16a. **Construction-plan model framework** — per-element planning structs (analogous to `PathTerraformPlan` for roads) for every new evolution-placeable element type. Composes with the forbidden-cell mask, returns success / replanned / needs-expropriation. See §2.18 and Q-new-36.
16b. **Expropriation mechanic** — escalation path when a parent-scale evolution algorithm cannot replan around a player-authored invariant. Player notification + happiness/money cost + counter-plan option. Reinvoked from a previously parked concept. See §2.18 and Q-new-29.
16c. **Progressive scale-switch loader** — implementation of the §2.19 loading shape: dashboard renders first, map builds in chunks center-out / player-last-position-first, input gates on first interactive chunk. Affects Step 2 stage 5.
17. **Parity budget harness** — empirical parity checks via playtest + mutation + training runs.

**Region + Country content (Step 3):**

18. **Region simulation model** — coarse grid/graph using region cells, tick loop, migration/trade/economy, regional projects, multi-climate geography, new-city founding.
19. **Country political & policy layer** — elections, budget, trade, immigration, international relations, war/border expansion, natural-resource policy; head-of-state loop (§4 Step 3 stage 2).
20. **Inter-city trade network solver** — distributes production/consumption across region graph edges.
21. **Player-authored dormant-scale control** — region UI for tuning dormant-city evolution parameters; country UI for tuning dormant-region parameters (promoted from moonshot to core by Q-new-15 resolution).
22. **Auto mode (region + country)** — hands-off default per scale.
23. **Shared dashboard** — FEAT-51 extension with per-scale read/write permissions and cross-scale key stats (replaces the earlier "drill-down rollup" framing).
24. **Multi-scale scenario tests** — extends TECH-31 scenario generator to three-scale round trips, run through the existing agent ↔ Unity bridge tooling.
24a. **Inter-city tax transfer** — region-scale fiscal mechanic that lets the region give money to one child city or levy more from another, without touching city-internal budget categories. New tax type. See §3 glossary "Inter-city tax transfer" and Q-new-32.

**Process & lifecycle preparation (Step −1):**

P1. **Lifecycle skill rewrite** — `project-spec-*` skills taught the orchestrator-vs-project-spec distinction (§6.3) + new orchestrator-close recipe. Q-new-27 resolved.
P2. **`spec-reviewer` subagent** — third independent reader between `spec-kickoff` and `spec-implementer`. §2.20 gap #2. Scope/verdict shape pending — see Q-new-38.
P3. **Per-phase risk / preflight checklist template** — declarative impact / reversibility / touched-dependencies checklist consumed by `spec-implementer`. §2.20 gap #3.
P4. **Per-phase budget convention** — soft step / token / time limit per phase before prompting for human confirmation. §2.20 gap #5.
P5. **Spec size classification (S/M/L)** — frontmatter label + lifecycle-skill branching. §2.20 gap #10.
P6. **Backlog triage pass** — single dedicated session walking the full BACKLOG against this brainstorm; deletes / rewrites / new files. Q-new-7 resolved, scheduled here as Step −1 stage 3.
P7. **Brainstorm → orchestrator promotion** — create `ia/projects/multi-scale-master-plan.md` from the §4 skeleton. Step −1 stage 4.

**Parked (post-MVP, unchanged role):**

25. **World climate + commodity trade macro** — parked for post-MVP. The Step 0 world-climate pointer is the forward stub.

---

## 10. Open questions

Split by resolution wave: **§10.A** original questions resolved 2026-04-11 first pass · **§10.B** carry-overs resolved second pass · **§10.C** Q-new-1..15 resolved second pass · **§10.D** Q-new-16..27 resolved third pass · **§10.E** Q-new-28 still open (deferred to a separate thread) · **§10.F** Q-new-29..38 planted third pass.

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

### 10.B. Resolved — 2026-04-11 second pass (carried-over from §10.B)

All three carried-over questions are resolved by the second pass.

8. ~~**Save format versioning.**~~ **Resolved → deferred until first stable version.** No versioning strategy is designed now; we will ship the first stable save schema before worrying about migrations. Revisit when the three-scale MVP stabilizes.
10. ~~**Multipolar FEAT-47 split.**~~ **Resolved → one concept, two scales.** Inside a city scale, multipolar urban poles are districts/communes of the same city. At region scale, those multipolar cities appear as "shapes" on the region map composed of urbanization cells (coarser, but analogous). Same underlying concept rendered at two levels of detail. Still a candidate for a row split in the backlog triage pass (Q-new-7), but the design question is closed.
15. ~~**World scale sneak-in.**~~ **Resolved → climate in city MVP + Step 0 conceptual stub.** Climate is introduced in the city MVP (§2.13). A conceptual parent-region and parent-country stub lands in **Step 0** (§2.10 + §4 Step 0) — new step inserted before the former Step 1. The save format does not yet need a `world` root in Step 0; world-scale climate is read through a pointer that can hold a constant.

### 10.C. Resolved — 2026-04-11 second pass (Q-new-1..Q-new-15)

All 15 questions planted in the first 2026-04-11 pass are resolved by the second pass. Strikethrough + decision + pointer to where each one now lives in the body.

- **Q-new-1.** ~~Evolution algorithm shape.~~ **Resolved → hybrid (deterministic backbone + shaping-events channel).** Outcomes depend on how well the player prepared the entity before leaving. Preparedness has a resource cost and is its own design surface. See §2.7 refinements, §2.12, §3 glossary "Evolution algorithm".
- **Q-new-2.** ~~Parity budget units.~~ **Resolved → empirical (playtest + mutation + training).** No single static numeric threshold. The Step 2 parity-budget harness is an empirical measurement system, not a fixed bound. See §3 glossary "Parity budget".
- **Q-new-3.** ~~Evolution-invariant vs mutable split (city).~~ **Resolved → invariant = whatever the player touched.** Main road backbone, landmarks, districts, player-assigned budgets, explicit zoning decisions. Evolution may additively create new main roads and density. Writes flow through the parent-region entity before load. Since NPCs are deferred, the player is the only actor. See §2.7 refinements, §2.14, §3 glossary "Evolution-invariant / mutable".
- **Q-new-4.** ~~Scale unlock trigger.~~ **Resolved → population + % map usage.** Population threshold combined with percentage of map usage (through construction or explicit decisions). See §3 glossary "Scale unlock" and Step 2 exit criteria.
- **Q-new-5.** ~~Save migration path (jsonb).~~ **Resolved → relational schema, not single jsonb.** Main `game_save` table + per-scale tables (`city_nodes`, `region_nodes`, `country_nodes`), each with a JSON column for cell data and typed columns for structural links. Replaces the first-pass jsonb-single-document framing. Migration path details still need a dedicated TECH spec — see new Q-new-22. See §3 glossary "Multi-scale save tree" and Step 2 stage 2.
- **Q-new-6.** ~~Parallel agent safety map.~~ **Resolved → agent-parallelization problem, not human.** Javier is the only human dev. Parallel work means AI agents working in parallel, not humans. The abstraction needs to be defined now; which tasks to parallelize is a decision for after all tasks are enumerated. See new Q-new-28.
- **Q-new-7.** ~~Backlog triage pass scheduling.~~ **Resolved → at brainstorm-close, as the first task of the master plan.** The triage pass runs when we promote this brainstorm into the master plan, is scheduled as the master plan's opening task, and the following steps renumber around it. Not at end-of-Step-1. See §12 "what the human wants next" (updated).
- **Q-new-8.** ~~Lifecycle skill rewrite scope.~~ **Resolved → deep rewrite expected.** The entire task system will mutate; lifecycle skills will be deeply modified or replaced. Exact scope is deferred until the master plan crystallizes. See new Q-new-27.
- **Q-new-9.** ~~Dashboard drill-down UX.~~ **Resolved → shared screen with per-scale read/write permissions + cross-scale key stats.** Single shared dashboard with permissions gating editing per scale. Cross-scale key stats are surfaced alongside the active scale's stats because that mirrors how the real world works. Replaces the earlier "drill-down rollup" framing from the first pass. See §4 Step 3 stage 3.
- **Q-new-10.** ~~Non-city test scenarios.~~ **Resolved → defined in the master plan, same agent ↔ Unity bridge tooling.** Scenario shape at non-city scales will be specified during master-plan creation, after brainstorm close. Tests run through the same agent ↔ Unity bridge tooling already in use. See §4 Step 2 stage 7 and Step 3 stage 9.
- **Q-new-11.** ~~Does a city remember its mayor.~~ **Resolved → NPCs deferred; no calibration layer in MVP.** NPC leaders are out of scope for the three-scale MVP. The player defines, entities hold those definitions, outcomes vary with shaping events and preparedness. Calibration from past live play is not in MVP. See §3 glossary "Scale sovereign" (marked deferred).
- **Q-new-12.** ~~Event bubble-up / constraint push-down scope at switch time.~~ **Partially resolved.** Framing is clear — switch-time only, large-scale economics push down, disasters/events bubble up, everything affects everything but with explicit limits. The exact function-signature-level hooks still need deeper exploration. See new Q-new-23.
- **Q-new-13.** ~~NPC leader modifier model.~~ **Resolved → deferred.** Same as Q-new-11. No NPC leader model in MVP.
- **Q-new-14.** ~~"Who is touching what" register.~~ **Resolved → agent-session register + context compression.** Durable register keyed by agent session id. Context compression for sub-orchestrator / delegator agents so they stay focused. Agent-focused, not human-focused. See new Q-new-28.
- **Q-new-15.** ~~Player-authored dormant evolution parameters.~~ **Resolved → promoted from moonshot to core.** At region scale, the player controls parameters for dormant cities in that region. At country scale, the player controls parameters for dormant regions. Landed in §4 Step 3 stage 5 and new feature row in §9. Granularity still an open sub-question — see new Q-new-21.

---

### 10.D. Resolved — 2026-04-11 third pass (Q-new-16..Q-new-27)

All thirteen second-pass questions are resolved (or partially resolved with the open piece moved to a new question) by the third pass. Q-new-28 stays open and is restated in §10.E.

- **Q-new-16.** ~~Δt tracking without dilation.~~ **Resolved → trivial calendar stamp.** Same clock across all scales means the leaving-scale calendar timestamp **is** the entering-scale calendar timestamp. Each child-scale entity holds `last_active_at`; pending delta is `(last_active_at → now)`. No hidden difficulty. See §2.4 third-pass clarification.
- **Q-new-17.** ~~Step 0 exit criteria final checklist.~~ **Partially resolved → provisional list accepted as initial draft, not yet locked.** Human accepted the §4 Step 0 exit criteria list as a working starting point; final checklist will be locked before Step 0 stability work begins. New Q-new-29 carries the lock-before-execution requirement.
- **Q-new-18.** ~~Climate v1 scope.~~ **Resolved → single biome + seasonal cycle + weather events + agriculture hook.** Single biome per city; region multi-climate; country multi-climate (polar gradients). Weather events: wind speed/direction, frosts, storms, droughts, heat waves. Couples into desirability, happiness, pollution dispersion, demand, and a new agricultural zone family under industrial-light zoning. See §2.16 and §3 glossary "Climate", "Weather event", "Agricultural zone".
- **Q-new-19.** ~~Shaping-events taxonomy and defense-structure family.~~ **Resolved → starter taxonomy.** Earthquakes (regional/local), tsunamis (regional/local), landslides (local), storm floods (local), river floods (local). Defense family: elevated construction, no-build zone declaration, early-warning station, concrete containment wall, artificial channel terraforming. See §2.17 and §3 glossary "Defense structure family".
- **Q-new-20.** ~~Parent-writes-child conflict model.~~ **Resolved → forbidden mask + replan + expropriation + per-element construction-plan models.** No-build zone is a forbidden-cell mask the evolution algorithm reads. Evolution replans around the mask. When replan fails, escalates to the **expropriation** mechanic (reinvoked from a previously parked concept). Each new evolution-placeable element type ships its own construction-plan model. See §2.18 and §3 glossary "Expropriation", "Construction-plan model".
- **Q-new-21.** ~~Cell-type refactor timing.~~ **Resolved → Step 0, executed (not just decided).** Semantic preparation refactors for the scale structure must happen in Step 0. Step 0 stage 5 changed from "decision artifact" to "execute the split". See §4 Step 0 exit criteria + stage 5.
- **Q-new-22.** ~~Relational save schema shape.~~ **Resolved → proposal accepted as base, deepen later.** The candidate schema (`game_save` main + per-scale `*_nodes` tables with `cell_data jsonb`, `evolution_params jsonb`, `pending_delta jsonb`, `last_active_at`) is the working base. Deeper schema work is deferred to a dedicated TECH spec scheduled in Step 2 stage 2. New Q-new-30 carries the open columns (player invariants, shaping event log location).
- **Q-new-23.** ~~Scale-switch hook function signatures.~~ **Resolved (in framing) → mirror the bidirectional pattern: child evolution writes flow up via the parent entity, parent decisions flow down via the child's pending delta, and on switch-in the entity's accumulated mutations materialize into the entered scale's live state.** Same shape city↔region as region↔country. Concrete function-level signatures still pending implementation; not gating until Step 2 stage 3 opens. New Q-new-31 carries the function-signature outline.
- **Q-new-24.** ~~Scale switch loading budget.~~ **Resolved → 1–2 s progressive, dashboard-first, chunked map load.** Dashboard renders immediately, map builds center-out / player-last-position-first in chunks, input gates on the first interactive chunk. See §2.19 and §3 glossary "Progressive scale-switch load".
- **Q-new-25.** ~~Budget hierarchy across scales.~~ **Resolved → city budget categories stay city-controlled; region only does inter-city transfers.** New mechanic: **inter-city tax transfer** — region can give money to one child city or levy more from another. New tax type pending. See §3 glossary "Inter-city tax transfer". Mechanic details still open — see new Q-new-32.
- **Q-new-26.** ~~Player-authored dormant control granularity.~~ **Resolved (region scale) / partially open (country scale).** Region-view parameters: resource allocation, regional energy-grid connections, transport-line connections, distance-effect constructions near cities, designate the regional capital. Country-view parameters: not yet enumerated by the human. New Q-new-33 carries the country-view enumeration.
- **Q-new-27.** ~~Lifecycle skill rewrite sequencing.~~ **Resolved → before any master-plan implementation work begins.** The rewrite is the first task of the plan, scheduled in the new **Step −1 — Process & lifecycle preparation** block (§4). Runs in parallel with the §2.20 process-engineering gap closures and the Q-new-7 backlog triage pass.

### 10.E. Open — Q-new-28 carried over

- **Q-new-28. Agent-parallelization abstraction.** **Still open. Deferred to a separate brainstorm thread (third-pass decision).** The register + context-compression layer for sub-orchestrator / delegator agents needs an owner and a landing place, but not in this thread. Reopen when the separate thread produces a proposal. Until then, parallel agent work uses the §6.2 convention (no file-level overlap between BACKLOG rows).

### 10.F. New questions planted 2026-04-11 (third pass)

Downstream of the third-pass resolutions and of the process-engineering practices folded into §2.20. Ordered roughly by how soon they gate work.

- **Q-new-29. Step 0 exit checklist — lock-in moment.** Q-new-17 resolved the **content** of the Step 0 checklist as "current draft is good enough"; it did not lock the **moment** at which the checklist becomes immutable for the duration of Step 0. Candidate rule: lock at the end of Step −1 (process & lifecycle preparation), as part of the brainstorm → orchestrator promotion step. Alternative: lock at the start of Step 0 stage 1 (parent-id wiring). Affects whether Step −1 can ship without Step 0 already being scoped.
- **Q-new-30. Relational save schema — open columns.** Q-new-22 accepted the base proposal but deferred two columns: (a) where do **player-assigned invariants** live — typed columns inside the node table, a sibling table, or a JSON column with a typed subset? (b) where do **shaping event logs** live — inside the node row's JSON, or in a dedicated `shaping_events` table joined back to the node id? Needs an answer before the Step 2 stage 2 TECH spec is written.
- **Q-new-31. Scale-switch hook function signatures.** Q-new-23 resolved the **shape** ("evolution writes flow up via parent entity, parent decisions flow down via child's pending delta, materialize at switch-in"). The **concrete function signatures** are still missing. Candidate outline: `IChildScaleEntity.ApplyPendingDelta(snapshot, deltaParams) → snapshot'`, `IScaleSwitch.Out(activeScale, leavingTimestamp) → snapshotBundle`, `IScaleSwitch.In(targetScale, snapshotBundle, parentDelta) → liveState`. Needs concrete C# / compute-lib signatures before Step 2 stage 3 ships.
- **Q-new-32. Inter-city tax transfer mechanic.** Q-new-25 introduced "region can give to / levy from cities" as a new tax type. What is the player-facing UI? Discrete transfer command (one-shot)? Continuous policy (per-month tax rate adjustment)? Both? How does it interact with the city-internal budget categories — does it appear as an external income/expense line? Affects Step 3 stage 1 region sim model.
- **Q-new-33. Player-authored dormant control — country-view parameter list.** Q-new-26 enumerated region-view parameters but did not enumerate country-view parameters for dormant regions. Candidate set: national budget allocation per region, inter-region transport priorities, national infrastructure project routing, regional autonomy level, war/peace stance per neighbor. Needs an enumeration before Step 3 stage 5 ships.
- **Q-new-34. Weather event vs shaping event boundary.** §2.16 weather events (frosts, storms, droughts, heat waves) and §2.17 shaping events (earthquakes, tsunamis, landslides, floods) overlap conceptually — both are climate-/geography-driven adverse events. Are weather events the **continuous low-severity tail** of the shaping-event taxonomy (one system, two severity bands), or a **separate sub-system** that climate owns? Affects the Step 1 climate v1 / shaping-events stub split and whether they share a preparedness model.
- **Q-new-35. Agricultural zone — semantic split from industrial-light.** §2.16 places agriculture under industrial-light zoning, but agriculture is climate-sensitive, seasonal, and weather-vulnerable in ways industrial-light is not. Does agriculture inherit all industrial-light rules (pollution, demand, maintenance) and add climate sensitivity on top? Or does it split into its own family with selective inheritance? Load-bearing for the Step 1 climate v1 stage.
- **Q-new-36. Construction-plan model interface.** §2.18 says each new evolution-placeable element type ships its own construction-plan model analogous to `PathTerraformPlan`. What is the **shared interface** all such models implement — `ITryPlace(forbiddenMask, params) → PlanResult { Success | Replanned | NeedsExpropriation }`? How many model types are needed for Step 2 (regional road, district seed, service offset)? How do they compose with the existing road preparation family without bloating it?
- **Q-new-37. Process-engineering gap implementation order inside Step −1.** §2.20 named four load-bearing gaps to land in Step −1 (cross-review subagent, per-phase risk checklist, per-phase budget, S/M/L size labels) and three deferred gaps (phase metrics dashboard, automatic retro at closeout, cross-project dependency index). Inside Step −1, what is the **implementation order** of the four load-bearing gaps? Lifecycle skill rewrite first (because all gap closures touch the lifecycle skills), or in parallel? Which gap landing first **unblocks** the others?
- **Q-new-38. Spec-reviewer subagent — scope and verdict shape.** §2.20 gap #2 calls for a `spec-reviewer` subagent between `spec-kickoff` and `spec-implementer`. What does the reviewer **read** (just the project spec, or the spec + the touched code paths + the cited BACKLOG rows)? What does it **emit** (a structured verdict — approve / request-changes / block — or a free-form review)? Is its verdict **gating** for `spec-implementer` (cannot start without an approve), or **advisory** (implementer reads it as input)? Needs an answer before the subagent is filed.

---

## 11. Mutation log

Append-only. One line per change, with date and short rationale.

- **2026-04-10** — Initial seed of this brainstorm document. Captures the vision, the three-step skeleton, the existing-issue-to-step mapping, the provisional glossary, the orchestrator/skill proposal, and the open questions. No BACKLOG rows filed yet. No umbrella tracker yet.
- **2026-04-11** — Human answered Open Questions 1–7 and 9–14. Major consequences: (a) dormant scales evolve via a pure evolution algorithm on zoom, not via N-tick aggregate publishes (new §2.7); (b) NPC leaders become evolution parameter modulators (§2.7, glossary "scale sovereign"); (c) scales unlock progressively starting from CITY (new §2.8); (d) a save is the full multi-scale tree, not a single city (new §2.9); (e) save format migrates to Postgres `jsonb` column; (f) country scale gains a concrete head-of-state loop (Step 3 stage 2); (g) dashboard is drill-down by hierarchy; (h) the tracker is replaced by a delegated hierarchy of orchestrator documents with no automation (§6 rewritten); (i) lifecycle skills need to learn to distinguish project specs from orchestrator docs; (j) backlog triage pass scheduled as a dedicated task. Step 2 stages 3–4 rewritten to drop the aggregate-digest/delta-log model in favor of snapshot + evolution algorithm. §10 split into Resolved / Open / New-planted; 15 new open questions planted (Q-new-1..15) including parity budget, evolution-invariant vs mutable split, parallel agent safety, and out-of-the-box player-authored evolution parameters.
- **2026-04-11 (second pass)** — Human answered all remaining Open Questions (§10.B carried-over Q8/Q10/Q15 and the full Q-new-1..Q-new-15 set) in a dedicated answer pass; the answers have been folded inline into this brainstorm. Major consequences: **(a) Time-dilation retired.** §2.4 rewritten — every scale runs on the same real-time clock. No dilation, no per-scale natural tick periods. **(b) Step 0 inserted.** New Step 0 — Parent-scale conceptual stubs — lands before the former Step 1 (which is now "Step 1 — City MVP close" under Step 0). Plan is now four steps. §2.10 added. **(c) Scale switch, not scale zoom.** §2.11 added. Transition is a top-bar button panel with save-on-click + lazy load, not a camera zoom. Glossary renamed `scale zoom` → `scale switch`. **(d) Shaping events / disasters are the non-determinism engine.** §2.12 added. Cross-scale from day one. Player preparedness with resource cost. New defense-structure building family. **(e) Climate in city MVP.** §2.13 added. Minimum climate lands in Step 1; full multi-climate geography in Step 3. **(f) Parent-scale entities own evolution writes.** §2.14 added. Dormant children are represented inside their parent as child-scale entities holding a pending evolution delta; writes are materialized at scale-switch load time. **(g) Save format is relational, not single jsonb.** Main `game_save` table + per-scale tables with JSON cell-data columns (§3 glossary, §4 Step 2 stage 2, §9 feature #8). **(h) Player-authored dormant control promoted from moonshot to core.** At region scale the player tunes dormant-city parameters; at country scale the player tunes dormant-region parameters. Added to §4 Step 3 stage 5 and §9 feature #21. **(i) NPCs fully deferred.** Scale sovereign / NPC leader glossary row marked deferred. The player is the only actor in MVP. §2.7 and Step 3 country sim model updated. **(j) Dashboard reframed** as shared screen with per-scale read/write permissions + cross-scale key stats (Step 3 stage 3, replaces earlier drill-down rollup framing). **(k) §9 new feature list** rewritten end-to-end: removed time-dilation clock, jsonb save tree, NPC modifier rows; added Step 0 stubs, cell-type split, climate v1, shaping-events (stub + framework), child-scale entity model, single shared clock, scale switch UX, shaping-events cross-scale, player-authored dormant control, auto mode, shared dashboard. **(l) §10 restructured.** All previously open questions are now resolved (§10.B + §10.C), and **13 new questions planted in §10.D** (Q-new-16..Q-new-28): Δt tracking without dilation, Step 0 exit checklist, climate v1 scope, shaping-events taxonomy, parent-writes-child conflict model, cell-type refactor timing, relational save schema shape, switch-hook function signatures, switch loading budget, cross-scale budget hierarchy, player-authored control granularity, lifecycle skill rewrite sequencing, agent-parallelization abstraction.
- **2026-04-11 (third pass)** — Human answered Q-new-16..Q-new-27 in a dedicated answer pass and folded a 12-principle process-engineering study into §2.20. Both source documents were folded inline and removed; this brainstorm is the only permanent record. Q-new-28 explicitly deferred to a separate brainstorm thread. Major consequences: **(a) Δt tracking is trivial.** §2.4 third-pass clarification: same calendar across scales means a `last_active_at` stamp suffices, no dilation math. **(b) Climate v1 is concrete.** §2.16 added — single biome per city, seasonal cycle, weather events (wind, frost, storm, drought, heat wave), agricultural zone family under industrial-light zoning, couples into desirability/happiness/pollution/demand. Step 1 stage 7 rewritten. **(c) Shaping events have a starter taxonomy.** §2.17 added — earthquakes (regional/local), tsunamis (regional/local), landslides, storm floods, river floods. Defense family: elevated construction, no-build zone declaration, early-warning station, concrete containment wall, artificial channel terraforming. Step 1 stage 8 rewritten with concrete first disaster (local storm flood) + two defense options. **(d) Expropriation reinvoked.** §2.18 added — parent-writes-child conflict model. Forbidden-cell mask + replan + expropriation escalation + per-element construction-plan models. **(e) Scale-switch loading budget committed.** §2.19 added — 1–2 s progressive, dashboard-first, chunked map load center-out. **(f) Process-engineering practices folded in.** §2.20 added — twelve principles triaged into "already practiced", "load-bearing gaps to close before Step 2", and "post-MVP polish". Four gaps land in the new Step −1 block: cross-review subagent, per-phase risk checklist, per-phase budget, S/M/L size labels. **(g) Step −1 block inserted.** New "Process & lifecycle preparation" block lands before Step 0 (per Q-new-27 resolution): lifecycle skill rewrite, process-engineering gap closures, backlog triage pass, brainstorm → orchestrator promotion. Plan is now **Step −1 → Step 0 → Step 1 → Step 2 → Step 3** (five blocks). **(h) Cell-type refactor execution moved into Step 0.** Per Q-new-21 resolution, Step 0 stage 5 changed from "decision artifact" to "execute the split". §4 Step 0 exit criteria updated. **(i) Glossary additions.** Weather event, Agricultural zone, Expropriation, Construction-plan model, Defense structure family (replaces vague row), Inter-city tax transfer, Progressive scale-switch load. Climate row refined with concrete content. **(j) §10 restructured again.** Q-new-16..Q-new-27 moved to new §10.D resolved. Q-new-28 moved to its own §10.E open block. Ten new questions planted in §10.F (Q-new-29..Q-new-38): Step 0 checklist lock-in moment, relational save schema open columns, scale-switch hook function signatures, inter-city tax transfer mechanic UI, country-view dormant control parameters, weather vs shaping event boundary, agricultural zone semantic split, construction-plan model interface, process-engineering gap order inside Step −1, spec-reviewer subagent scope.

---

## 12. Pointers for a fresh agent

If you are picking this up cold, this is the minimum reading to not waste time:

1. **This document, section 1 and section 2** — vision and design insights. Ten minutes.
2. **`BACKLOG.md`** — skim § Compute-lib program, § Agent ↔ Unity & MCP context lane, § IA evolution lane, § Economic depth lane, § Gameplay & simulation lane, § High Priority. Twenty minutes.
3. **`CLAUDE.md` + `ia/rules/invariants.md`** — hard rules. Do not skip.
4. **`ia/specs/simulation-system.md`** (via `mcp__territory-ia__spec_section`, not full read) — current single-scale tick loop.
5. **`ARCHITECTURE.md`** — runtime layers, dependency map, especially GridManager hub trade-off.
6. **`docs/information-architecture-overview.md`** — the IA stack so you know why we lean on MCP + specs + glossary.

**What the human wants next (as of 2026-04-11 third pass):**

- Continue the conversation. The third 2026-04-11 pass closed Q-new-16..Q-new-27, deferred Q-new-28 to a separate thread, and planted ten new questions (Q-new-29..Q-new-38). The next pass should push on the ones that gate downstream work soonest:
  - **Q-new-29** — Step 0 exit checklist lock-in moment (gates Step −1 closure).
  - **Q-new-37** — process-engineering gap implementation order inside Step −1 (gates Step −1 stage planning).
  - **Q-new-38** — `spec-reviewer` subagent scope and verdict shape (gates Step −1 stage 2).
  - **Q-new-30** — relational save schema open columns (gates the Step 2 stage 2 TECH spec).
  - **Q-new-31** — scale-switch hook concrete function signatures (gates Step 2 stage 3).
  - **Q-new-34** — weather event vs shaping event boundary (gates Step 1 stage 7 vs stage 8 split).
  - **Q-new-35** — agricultural zone semantic split from industrial-light (gates Step 1 stage 7).
  - **Q-new-36** — construction-plan model interface (gates Step 2 stage 4 + §2.18).
- Do not yet file BACKLOG rows for the new FEAT ideas in §9 — the **backlog triage pass** (Q-new-7 resolved) is now stage 3 of **Step −1**, not end-of-Step-1. Hold on row filing until Step −1 stage 3 opens.
- Do not yet create the global orchestrator `ia/projects/multi-scale-master-plan.md` — that is **stage 4 of Step −1** and runs only after the §4 skeleton stops churning.
- Do not create a `multi-scale-master-plan-navigator` skill — still no automated navigator.
- **Do** propose edits to the §4 skeleton, especially the new **Step −1** block (it is brand new and the least stress-tested).
- **Do** think about **Q-new-37 / Q-new-38** — the lifecycle skill rewrite (Q-new-27 resolved) and the process-engineering gap closures (§2.20) need a concrete sequencing inside Step −1.
- **Q-new-28** stays parked. Do not re-derive it inside this thread; the human owns it on a separate brainstorm.

**What the human does not want:**

- Time estimates for any step, stage, phase, or task.
- Premature decomposition of Step 3 while Step 1 is still open.
- Scope creep toward world / solar scales before the three-scale MVP ships.
- New speculative abstractions in code before the plan itself is stable.
- Resurrection of the old "N-tick aggregate publish" model anywhere. Dormant scales evolve only via the evolution algorithm.
- **Resurrection of time-dilation framing.** Every scale runs on the same real-time clock (§2.4 rewritten).
- **Resurrection of the single-jsonb save tree.** The save is relational (§3 glossary, §9 feature #8).
- **Resurrection of NPC leader modeling in MVP.** NPCs are fully deferred (§3 glossary "Scale sovereign"). The player is the only actor.
- **"Scale zoom" terminology.** It is a scale **switch** (§2.11, §3 glossary).

---

*End of brainstorm seed. Treat the whole document as provisional except for sections 0, 1, and the mutation log.*
