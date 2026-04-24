# Multi-Scale Simulation — Master Plan (MVP)

> **Status:** In Progress — Step 2 (Step 1 Final 2026-04-14; Step 2 decomposed 2026-04-16, tasks _pending_)
>
> **Scope:** Min load-bearing work to prove city ↔ region ↔ country game loop (dormant evolution + reconstruction). Rest → `multi-scale-post-mvp-expansion.md`.
>
> **Vision + design principles:** `ia/specs/game-overview.md`
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Sibling orchestrators in flight (shared `feature/multi-scale-plan` branch):**
>
> - `ia/projects/blip-master-plan.md` — audio subsystem. Stage 1.1 archived (TECH-98..101); Stages 1.2–1.4 pending. Blip Step 3.3 (World lane call sites) wires into `GridManager.cs` cell-select + road/building tools + save hooks — coordinate so blip Step 3 kickoff lands after multi-scale `GridManager` mutations settle.
> - `ia/projects/sprite-gen-master-plan.md` — Python sprite generator (`tools/sprite-gen/`) + `Assets/Sprites/Generated/` output. City-scale 1×1 building footprints only in v1; region / country scale sprite needs surface when this orchestrator's Step 4 opens — not yet scoped anywhere.
> - **Parallel-work rule:** do NOT run `/stage-file` or `/closeout` against two sibling orchestrators concurrently — glossary + MCP index regens must sequence on a single branch.
>
> **Read first if landing cold:**
>
> - `ia/specs/game-overview.md` — vision + principles
> - `ia/specs/simulation-system.md` — current single-scale tick loop (MCP `spec_section`)
> - `docs/multi-scale-post-mvp-expansion.md` — scope boundary (what's OUT of MVP)
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics
> - MCP: `backlog_issue {id}` per referenced id; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

### Stage index

- [Stage 1 — Parent-scale conceptual stubs / Parent-scale identity fields](stage-1-parent-scale-identity-fields.md) — _Final_
- [Stage 2 — Parent-scale conceptual stubs / Cell-type split](stage-2-cell-type-split.md) — _Final_
- [Stage 3 — Parent-scale conceptual stubs / Neighbor-city stub + interstate-border semantics](stage-3-neighbor-city-stub-interstate-border-semantics.md) — _Final (2026-04-14 — all tasks archived TECH-102→TECH-109)_
- [Stage 4 — City MVP close / Bug stabilization](stage-4-bug-stabilization.md) — _Done (2026-04-17 — all 4 tasks archived)_
- [Stage 5 — City MVP close / Tick performance + metrics foundation](stage-5-tick-performance-metrics-foundation.md) — _In Progress (tasks filed 2026-04-17 — TECH-290..TECH-293)_
- [Stage 6 — City MVP close / City readability dashboard](stage-6-city-readability-dashboard.md) — _In Progress (FEAT-51 filed)_
- [Stage 7 — City MVP close / Parent-stub consumption](stage-7-parent-stub-consumption.md) — _Draft (tasks _pending_ — not yet filed)_
