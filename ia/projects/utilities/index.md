# Utilities — Master Plan (Bucket 4-a MVP)

> **Last updated:** 2026-04-17
>
> **Status:** In Progress — Step 1 / Stage 1.1 (4 tasks filed: TECH-331..TECH-334, all Draft)
>
> **Scope:** Utilities v1 — water / power / sewage as country-pool-first resources with local contributor buildings feeding per-scale pools (city / region / country). EMA soft warning → cliff-edge deficit (freeze + happiness decay + desirability decay). Infrastructure category with 2–3 capacity-based upgrade tiers. Natural wealth seeds water pool via adjacency; forests / mountains ambient-only; sea → port/commerce. Landmarks contributor-registry contract owned here; landmark catalog plugs in via `RegisterWithMultiplier`. **OUT of scope:** landmarks proper (sibling `docs/landmarks-exploration.md` + future `landmarks-master-plan.md`), Zone S economy (Bucket 3), signal integration (Bucket 2), CityStats overhaul (Bucket 8), multi-scale core (Bucket 1), energy storage, rolling blackouts, grid-loss transfer, private operators, climate modifiers.
>
> **Exploration source:** `docs/utilities-exploration.md` (§Design Expansion — Chosen Approach, Architecture, Subsystem Impact, Implementation Points, Examples, Review Notes).
>
> **Umbrella:** `ia/projects/full-game-mvp-master-plan.md` Bucket 4 row. Schema bump coordinates with Bucket 3 (`zone-s-economy`) — Bucket 3 owns the v3 schema jump; this plan stages additions against the v3 envelope (no mid-tier v2.x bump).
>
> **Locked decisions (do not reopen in this plan):**
> - Approach B — country-pool first, local contributors. Rejected A (local-only), C (signal-integrated), D (defer).
> - Pool accounting = instantaneous flow-rate + EMA(~5 ticks) warning; no stored capacity, no ring buffer.
> - Deficit = cliff-edge. Freeze expansion (spawn / road / auto / manual) + slow happiness decay + map-wide desirability decay. No rolling blackouts, no lighting effects.
> - Natural wealth: water body → water pool via Moore-adjacency of treatment building. Forests / mountains = ambient only (no pool feed). Sea → port commerce bonus.
> - Terrain-sensitive placement, no in-range indicator. Discover by try.
> - Infrastructure = own category, not Zone S. Basic tier ungated; 2–3 capacity tiers by output threshold (no tech tree).
> - Cross-scale rollup lossless (grid losses deferred post-MVP). Country deficit cascades down to child regions / cities.
> - Save schema: per-pool floats + contributor ids. `schemaVersion` bump coordinated with Bucket 3 (do not own migration).
> - Landmarks hook = `UtilityContributorRegistry.RegisterWithMultiplier`. Contract owned here, consumed by sibling landmarks doc.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable via `/closeout`).
>
> **Read first if landing cold:**
> - `docs/utilities-exploration.md` — full design + architecture + deficit-entry example + save JSON sample. Design Expansion block is ground truth.
> - `ia/projects/full-game-mvp-master-plan.md` — umbrella Bucket 4 row + schema-bump coordination rule (§Gap B3).
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + cardinality rule (≥2 tasks per phase, ≤6 soft).
> - `ia/rules/invariants.md` — #3 (no `FindObjectOfType` in hot loops), #4 (no new singletons), #5 (no direct `cellArray` outside `GridManager` — helper-service carve-out), #6 (do not add responsibilities to `GridManager`).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; `spec_section save load-pipeline` for persistence step ordering; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

### Stage index

- [Stage 1 — Pool core + contributor registry / Data contracts + enums](stage-1-data-contracts-enums.md) — _In Progress — 4 tasks filed (TECH-331..TECH-334, all Draft)_
- [Stage 2 — Pool core + contributor registry / UtilityPoolService (per-scale)](stage-2-utilitypoolservice-per-scale.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 3 — Pool core + contributor registry / UtilityContributorRegistry](stage-3-utilitycontributorregistry.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 4 — Pool core + contributor registry / Rollup + deficit cascade + DeficitResponseService skeleton](stage-4-rollup-deficit-cascade-deficitresponseservice-skeleton.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 5 — Infrastructure buildings + terrain-sensitive placement / Infrastructure category + building def SO](stage-5-infrastructure-category-building-def-so.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 6 — Infrastructure buildings + terrain-sensitive placement / Placement validators + freeze gate](stage-6-placement-validators-freeze-gate.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 7 — Infrastructure buildings + terrain-sensitive placement / Placement lifecycle + registry wiring + tier promotion](stage-7-placement-lifecycle-registry-wiring-tier-promotion.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 8 — Infrastructure buildings + terrain-sensitive placement / Natural wealth adjacency probe](stage-8-natural-wealth-adjacency-probe.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 9 — Deficit response + UI dashboard / Happiness + desirability decay coroutines](stage-9-happiness-desirability-decay-coroutines.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 10 — Deficit response + UI dashboard / CityStats + DemandManager readers](stage-10-citystats-demandmanager-readers.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 11 — Deficit response + UI dashboard / UIManager utilities dashboard + HUD indicator](stage-11-uimanager-utilities-dashboard-hud-indicator.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 12 — Save/load + landmarks hook + glossary/spec closeout / Save/load schema + restore pipeline](stage-12-load-schema-restore-pipeline.md) — _Draft (tasks _pending_ — not yet filed)_
- [Stage 13 — Save/load + landmarks hook + glossary/spec closeout / Canonical spec + glossary closeout + landmarks contract freeze](stage-13-canonical-spec-glossary-closeout-landmarks-contract-freeze.md) — _In Progress (BUG-20 filed)_

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) runs.
- Run `claude-personal "/stage-file ia/projects/utilities-master-plan.md Stage {N}.{M}"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to `docs/utilities-exploration.md` §Design Expansion.
- Keep this orchestrator synced with `ia/projects/full-game-mvp-master-plan.md` Bucket 4 row — per `/closeout` umbrella-sync rule.
- Coordinate schema bump with `ia/projects/zone-s-economy-master-plan.md` Step 1. Never introduce a mid-tier `schemaVersion` bump from this plan — Bucket 3 owns v3.
- Keep `UtilityContributorRegistry.RegisterWithMultiplier` contract stable once Step 1.3 closes — sibling landmarks doc consumes it.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal step landing triggers a final `Status: Final`; the file stays.
- Silently promote post-MVP items (energy storage, rolling blackouts, grid losses, private operators, climate modifiers, tier visual variants, sea-access commerce bonus impl) into MVP stages — they belong in a future `docs/utilities-post-mvp-extensions.md` stub.
- Add responsibilities to `GridManager` (invariant #6). Cell-loop helpers belong on `GeographyManager` or under `GameManagers/*Service.cs` carve-out (invariant #5). Document rationale at each touch site.
- Add singletons (invariant #4). All three services = MonoBehaviour + Inspector + `FindObjectOfType` fallback in `Awake`.
- Merge partial stage state — every stage must land on a green bar (`npm run validate:all` + `npm run unity:compile-check`).
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Resolve BUG-20 in this plan — orthogonal to contributor registration; track separately.

---
