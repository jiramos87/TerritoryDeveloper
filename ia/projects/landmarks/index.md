# Landmarks — Master Plan (Bucket 4-b MVP)

> **Last updated:** 2026-04-17
>
> **Status:** In Progress — Step 1 / Stage 1.1
>
> **Scope:** Landmarks v1 — two parallel progression tracks. **Tier-defining landmarks** (free gift on scale-tier transition — Bucket 1 coupling) + **intra-tier reward landmarks** (designer-tuned pop milestones → commissioned "super-building" via bond-backed multi-month build). Catalog-driven (`StreamingAssets/landmark-catalog.yaml`). Sidecar `landmarks.json` = authoritative state; main-save cell-tag map = denormalized index. Super-utility buildings register into sibling Bucket 4-a `UtilityContributorRegistry` via narrow catalog interface. **OUT of scope:** utilities sim (sibling `docs/utilities-exploration.md`), Zone S + per-service budgets (Bucket 3 — consumed only as `IBondConsumer`), city-sim signals (Bucket 2), CityStats overhaul (Bucket 8), multi-scale core (Bucket 1 — consumed as scale-transition event source), heritage / cultural landmarks, landmark-specific tourism effects, destructible landmarks, mid-build cancellation, multi-cell footprints.
>
> **Exploration source:** `docs/landmarks-exploration.md` (§Design Expansion — Chosen Approach, Architecture, Subsystem Impact, Implementation Points §A–§F, Examples, Review Notes).
>
> **Umbrella:** `ia/projects/full-game-mvp-master-plan.md` Bucket 4-b row. Sibling orchestrator `ia/projects/utilities-master-plan.md` (Bucket 4-a). Schema bump piggybacks on Bucket 3 v3 envelope (same rule as utilities — no mid-tier v2.x bump owned here).
>
> **Locked decisions (do not reopen in this plan):**
> - Approach D — hybrid two-track, both pop-driven. Rejected A (registry only), B (scale-unlock only), C (commission only).
> - Tier-defining track = free gift on scale-tier transition (no commission cost, instant placement). Coupling to Bucket 1 `ScaleTierController`.
> - Intra-tier track = commissioned — bond-backed multi-month build, drawn against Bucket 3 per-service budget. Pause-able; NO mid-build cancellation (v1).
> - Deficit commission allowed (bond underwrites — no floor check beyond bond ceiling per Bucket 3 kickoff contract).
> - Catalog = hand-authored YAML at `StreamingAssets/landmark-catalog.yaml`. Schema: `id`, `name`, `tier`, `popGate`, `sprite`, `commissionCost`, `buildMonths`, `utilityContributorRef?`, `contributorScalingFactor?`.
> - Count target: 2 tier-defining (city→region, region→country) + 4 intra-tier = 6 rows v1.
> - Persistence: sidecar `landmarks.json` = truth; main-save per-scale cell-tag map = denormalized index. Reconciliation on load — sidecar wins; dangling cell tags cleared.
> - Placement is tile-sprite only — NO `HeightMap` mutation (invariant #1 safe). 1-cell footprint v1.
> - Super-utility bridge = narrow catalog interface. Landmark `utilityContributorRef` nullable — sibling Bucket 4-a `UtilityContributorRegistry.Register(landmarkId, contributorRef, scalingFactor)` called on `LandmarkBuildCompleted` when non-null.
> - Costs = placeholder constants. Migration to cost-catalog bucket (future Bucket 11) flagged at every commission-cost touch site.
> - UI = progress panel + commission dialog minimum viable. No tooltip / glossary polish (Bucket 6 scope). Bucket 6 `UiTheme` must land first.
> - Hard deferrals: heritage / cultural / tourism effects, destructible / decay, mid-build cancel, multi-cell footprints, in-game info panel polish.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable via `/closeout`).
>
> **Read first if landing cold:**
> - `docs/landmarks-exploration.md` — full design + architecture mermaid + sidecar/cell-tag reconciliation example. §Design Expansion is ground truth.
> - `ia/projects/utilities-master-plan.md` — sibling orchestrator; `UtilityContributorRegistry.Register` contract consumed by Step 4 of this plan.
> - `ia/projects/full-game-mvp-master-plan.md` — umbrella Bucket 4-b row + Bucket 3 v3 schema envelope rule.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + cardinality rule (≥2 tasks per phase, ≤6 soft).
> - `ia/rules/invariants.md` — **#1** (no `HeightMap` mutation — placement is tile-sprite only), **#3** (no `FindObjectOfType` in hot loops — cache refs in `Awake`), **#4** (no new singletons — `LandmarkProgressionService` / `BigProjectService` / `LandmarkPlacementService` / `LandmarkCatalogStore` all MonoBehaviour + Inspector + `FindObjectOfType` fallback), **#5 + #6** (`LandmarkPlacementService` under `Assets/Scripts/Managers/GameManagers/*Service.cs` carve-out — no `GridManager` responsibility creep), **#12** (permanent domain → `ia/specs/landmarks-system.md` authored in Stage 4.2).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; `spec_section persistence-system load-pipeline` for sidecar restore ordering; `rule_content orchestrator-vs-spec` for permanence rule. Never full `BACKLOG.md` read.
> - **Umbrella parallel-work rule:** sequential filing only. No concurrent `/stage-file` run with sibling `ia/projects/utilities-master-plan.md` on same branch.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

