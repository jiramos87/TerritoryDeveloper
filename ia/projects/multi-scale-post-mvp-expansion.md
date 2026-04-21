# Multi-Scale Simulation — Post-MVP Expansion

> **Status:** Deferred-scope compendium. Everything in this document was considered for the three-scale MVP and explicitly **cut** from it. Nothing here is load-bearing for the first playable multi-scale loop; nothing here should be promoted into a step or stage inside `multi-scale-master-plan.md` without a prior explicit MVP-exit decision.
>
> **Purpose:** Preserve the full design surface considered during the multi-scale brainstorm so it is not lost, and keep each deferred feature grouped by domain with the rationale for its deferral.
>
> **Companion documents:**
> - `ia/projects/multi-scale-master-plan.md` — the MVP master plan (what is actually shipping).
> - Brainstorm seed history lives in git only — see the `chore: brainstorm*` commits on `feature/multi-scale-plan`.

---

## 0. How this document relates to the MVP

- The MVP master plan proves the three-scale game loop — city ↔ region ↔ country switching, dormant evolution, reconstruction, minimum head-of-state loop — and nothing more. It is intentionally thin on polish, content, variance, and process tooling.
- This document holds everything that the brainstorm considered load-bearing for a *richer* experience but that is **not** required for the three-scale loop itself to function. When the MVP ships and stabilizes, items from this document get promoted into a follow-up expansion plan (a new master plan doc, or extensions of the existing one).
- Do **not** file BACKLOG rows for items in this document during the MVP backlog triage pass. The triage pass files only MVP rows under `§ Multi-scale simulation lane`. Post-MVP rows are filed later, during a dedicated post-MVP triage pass.

---

## 1. Deferred — Climate system

**Source:** brainstorm §2.13, §2.16, §3 glossary (`Climate`, `Weather event`, `Agricultural zone`), Q-new-18, Q-new-34, Q-new-35.

### 1.1. Climate v1 (city)

- Single biome per city, seasonal cycle driving per-month modifiers (temperature band, precipitation band, sun/day length).
- Weather event sub-system: wind speed + direction (feeds pollution dispersion), frosts (agricultural damage + happiness penalty), storms (transient desirability/happiness hit + property damage hook), droughts (agriculture failure, water stress, demand shifts), heat waves (happiness, health demand, industrial output dip).
- Coupling into existing systems: desirability, happiness, pollution dispersion, demand, and a new **agricultural zone** family under industrial-light zoning.
- Step 0 "world-climate pointer" reintroduction — minimum conceptual stub inside the city scale so the coarser scales have something to generalize from.

### 1.2. Multi-climate region and country

- Region: multiple climates possible inside a single region (coastal vs inland, gradients, rivers threading through cities, lake shorelines crossing cities).
- Country: polar regions, polar gradients, equatorial bands.
- Cross-scale continuity so aggregation from single-biome city cells up to multi-biome region and country cells is well-defined.

### 1.3. Agricultural zone family

- New building family treated as a sub-type of industrial-light zoning. First climate-sensitive city system; canonical testbed for the climate v1 hooks (frost damage, drought failure, seasonal yield).
- **Open question (Q-new-35):** does agriculture inherit all industrial-light rules (pollution, demand, maintenance) and add climate sensitivity on top, or split into its own family with selective inheritance? Load-bearing for any climate v1 landing.

### Why deferred

Climate is **not** required to prove that three scales switch, evolve, and reconstruct. A three-scale MVP without climate still exercises the full scale-neutral spine; climate is a content/variance layer on top. Land it after the MVP ships and stabilizes, together with shaping events (§2) so their boundary can be decided at the same time.

---

## 2. Deferred — Shaping events and disasters

**Source:** brainstorm §2.12, §2.17, §3 glossary (`Shaping event`, `Defense structure family`, `Expropriation`, `Construction-plan model`), Q-new-19, Q-new-20, Q-new-36.

### 2.1. Shaping-events framework (cross-scale)

