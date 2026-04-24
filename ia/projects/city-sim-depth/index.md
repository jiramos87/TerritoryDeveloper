# City-Sim Depth — Master Plan (Bucket 2 MVP)

> **Status:** In Progress — Step 1 / Stage 1.1
>
> **Scope:** Shared simulation-signal contract (12 signals) + district aggregation layer + migration of existing happiness/pollution scalar to `HappinessComposer` + 7 new simulation sub-surfaces (pollution split, crime, services, traffic, waste, construction evolution, density evolution + industrial sub-types) + signal overlays + HUD/district panel parity. Excludes Zone S / economy (Bucket 3), utilities (Bucket 4), CityStats UI overhaul (Bucket 8), per-vehicle pathing, animation pipeline (Bucket 5), region/country feedback consumers.
>
> **Exploration source:** `docs/city-sim-depth-exploration.md` (§Design Expansion — Architecture, Subsystem Impact, Implementation Points are ground truth).
>
> **Locked decisions (do not reopen in this plan):**
> - Approach E (hybrid signal contract + district aggregation) — selected; criteria matrix clear.
> - Signal inventory fixed at 12 for MVP: `PollutionAir`, `PollutionLand`, `PollutionWater`, `Crime`, `ServicePolice`, `ServiceFire`, `ServiceEducation`, `ServiceHealth`, `ServiceParks`, `TrafficLevel`, `WastePressure`, `LandValue`.
> - FEAT-43 toggle — explicit `bool useSignalDesirability`; NOT parallel A/B; old path disabled when new path on.
> - District auto-derivation from `UrbanCentroidService` centroid + ring bands (single-centroid MVP); player-drawn districts deferred.
> - `SignalField` data NOT persisted; `SignalWarmupPass` deterministic recompute on load.
> - `DistrictMap` + tuning weights ARE persisted; signal fields are NOT.
> - Per-frame signal updates deferred; MVP = daily or monthly tick only.
> - Rollup rules: `Crime` + `TrafficLevel` = P90; all other signals = mean.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Read first if landing cold:**
> - `docs/city-sim-depth-exploration.md` — full design + architecture + examples. Design Expansion block is ground truth.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality (≥2 tasks per phase).
> - `ia/rules/invariants.md` — `#3` (no `FindObjectOfType` in Update/per-frame loops), `#4` (no new singletons — Inspector + `FindObjectOfType` fallback), `#5` (no direct `gridArray`/`cellArray` outside `GridManager`), `#6` (don't add responsibilities to `GridManager`), `#1` (`HeightMap` sync — signal systems read water/terrain cells but never write `HeightMap`).
> - `ia/specs/simulation-system.md` §Tick execution order — tick phase insertion point (signal phase inserts between `UrbanCentroidService.RecalculateFromGrid` and `AutoRoadBuilder.ProcessTick`).
> - `ia/specs/managers-reference.md` §Demand (R/C/I), §World features — current happiness + pollution API.
> - `ia/specs/persistence-system.md` §Load pipeline — save/load restore order.
> - `ia/specs/simulation-signals.md` — authored in Stage 1.1 (T1.1.4); signal inventory, rollup rules, diffusion physics contract.
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

---

### Stage index

- [Stage 1 — Signal Layer Foundation / Signal Contract Primitives](stage-1-signal-contract-primitives.md) — _In Progress (4 tasks filed 2026-04-17 — TECH-305..TECH-308)_
- [Stage 2 — Signal Layer Foundation / DiffusionKernel + SignalTickScheduler](stage-2-diffusionkernel-signaltickscheduler.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 3 — Signal Layer Foundation / District Layer](stage-3-district-layer.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 4 — Happiness Migration + Warmup / HappinessComposer Migration](stage-4-happinesscomposer-migration.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 5 — Happiness Migration + Warmup / DesirabilityComposer + FEAT-43 Toggle](stage-5-desirabilitycomposer-feat-43-toggle.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 6 — Happiness Migration + Warmup / SignalWarmupPass + Save Schema](stage-6-signalwarmuppass-save-schema.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 7 — New Simulation Signals / Pollution Split + LandValue](stage-7-pollution-split-landvalue.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 8 — New Simulation Signals / CrimeSystem](stage-8-crimesystem.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 9 — New Simulation Signals / Services + Traffic + Waste](stage-9-services-traffic-waste.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 10 — Construction + Density + Industrial / ConstructionStageController](stage-10-constructionstagecontroller.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 11 — Construction + Density + Industrial / DensityEvolution + IndustrialSubtypeResolver](stage-11-densityevolution-industrialsubtyperesolver.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 12 — Overlays + HUD Parity / SignalOverlayRenderer](stage-12-signaloverlayrenderer.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 13 — Overlays + HUD Parity / District Info Panel](stage-13-district-info-panel.md) — _Draft (tasks _pending_ — not yet filed)_

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) runs.
- Run `claude-personal "/stage-file ia/projects/city-sim-depth-master-plan.md Stage 1.1"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to `docs/city-sim-depth-exploration.md` Design Expansion block.
- Author `ia/specs/simulation-signals.md` in Stage 1.1 Task T1.1.4 per invariant #12 — signal contract is a permanent domain.
- `SignalTickScheduler` inserts between `UrbanCentroidService.RecalculateFromGrid` (~line 74) and `AutoRoadBuilder.ProcessTick` (~line 77) in `SimulationManager.ProcessSimulationTick`.
- Save manager is `GameSaveManager.cs` (not `SaveManager.cs`) — load hook target for `SignalWarmupPass.Run()`.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Terminal step Final status flips; file stays.
- Promote post-MVP items into MVP stages — deferred: per-vehicle pathing, named sims, multi-centroid districts, region/country feedback consumers, crime → protest animation (Bucket 5 event placeholder only), player-drawn districts, per-frame signal updates, Zone S / economy (Bucket 3), utilities (Bucket 4).
- Merge partial stage state — every stage lands on a green bar.
- Insert BACKLOG rows directly — only `stage-file` materializes them.
- Run FEAT-43 old desirability path and new `DesirabilityComposer` path in parallel — explicit `bool useSignalDesirability` toggle only (locked decision).
- Write `HeightMap` or `Cell.height` from any signal system — invariant #1; `PollutionWater` reads water cells via `WaterManager` but does not write terrain.
- Call `FindObjectOfType` in `Update` or per-frame loops — invariant #3; all producer/consumer lists resolved once in `Awake`.