### Stage index

- [Stage 1 — Catalog + data model + glossary/spec seed / Data contracts + enums](stage-1-data-contracts-enums.md) — _In Progress (TECH-335, TECH-336, TECH-337, TECH-338 filed)_
- [Stage 2 — Catalog + data model + glossary/spec seed / Catalog YAML + validator rule](stage-2-catalog-yaml-validator-rule.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 3 — Catalog + data model + glossary/spec seed / LandmarkCatalogStore + glossary + spec stub](stage-3-landmarkcatalogstore-glossary-spec-stub.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 4 — LandmarkProgressionService (unlock-only) / Service scaffold + unlock flags](stage-4-service-scaffold-unlock-flags.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 5 — LandmarkProgressionService (unlock-only) / Gate evaluation + tick loop](stage-5-gate-evaluation-tick-loop.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 6 — LandmarkProgressionService (unlock-only) / Tick ordering + bootstrap integration](stage-6-tick-ordering-bootstrap-integration.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 7 — BigProjectService + LandmarkPlacementService + sidecar save / LandmarkPlacementService + cell-tag write](stage-7-landmarkplacementservice-cell-tag-write.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 8 — BigProjectService + LandmarkPlacementService + sidecar save / BigProjectService commission pipeline](stage-8-bigprojectservice-commission-pipeline.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 9 — BigProjectService + LandmarkPlacementService + sidecar save / Sidecar save + reconciliation on load](stage-9-sidecar-save-reconciliation-on-load.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 10 — Super-utility bridge + UI surface + spec closeout / Super-utility contributor bridge](stage-10-super-utility-contributor-bridge.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 11 — Super-utility bridge + UI surface + spec closeout / UI surface (progress panel + commission dialog)](stage-11-ui-surface-progress-panel-commission-dialog.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 12 — Super-utility bridge + UI surface + spec closeout / landmarks-system.md §3–§8 prose + glossary specRef update](stage-12-landmarks-system-md-3-8-prose-glossary-specref-update.md) — _Draft (tasks _pending_ — not yet filed)_

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) runs.
- Run `claude-personal "/stage-file ia/projects/landmarks-master-plan.md Stage {N}.{M}"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to `docs/landmarks-exploration.md` §Design Expansion.
- Keep this orchestrator synced with `ia/projects/full-game-mvp-master-plan.md` Bucket 4-b row — per `/closeout` umbrella-sync rule.
- Respect sibling Bucket 4-a hard-dep — Stage 4.1 (super-utility bridge) files ONLY after utilities Stage 1.3 (`RegisterWithMultiplier`) closes.
- Respect Bucket 6 hard-dep — Stage 4.2 (UI) files ONLY after Bucket 6 `UiTheme` Tier B' exit lands.
- Coordinate schema bump with Bucket 3 — never introduce mid-tier `schemaVersion` bump; Bucket 3 owns v3.
- Flag every `commissionCost` placeholder touch with `// cost-catalog bucket 11` marker until migration lands.
- **Umbrella parallel-work rule** — never run `/stage-file` on this plan concurrent with sibling `ia/projects/utilities-master-plan.md` on same branch. Sequential filing only.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only terminal step landing triggers a final `Status: Final`; file stays.
- Silently promote post-MVP items (heritage / cultural landmarks, tourism effects, destructible landmarks, mid-build cancel + partial refund, multi-cell footprints, info-panel polish) into MVP stages — flag to a future `docs/landmarks-post-mvp-extensions.md` stub.
- Mutate `HeightMap` from placement path (invariant #1) — landmark placement is tile-sprite only. Any proposed height change requires a separate master-plan decision.
- Add responsibilities to `GridManager` (invariant #6). Cell-tag write belongs on `LandmarkPlacementService` under `GameManagers/*Service.cs` carve-out (invariant #5).
- Add singletons (invariant #4). All four services (`LandmarkCatalogStore`, `LandmarkProgressionService`, `BigProjectService`, `LandmarkPlacementService`) = MonoBehaviour + Inspector + `FindObjectOfType` fallback.
- Use `FindObjectOfType` in `Update` / per-frame loops (invariant #3). Cache in `Awake`.
- Merge partial stage state — every stage must land on a green bar (`npm run validate:all` + `npm run unity:compile-check`).
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Resolve BUG-20 in this plan — orthogonal to landmark placement; track separately.

---