- Starter taxonomy: regional earthquake, local earthquake, regional tsunami, local tsunami, local landslide, local storm flood, local river flood. Pandemic, boom, crash, drought carry forward from the wider taxonomy.
- Frequency / severity curves per scale and per geography.
- Non-determinism channel in the evolution algorithm: `evolve(snapshot, Δt, params, events) → snapshot'`. MVP evolution is deterministic (no `events` channel); shaping events reintroduce that channel and the variance it brings.
- Cross-scale from day one: a city fire is a city-scale event; a regional drought affects many cities; a country pandemic affects many regions.
- Event bubble-up at switch time re-labels child-scale entities in parent views (e.g., switching out of a city whose last year contained a large fire re-labels that city's entity in the region view).

### 2.2. Defense structure family

- Placement rules and new building families the player can invest in to reduce shaping-event impact:
  - **Elevated construction** — placement rule the player opts into per-site.
  - **No-build zone declaration** — player-authored invariant that forbids construction on a tile or district. Evolution algorithms must respect it.
  - **Early-warning station** — new building type that reduces event impact via advance notice.
  - **Concrete containment wall** — linear building family that blocks landslides and storm surges along its footprint.
  - **Artificial channel terraforming** — terraforming operation that reshapes the heightmap to create a drainage channel. Uses the existing road-preparation-family terraform infrastructure.
- Preparedness has a resource cost. Dormant-scale outcomes depend on how well the player prepared the scale before switching away.

### 2.3. Parent-writes-child conflict model

- Player invariants act as a **forbidden-cell mask** the evolution algorithm reads at switch-out time.
- Evolution **replans** around the mask: a new road path routes around landmarks; a new district seeds in the nearest legal cluster; a new service building offsets to the nearest legal lot.
- When replanning fails, evolution escalates to an **expropriation** mechanic: the parent scale flags the target cell for expropriation, the player sees a notification at switch-in, and must approve (with happiness/money cost) or counter-plan (accepting a growth penalty).
- Each new evolution-placeable element type ships its own **construction-plan model** analogous to `PathTerraformPlan` for roads, describing how that element composes with the forbidden-cell mask, what it returns on success, on replan, and on expropriation.
- **Open question (Q-new-36):** the shared interface all such models implement — `ITryPlace(forbiddenMask, params) → PlanResult { Success | Replanned | NeedsExpropriation }`? How many model types are needed (regional road, district seed, service offset, inter-city rail, national infrastructure, defense structure, agricultural zone)? How do they compose with the existing road preparation family without bloating it?

### 2.4. Expropriation mechanic

- Reinvoked from a previously parked concept.
- Player notification at switch-in + happiness / money cost + counter-plan option + growth penalty path.
- The conflict model must exist the moment the first parent-owned evolution algorithm runs; the MVP's evolution algorithm is deterministic and does not place new evolution-mutable elements that could conflict with player invariants, so the expropriation surface is not yet needed.

### 2.5. Weather vs shaping event boundary

- **Open question (Q-new-34):** are weather events the **continuous low-severity tail** of the shaping-event taxonomy (one system, two severity bands), or a **separate sub-system** that climate owns? Affects whether they share a preparedness model.
- Decided alongside climate v1 (§1) and the shaping-events framework landing.

### Why deferred

The entire shaping-events pipeline exists to inject variance into evolution. MVP evolution is intentionally deterministic — the three-scale loop is proved *without* variance first, and then variance is reintroduced on top. Everything in this section (framework, defense structures, conflict model, expropriation, construction-plan models) hangs together and is cleaner to land as one coherent post-MVP block than as fragments spread across MVP stages.

---

## 3. Deferred — City MVP depth (Step 1 polish)

**Source:** brainstorm §4 Step 1 stages 3–5, §8 Step 1 table.

### 3.1. Stability polish (cosmetic / non-corrupting)

- `BUG-48` — minimap stale refresh.
- `BUG-52` — AUTO zoning grass-gap regression.
- `BUG-20` — utility building load visual.
- `BUG-28` — slope / interstate sorting order.
- `BUG-31` — interstate border prefabs.

### 3.2. Economic depth and tension

- `FEAT-52` — city services coverage.
- `FEAT-08` — zone density evolution + spatial pollution.
- `FEAT-53` — districts.

### 3.3. City shape correctness

- `FEAT-43` — urban growth ring gradient tuning.
- `FEAT-47` (city-internal portions) — multipolar centroids inside a single city.

### 3.4. Player agency / QoL

- `FEAT-35` — area demolition drag.
- `FEAT-03` — forest hold-to-place.

### 3.5. Performance envelope measurement

- `TECH-16` — sim tick perf v2 + harness labels.
- `TECH-26` — per-frame `FindObjectOfType` scanner.

### Why deferred

The MVP city does not need to feel like a finished city-builder loop; it needs to be **stable enough and readable enough** to serve as an aggregation source and reconstruction target. Economic depth, city shape polish, QoL, and performance envelope measurement are what turn the city into a satisfying single-scale game; none of them are required for the three-scale loop. Land them after the three-scale MVP ships, scheduled against whatever scale is active at the time.

---

## 4. Deferred — Region + Country content depth (Step 3 polish)

**Source:** brainstorm §4 Step 3 stages 1–8, §8 Step 3 table, §9 features 18–24a.

### 4.1. Region content

- `FEAT-15` — port system (trade edges).
- `FEAT-16` — train system (trade edges).
- `FEAT-14` — vehicle traffic (aggregated flow at region scale).
- `FEAT-39` — sea / shore band (coastal region semantics).
- `FEAT-40` — water sources and drainage.
- `FEAT-48` — water body volume budget.
- `FEAT-10` — regional monthly bonus (evolves into region sim stub).
- Regional projects (big industrial plants, regional parks, regional infrastructure).
- Multi-climate geography: coastal and lake shorelines crossing multiple cities, rivers threading through cities, climate gradients across the region.
- Multiple natural resource types per region (MVP ships only one resource type).

### 4.2. Country content

- International relations: stance against neighboring countries, war declaration, border expansion.
- Natural resources as a national priority (allocation, strategic reserves, export policy).
- Polar regional climate gradients.
- National priority reordering surface beyond the minimum budget + one infra project.

### 4.3. Inter-city economics

- Full inter-city trade network solver — distributes production/consumption across region graph edges.
- `TECH-83` — sim parameter tuning and experiments for balancing inter-city trade.

### 4.4. Inter-city tax transfer mechanic

- Region-scale fiscal mechanic that lets the region shift money between child cities (give to one, levy more from another) without touching city-internal budget categories. New tax type.
- **Open question (Q-new-32):** player-facing UI — discrete transfer command (one-shot), continuous policy (per-month tax rate adjustment), both? How does it interact with city-internal budget categories — does it appear as an external income/expense line?

### 4.5. Auto mode at region + country

- Hands-off default per scale so the player can set policy, switch out, and still have dormant regions / countries advance sensibly.
- Couples into the extended player-authored dormant control parameters (§5).

### 4.6. Per-scale playability polish pass

- Each scale ships a minimum coherent player loop beyond the three-scale proof:
  - Region: influence migration, trade, inter-city infrastructure; place or upgrade regional trunk roads/rails/ports; found new cities with a richer creation UX.
  - Country: the head-of-state loop with the full priority / relations / resource / war surface.

### Why deferred

The MVP region and country ship the **minimum** content to prove that both scales tick live, evolve when dormant, and reconstruct correctly. Everything in this section is content depth — more content types, richer mechanics, more tunable surfaces, more loops. The three-scale loop already works without any of it.

---

## 5. Deferred — Extended player-authored dormant control

**Source:** brainstorm §2.15, §4 Step 3 stage 5, Q-new-26, Q-new-33.

### 5.1. Region-view parameter surface (extended)

- Resource allocation across child cities.
- Regional energy-grid connections between child cities.
- Transport-line connections (roads, rails, inter-city transit).
- Distance-effect construction placement near specific cities.
- Designate the regional capital (with downstream gameplay consequences).

### 5.2. Country-view parameter surface

- **Open question (Q-new-33):** candidate set — national budget allocation per region, inter-region transport priorities, national infrastructure project routing, regional autonomy level, war/peace stance per neighbor. Needs an enumeration before the extended country-view dormant control lands.

### Why deferred

MVP dormant control is **budget allocation per child**, one parameter per parent scale, and nothing else. That is enough to prove the "parameter surface IS the gameplay" claim at the smallest possible scope. Extended parameter surfaces are content, not architecture, and land as a post-MVP pass.

---

## 6. Deferred — Cross-scale dashboard + UX polish

**Source:** brainstorm §4 Step 3 stage 3, §2.19, §9 features 14, 16c, 23.

### 6.1. Shared cross-scale dashboard

- Single shared screen with per-scale read/write permissions.
- Surfaces key stats from other scales alongside the active scale's stats.
- Cross-scale rollups driven by the child-scale entity snapshots + pending deltas.
- Replaces the earlier "drill-down rollup" framing.

### 6.2. Progressive scale-switch loader

- Dashboard renders first (<300 ms) from the snapshot + pending delta, before the map is fully ready.
- Map loads in chunks **center-out** (or "player-last-position-first"), one chunk at a time, while the dashboard is already usable.
- Input is gated on at least one playable chunk being ready, not on the full map.
- Budget shape: "dashboard in <300 ms, first interactive chunk in <2 s, remaining chunks fill in background."

### 6.3. Richer event feed on switch-in

- Structured event bubble-up from child-scale entities surfaced in a dedicated feed, not just a text summary.
- Cross-scale event routing rules (which events surface at which scale, with what priority).

### 6.4. Scale-switch UX — post-MVP polish

MVP ships semantic zoom + procedural fog mask + minimal `ScaleToolProvider` (see master plan Step 3). Post-MVP:

- **Truly continuous rendering across scales** — remove fog mask; LOD streaming + progressive detail across zoom bands. Fog = MVP escape hatch.
- **Animated fly-to** on city-node click from region view (cinematic alternative to scroll-zoom).
- **Minimap integration** with scale transitions (click minimap → transition to that scale).
- **Asymmetric transition styles** — different zoom-out vs zoom-in animations if playtesting warrants.
- **Full per-scale tool depth** — rich region + country toolsets beyond MVP minimum.
- **Per-scale keybinding policy** — decision between per-scale vs semantic-consistent key mapping as tool depth grows.
- **Shift+scroll fast cross-scale zoom** — power-user shortcut.
- **Pinch-to-zoom** touch support.
- **Per-scale tool state persisted across saves** — MVP persists in-session only; save/load persistence post-MVP.

### Why deferred

MVP ships a per-scale dashboard (active scale only) and a plain loading screen. Both are functionally sufficient to demonstrate scale switching; neither is polished enough to ship as a finished experience. Polish ladders on after the three-scale loop is proven.

---

## 7. Deferred — Process-engineering gap closures

**Source:** brainstorm §2.20, Q-new-37, Q-new-38.

### 7.1. Load-bearing gaps (deferred from MVP Step −1)

1. **Cross-review gate between author and implementer.** A third independent reader agent reviews a project spec before the implementer touches it. Candidate owner: new subagent `spec-reviewer` launched by `/author` on completion.
   - **Open question (Q-new-38):** reviewer reads project spec alone, or project spec + touched code paths + cited BACKLOG rows? Emits structured verdict (approve / request-changes / block) or free-form review? Verdict is gating for `spec-implementer` or advisory?
2. **Per-phase risk / preflight checklist.** Declarative impact / reversibility / touched-dependencies checklist before each phase of the Implementation Plan.
3. **Explicit per-phase budget.** Soft limit on steps / tokens / time per phase before prompting for human confirmation.
4. **Spec size classification (S/M/L).** Frontmatter label that drives different workflows — a trivial `BUG` does not need the same pipeline as a multi-stage `TECH`.

### 7.2. Post-MVP polish gaps (deferred further)

- **Automatic retrospective at closeout.** Produce a "what failed / what worked / reusable pattern" digest in addition to lesson migration.
- **Cross-project dependency index.** Which open BACKLOG items touch shared files. Gates any agent parallelization.
- **Phase metrics dashboard.** Time / retries / gate-fail counts per phase per project.
- **Glossary gate in PRs.** Convention exists informally; formalize as a small `TECH` row during a future triage pass.
- **Dry-run closeout.** Only matters when a spec with many migrated lessons closes.
- **Consolidated decision journal.** Unified view over `project_spec_journal_*` MCP data.

### 7.3. Implementation order

- **Open question (Q-new-37):** inside the eventual post-MVP Step −1 analogue, what is the implementation order of the four load-bearing gaps? Lifecycle skill rewrite first (all gaps touch the skills), in parallel, or staged?

### Why deferred

Current lifecycle skills (with the MVP Step −1 orchestrator distinction added) are sufficient to ship the three-scale MVP. The process-engineering gap closures improve agent throughput and reduce drift, but none of them is load-bearing for the game code or the save schema. Land them when the MVP ships and there is slack to invest in tooling.

---

## 8. Deferred — Agent parallelization abstraction

**Source:** brainstorm §10.E Q-new-28.

- Register + context-compression layer for sub-orchestrator / delegator agents.
- Which tasks to parallelize is a decision for after all tasks are enumerated.
- **Deferred to a separate brainstorm thread** per the brainstorm's own decision. Do not re-derive inside this document. Reopen only when that separate thread produces a proposal.

---

## 9. Deferred — World, solar, and long tail

**Source:** brainstorm §4 Parked, §9 item 25, §12 do-not-want list.

### 9.1. World scale

- Global climate, commodity prices, sea level drift.
- Macro commodity trade.
- The MVP's "world-climate pointer" is **dropped** alongside climate itself; the forward stub only reappears when climate v1 lands.

### 9.2. Solar scale

- Epochs, catastrophes, long-term resource depletion.

### 9.3. Era / tech tree

- Shared across scales.

### 9.4. Other parked

- Epidemic propagation.
- NPC leaders / scale sovereigns / agents. Fully deferred. Player is the only actor in MVP.
- Fog of war / exploration.
- Culture drift.
- Refugee flows.
- Photo mode + time-lapse replay.
- Tutorial / onboarding.
- Achievements.

### Why deferred

None of these are part of the three-scale MVP target. They are explicitly listed as "parked" in the brainstorm and stay parked through MVP. They are listed here only so that the full design surface is preserved in one place.

---

## 10. Deferred — Miscellaneous scale-neutral spine polish

**Source:** brainstorm §4 Step 2 stages 1–6, §9 features 10, 14, 16c.

- **Progressive scale-switch loader (§6.2 above).** Not repeated here.
- **Research-gated spikes** — `TECH-32` (urban growth ring what-if tooling), `TECH-35` (property-based invariant fuzzing). Nice to have for confidence; not load-bearing for the three-scale loop.
- **`TECH-81` knowledge graph.** Cross-scale dependency queries. Only justified once scale dependency queries become common; MVP does not need them.
- **IA evolution lane** — `TECH-77`, `TECH-78`, `TECH-79`, `TECH-80`, `TECH-83`. Discretionary, schedule where they unblock post-MVP plan work.
- **Scale unlock mechanism (full).** MVP defers all scale unlock work — player starts with all three scales available. Post-MVP: persisted per-save unlock state, population threshold trigger, "% map usage" half, unlock UX entry point.
- **Scale unlock trigger — "% map usage" half.** Add when it produces gameplay-meaningful unlock pacing.
- **Mutation / training parity harness.** MVP parity harness is playtest-only. Mutation testing and training-run parity checks are richer measurement techniques for a future pass.
- **Shaping-events channel in the evolution algorithm signature.** MVP signature is `evolve(snapshot, Δt, params) → snapshot'`. The `events` channel reappears with §2.

---

## 11. Deferred open questions

Consolidated view of every brainstorm open question that is not answered inside the MVP master plan.

| Question | Status | Source |
|---|---|---|
| Q-new-28 | Deferred to a separate brainstorm thread (agent-parallelization abstraction). | brainstorm §10.E |
| Q-new-29 | **Decided in MVP master plan §8** — locked at start of Step 0 stage 1. | master plan §8 |
| Q-new-30 | **Decided in MVP master plan §8** for MVP (player invariants in `cell_data jsonb`). Shaping-event log location still open, deferred with §2. | master plan §8 + §2.3 above |
| Q-new-31 | **Baseline decided in MVP master plan §8.** Concrete C# shapes lock when Step 2 stage 3 opens. | master plan §8 |
| Q-new-32 | Deferred. Inter-city tax transfer mechanic UI. | §4.4 above |
| Q-new-33 | Deferred. Country-view dormant control parameter list. | §5.2 above |
| Q-new-34 | Deferred. Weather event vs shaping event boundary. | §2.5 above |
| Q-new-35 | Deferred. Agricultural zone semantic split from industrial-light. | §1.3 above |
| Q-new-36 | Deferred. Construction-plan model interface. | §2.3 above |
| Q-new-37 | Deferred. Process-engineering gap implementation order. | §7.3 above |
| Q-new-38 | Deferred. `spec-reviewer` subagent scope and verdict shape. | §7.1 above |

---

## 12. Post-MVP promotion rule

- Nothing in this document enters the MVP master plan (`multi-scale-master-plan.md`) without an explicit MVP-exit decision and a new mutation-log entry in the MVP master plan recording the scope change.
- When the MVP ships and stabilizes, a dedicated session walks this document and produces a **post-MVP expansion plan** — either a new master plan doc (`multi-scale-post-mvp-expansion-plan.md`) or an extension to the existing MVP master plan with new steps appended.
- The post-MVP expansion plan will need its own backlog triage pass (analogous to Step −1 stage 2 of the MVP master plan) before any new BACKLOG rows are filed. Do not file post-MVP rows ahead of that pass.

---

## 13. Mutation log

Append-only.

- **2026-04-11** — Initial cut. Extracted from the brainstorm seed (now retained only in git history on `feature/multi-scale-plan`, `chore: brainstorm*` commits) during the MVP master plan promotion. Captures climate (§1), shaping events and disasters (§2), city depth (§3), region + country content depth (§4), extended dormant control (§5), cross-scale dashboard and UX polish (§6), process-engineering gap closures (§7), agent parallelization (§8), world/solar/long-tail (§9), scale-neutral spine polish (§10), deferred open questions (§11). All items are **explicitly deferred** — no BACKLOG rows, no orchestrator documents, no step/stage placement inside the MVP master plan.
- **2026-04-12** — Scale unlock mechanism promoted from MVP to post-MVP (§10). MVP ships all three scales available from start; unlock mechanic is content polish, not architecture. Dashboards already covered in §6.
