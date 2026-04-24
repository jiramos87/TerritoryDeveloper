# CityStats Overhaul — Master Plan (MVP)

> **Status:** In Progress — Step 1 / Stage 3 (next: file T3.1–T3.4)
>
> **Scope:** Replace the `CityStats` god-class with a typed read-model facade (`CityStatsFacade`) backed by a columnar ring-buffer store (`ColumnarStatsStore`), migrate all consumers to the facade, add region/country scale rollup facades, and surface city metrics in a new `web/app/stats` route. Overlays, per-cell drill-down, history persistence in save files, and region/country Postgres tables are out of scope (see Deferred section of `docs/citystats-overhaul-exploration.md`).
>
> **Exploration source:** `docs/citystats-overhaul-exploration.md` (§Design Expansion — Chosen Approach, Architecture, Subsystem Impact, Implementation Points are ground truth).
>
> **Locked decisions (do not reopen in this plan):**
> - Approach E (hybrid facade + columnar store) selected; Approach A (incremental) and D (web-first) ruled out.
> - Legacy `CityStats` MonoBehaviour becomes shim implementing `ICityStats`; field signature preserved verbatim during migration.
> - `CityMetricsInsertPayload` Postgres row schema unchanged; `MetricsRecorder` only swaps data source.
> - `GameSaveData` MVP: scalar-only snapshot via `facade.ExportSaveSlice()`; no history in save; no `schemaVersion` bump unless fields added.
> - Performance budget default: 256-tick ring buffer; revisit before Stage 1.1 capacity constants locked.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Read first if landing cold:**
> - `docs/citystats-overhaul-exploration.md` — full design + architecture + examples. Design Expansion block is ground truth.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality rule (≥2 tasks per phase).
> - `ia/rules/invariants.md` — **#3** (no `FindObjectOfType` in `Update` or per-frame loops), **#4** (no new singletons — `CityStatsFacade` is `MonoBehaviour` with Inspector wire, not singleton), **#6** (no bloat on `GridManager` — facade lives in its own `GameManagers` class).
> - `ia/specs/simulation-system.md §Tick execution order` (lines 11–26) — steps 1-5 in `ProcessSimulationTick` NOT reordered; `BeginTick`/`EndTick` bracket wraps outside steps.
> - `ia/specs/persistence-system.md §Save` — `schemaVersion` bump required if any fields added to `GameSaveData`.
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

---

### Stage index

- [Stage 1 — Facade + Store Infra (additive, no consumer migration) / Core types (IStatsReadModel, StatKey, ColumnarStatsStore)](stage-1-core-types-istatsreadmodel-statkey-columnarstatsstore.md) — _Final (TECH-303, TECH-304 archived 2026-04-21)_
- [Stage 2 — Facade + Store Infra (additive, no consumer migration) / CityStatsFacade MonoBehaviour + tick bracket](stage-2-citystatsfacade-monobehaviour-tick-bracket.md) — _Final (TECH-602, TECH-603 archived 2026-04-21)_
- [Stage 3 — Facade + Store Infra (additive, no consumer migration) / CityStats shim dual-write + MetricsRecorder swap + EditMode test](stage-3-citystats-shim-dual-write-metricsrecorder-swap-editmode-test.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 4 — Consumer Migration / CityStatsUIController per-tick subscription](stage-4-citystatsuicontroller-per-tick-subscription.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 5 — Consumer Migration / Producer managers publish via facade](stage-5-producer-managers-publish-via-facade.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 6 — Consumer Migration / StatisticsManager migration + deletion](stage-6-statisticsmanager-migration-deletion.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 7 — Multi-scale Rollup + Web Stats Surface / RegionStatsFacade + CountryStatsFacade rollup](stage-7-regionstatsfacade-countrystatsfacade-rollup.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 8 — Multi-scale Rollup + Web Stats Surface / web/app/stats route](stage-8-stats-route.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 9 — Multi-scale Rollup + Web Stats Surface / Glossary + spec updates](stage-9-glossary-spec-updates.md) — _Draft (tasks _pending_ — not yet filed)_

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) runs.
- Run `claude-personal "/stage-file ia/projects/citystats-overhaul-master-plan.md Stage 2"` when Stage 2 tasks are ready to file → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to `docs/citystats-overhaul-exploration.md`.
- Before Stage 3.1: read `multi-scale-master-plan.md` Step 3 save-leaving section to confirm exact **Scale switch** hook method name before editing.
- Before Stage 1.1 capacity constant lock: confirm 256-tick ring buffer acceptable at max map size (open question from exploration review notes).

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal step landing triggers a final `Status: Final`; the file stays.
- Silently promote Deferred items (overlay migration to `GetRasterView`, per-cell drill-down, history persistence in save, `region_metrics_history` / `country_metrics_history` Postgres tables, dark-mode palette) into MVP stages.
- Merge partial stage state — every stage must land on a green bar.
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Add responsibilities to `GridManager` (invariant #6) — `CityStatsFacade` and store live in `GameManagers/`, not in `GridManager`.
- Re-enable `UrbanizationProposal` (invariant #11 — permanently obsolete).
